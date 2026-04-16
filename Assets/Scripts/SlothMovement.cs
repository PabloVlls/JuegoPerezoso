using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System;

[RequireComponent(typeof(CharacterController))]
public class SlothMovement : MonoBehaviour
{
    // ====== Física ======
    [Header("Física")]
    [SerializeField] float gravity = -30f;        // m/s² (negativa)
    [SerializeField] float jumpHeight = 2.6f;     // metros
    [SerializeField] float maxFallSpeed = -25f;   // límite de caída
    [SerializeField] float dropBoost = 10f;       // impulso extra hacia abajo (swipe ↓)

    // ====== Carriles ======
    [Header("Carriles (−1, 0, +1)")]
    [SerializeField] float laneWidth = 1.6f;          // separación entre carriles
    [SerializeField] float laneChangeDuration = 0.12f; // segundos para completar un cambio (0.10–0.15 = Subway Surfers)
    int lane = 0;                                      // -1, 0, +1

    // Estado del cambio de carril
    bool _isChangingLane = false;
    float _laneFromX;           // posición X de inicio
    float _laneToX;             // posición X de destino
    float _laneChangeT;        // progreso 0→1
    float _laneChangeDur;      // duración actual (puede variar por boost)

    // Input queueing: permite encolar UN cambio mientras estás en movimiento
    bool _hasPendingLane = false;
    int _pendingLane;

    // ====== Gestos ======
    [Header("Gestos (Input System - EnhancedTouch)")]
    [SerializeField] float minSwipePixels = 80f;
    Vector2 swipeStart;
    bool trackingSwipe;

    // ====== Grounding robusto ======
    [Header("Grounding / Snap")]
    [SerializeField] LayerMask groundLayers = ~0;
    [SerializeField] float groundCheckRadius = 0.22f;
    [SerializeField] float groundCheckOffset = 0.20f;
    [SerializeField] float maxGroundSnap = 0.30f;
    [SerializeField] float coyoteTime = 0.12f;
    float lastGroundedTime;
    bool isGrounded;

    float _lastProbeRadius;
    float _lastCastOriginY;
    float _lastHitDistance;

    // ====== Eventos para Combo System ======
    /// <summary>Se dispara cada vez que el jugador salta.</summary>
    public event Action OnJump;

    /// <summary>Se dispara cuando el jugador aterriza (aire → suelo). Pasa el Collider de la plataforma (puede ser null).</summary>
    public event Action<Collider> OnLand;

    /// <summary>Se dispara cuando se ejecuta un super jump.</summary>
    public event Action OnSuperJump;

    /// <summary>Se dispara cuando el jugador hace QuickDrop (swipe abajo en el aire).</summary>
    public event Action OnQuickDrop;

    // ====== Super Jump (combo) ======
    [NonSerialized] public float jumpMultiplier = 1f;

    /// <summary>Velocidad base del salto (sin multiplicadores). Útil para sistemas externos.</summary>
    public float BaseJumpVelocity => Mathf.Sqrt(jumpHeight * -2f * gravity);

    // ====== Internos ======
    CharacterController cc;
    float yVelocity;
    bool _wasGroundedLastFrame = false;

    // ====== Boost de carril (encapsulado) ======
    float _laneBoostFactor = 1f;
    Coroutine laneBoostCo;

    // ====== Ciclo de vida ======
    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        TouchSimulation.Enable(); // permite probar con mouse en el Editor
    }

    void OnDisable()
    {
        TouchSimulation.Disable();
        EnhancedTouchSupport.Disable();
    }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        Application.targetFrameRate = 60;
        cc.minMoveDistance = 0f;
    }

    void Update()
    {
        HandleSwipe();

        // --- Grounding robusto (antes de física) ---
        RaycastHit gHit;
        isGrounded = CheckGround(out gHit);
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            // Solo hacer snap si NO estamos saltando hacia arriba
            if (yVelocity <= 0f)
                SnapToGroundIfClose(gHit);
        }

        // --- Detectar aterrizaje (aire → suelo) ---
        bool groundedNow = isGrounded || cc.isGrounded;
        if (groundedNow && !_wasGroundedLastFrame)
        {
            OnLand?.Invoke(gHit.collider); // pasa el collider de la plataforma
        }
        _wasGroundedLastFrame = groundedNow;

        // --- Gravedad/salto/caída ---
        if (isGrounded && yVelocity < 0f)
            yVelocity = -2f;

        yVelocity += gravity * Time.deltaTime;
        yVelocity = Mathf.Max(yVelocity, maxFallSpeed);

        // --- Movimiento lateral (ease-out, estilo Subway Surfers) ---
        float deltaX = 0f;
        if (_isChangingLane)
        {
            _laneChangeT += Time.deltaTime / _laneChangeDur;

            if (_laneChangeT >= 1f)
            {
                // Llegamos al carril destino
                _laneChangeT = 1f;
                _isChangingLane = false;

                // Snap exacto al carril
                float finalX = _laneToX;
                deltaX = finalX - transform.position.x;

                // ¿Hay un cambio pendiente?
                if (_hasPendingLane)
                {
                    _hasPendingLane = false;
                    int newLane = Mathf.Clamp(_pendingLane, -1, +1);
                    if (newLane != lane)
                    {
                        StartLaneChange(newLane);
                    }
                }
            }
            else
            {
                // Interpolación con ease-out cúbico: rápido al inicio, suave al final
                float eased = EaseOutCubic(_laneChangeT);
                float targetX = Mathf.Lerp(_laneFromX, _laneToX, eased);
                deltaX = targetX - transform.position.x;
            }
        }
        else
        {
            // No corregir automáticamente: el jugador solo se mueve
            // entre carriles cuando hace swipe explícito
            deltaX = 0f;
        }

        // --- Aplicar movimiento (CharacterController.Move recibe DELTAS) ---
        Vector3 motion = new Vector3(deltaX, yVelocity * Time.deltaTime, 0f);
        cc.Move(motion);
    }

    // ====== Input / Gestos ======
    void HandleSwipe()
    {
        foreach (var t in ETouch.activeTouches)
        {
            switch (t.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    swipeStart = t.screenPosition;
                    trackingSwipe = true;
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                    if (!trackingSwipe) break;
                    Vector2 delta = t.screenPosition - swipeStart;

                    if (delta.magnitude >= minSwipePixels)
                    {
                        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                        {
                            // Izquierda / Derecha
                            int dir = delta.x > 0 ? +1 : -1;
                            RequestLaneChange(dir);
                        }
                        else
                        {
                            // Arriba / Abajo
                            if (delta.y > 0) TryJump();
                            else QuickDrop();
                        }
                    }
                    trackingSwipe = false;
                    break;
            }
        }

#if UNITY_EDITOR
        // Controles de prueba en Editor
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.wasPressedThisFrame) RequestLaneChange(-1);
            if (Keyboard.current.dKey.wasPressedThisFrame) RequestLaneChange(+1);
            if (Keyboard.current.spaceKey.wasPressedThisFrame) TryJump();
            if (Keyboard.current.sKey.wasPressedThisFrame) QuickDrop();
        }
#endif
    }

    // ====== Suelo / Snap ======

    Vector3 GetFeetWorld()
    {
        Vector3 centerWorld = transform.position + cc.center;
        float baseOffset = (cc.height * 0.5f) - cc.radius;
        return centerWorld + Vector3.down * (baseOffset - 0.005f);
    }

    bool CheckGround(out RaycastHit hit)
    {
        Vector3 centerWorld = transform.position + cc.center;
        float baseOffset = (cc.height * 0.5f) - cc.radius;
        Vector3 feet = centerWorld + Vector3.down * (baseOffset - 0.005f);

        float probeRadius = Mathf.Max(0.05f, cc.radius * 0.9f);
        Vector3 origin = feet + Vector3.up * 0.02f;

        bool ok = Physics.SphereCast(
            origin,
            probeRadius,
            Vector3.down,
            out hit,
            groundCheckOffset + 0.02f,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        if (ok)
        {
            _lastProbeRadius = probeRadius;
            _lastCastOriginY = origin.y;
            _lastHitDistance = hit.distance;
        }

        ok = ok || cc.isGrounded;
        return ok;
    }

    void SnapToGroundIfClose(RaycastHit hit)
    {
        Vector3 centerWorld = transform.position + cc.center;
        float baseOffset = (cc.height * 0.5f) - cc.radius;
        Vector3 feet = centerWorld + Vector3.down * (baseOffset - 0.005f);

        float groundY = (_lastCastOriginY - _lastHitDistance) - _lastProbeRadius;
        float dist = feet.y - groundY;

        if (dist > 0f && dist <= maxGroundSnap)
        {
            cc.Move(Vector3.down * dist);
            if (yVelocity < 0f) yVelocity = -2f;
        }
    }

    // ====== Acciones ======
    void TryJump()
    {
        bool canJump = cc.isGrounded || (Time.time - lastGroundedTime <= coyoteTime);
        if (canJump)
        {
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity) * jumpMultiplier;

            bool wasSuperJump = jumpMultiplier > 1f;
            jumpMultiplier = 1f;

            OnJump?.Invoke();
            if (wasSuperJump)
            {
                OnSuperJump?.Invoke();
            }
        }
    }

    // ====== Cambio de carril (estilo Subway Surfers) ======

    /// <summary>Solicita un cambio de carril. Si ya estamos cambiando, lo encola.</summary>
    void RequestLaneChange(int dir)
    {
        int targetLane = Mathf.Clamp(lane + dir, -1, +1);

        // Si no cambió (ya estamos en el borde), ignorar
        if (targetLane == lane && !_isChangingLane) return;

        if (_isChangingLane)
        {
            // Encolar: solo guardamos el carril final deseado
            int pendingTarget = Mathf.Clamp(lane + dir, -1, +1);
            if (pendingTarget != lane)
            {
                _hasPendingLane = true;
                _pendingLane = pendingTarget;
            }
        }
        else
        {
            StartLaneChange(targetLane);
        }
    }

    /// <summary>Inicia un cambio de carril con animación ease-out.</summary>
    void StartLaneChange(int targetLane)
    {
        _laneFromX = transform.position.x;
        _laneToX = targetLane * laneWidth;
        _laneChangeT = 0f;
        _laneChangeDur = laneChangeDuration / Mathf.Max(0.1f, _laneBoostFactor);
        _isChangingLane = true;
        lane = targetLane;
    }

    /// <summary>Curva ease-out cúbica: arranque rápido, frenado suave.</summary>
    static float EaseOutCubic(float t)
    {
        t = 1f - t;
        return 1f - (t * t * t);
    }

    void QuickDrop()
    {
        if (yVelocity > 0f) yVelocity = 0f;
        yVelocity -= dropBoost;
        OnQuickDrop?.Invoke();
    }

    /// <summary>
    /// Aplica una velocidad vertical directa (usado por BounceComboSystem para el rebote).
    /// </summary>
    public void ApplyBounce(float bounceVelocity)
    {
        yVelocity = bounceVelocity;
    }

    // ====== Boost de cambio de carril (API pública) ======
    /// <summary>
    /// Aplica un boost temporal al cambio de carril.
    /// factor > 1 = más rápido, factor < 1 = más lento.
    /// </summary>
    public void ApplyLaneBoost(float factor, float duration)
    {
        if (laneBoostCo != null) StopCoroutine(laneBoostCo);
        laneBoostCo = StartCoroutine(LaneBoostRoutine(factor, duration));
    }

    private System.Collections.IEnumerator LaneBoostRoutine(float factor, float duration)
    {
        _laneBoostFactor = Mathf.Max(0.1f, factor);
        yield return new WaitForSeconds(duration);
        _laneBoostFactor = 1f;
        laneBoostCo = null;
    }

    // ====== Debug opcional ======
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!cc) cc = GetComponent<CharacterController>();
        Vector3 feet = transform.position + Vector3.down * (cc.height * 0.5f - cc.radius + 0.01f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(feet, 0.05f);
        Gizmos.color = Color.cyan;
        Vector3 origin = feet + Vector3.up * 0.02f;
        Gizmos.DrawWireSphere(origin - Vector3.up * (groundCheckOffset), groundCheckRadius);
    }
#endif
}
