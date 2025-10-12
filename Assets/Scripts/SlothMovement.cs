using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

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
    [SerializeField] float laneWidth = 1.6f;      // separación entre carriles
    [SerializeField] private float laneChangeSpeed = 10f; // m/s para deslizar lateral
    int lane = 0;                                  // -1, 0, +1

    // ====== Gestos ======
    [Header("Gestos (Input System - EnhancedTouch)")]
    [SerializeField] float minSwipePixels = 80f;
    Vector2 swipeStart;
    bool trackingSwipe;

    // ====== Grounding robusto ======
    [Header("Grounding / Snap")]
    [SerializeField] LayerMask groundLayers = ~0;
    [SerializeField] float groundCheckRadius = 0.22f; // ≈ cc.radius * 0.9f
    [SerializeField] float groundCheckOffset = 0.20f; // profundidad de sondeo
    [SerializeField] float maxGroundSnap = 0.30f;     // cuánto puede “pegarse”
    [SerializeField] float coyoteTime = 0.12f;
    float lastGroundedTime;
    bool isGrounded;

    float _lastProbeRadius;
    float _lastCastOriginY;
    float _lastHitDistance;

    // ====== Internos ======
    CharacterController cc;
    float yVelocity;

    // ====== Boost de carril (encapsulado) ======
    float laneChangeSpeedBase;
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
        // Evita que ignore micro-movimientos (crítico para el snap)
        cc.minMoveDistance = 0f;

        // Guardar valor base para restaurar tras boosts
        laneChangeSpeedBase = laneChangeSpeed;
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
            SnapToGroundIfClose(gHit); // elimina jitter/offset en Y
        }

        // --- Gravedad/salto/caída ---
        if (isGrounded && yVelocity < 0f)
            yVelocity = -2f; // mantener pegado al suelo sin acumular caída

        yVelocity += gravity * Time.deltaTime;
        yVelocity = Mathf.Max(yVelocity, maxFallSpeed);

        // --- Movimiento lateral hacia carril objetivo ---
        float targetX = lane * laneWidth;
        float newX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
        float deltaX = newX - transform.position.x;

        // Gravedad y salto (sin auto-movimiento vertical)
        if (cc.isGrounded && yVelocity < 0f) yVelocity = -2f;
        yVelocity += gravity * Time.deltaTime;
        yVelocity = Mathf.Max(yVelocity, maxFallSpeed);
        float deltaY = yVelocity * Time.deltaTime;

        cc.Move(new Vector3(deltaX, deltaY, 0f));

        //Lianas
        if (insideTrigger)
        {
            Vector3 movement = Vector3.up * velocityUp * Time.deltaTime;
            cc.Move(movement);
        }
        // --- Aplicar movimiento (CharacterController.Move recibe DELTAS) ---
        /*Vector3 motion = new Vector3(deltaX, yVelocity * Time.deltaTime, 0f);
        cc.Move(motion);*/
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Liana"))
        {
            insideTrigger = true;
            gravity = 0f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Liana"))
        {
            insideTrigger = false;
            gravity = -30f;
            Debug.Log("salí");
        }    
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
                            lane = Mathf.Clamp(lane + (delta.x > 0 ? +1 : -1), -1, +1);
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
            if (Keyboard.current.aKey.wasPressedThisFrame) lane = Mathf.Clamp(lane - 1, -1, +1);
            if (Keyboard.current.dKey.wasPressedThisFrame) lane = Mathf.Clamp(lane + 1, -1, +1);
            if (Keyboard.current.spaceKey.wasPressedThisFrame) TryJump();
            if (Keyboard.current.sKey.wasPressedThisFrame) QuickDrop();
        }
#endif
    }

    // ====== Suelo / Snap ======

    // Calcula correctamente la posición de los “pies” del CharacterController
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

        // Guarda datos para SnapToGroundIfClose
        if (ok)
        {
            _lastProbeRadius  = probeRadius;
            _lastCastOriginY  = origin.y;
            _lastHitDistance  = hit.distance;
        }

        // Fallback: si el CC reporta grounded, úsalo también
        ok = ok || cc.isGrounded;
        return ok;
    }

    void SnapToGroundIfClose(RaycastHit hit)
    {
        // Recalcula pies en mundo
        Vector3 centerWorld = transform.position + cc.center;
        float baseOffset = (cc.height * 0.5f) - cc.radius;
        Vector3 feet = centerWorld + Vector3.down * (baseOffset - 0.005f);

        // Altura real del suelo en base al SphereCast:
        // groundY = (origen del cast) - (distancia del hit) - (radio de la esfera)
        float groundY = (_lastCastOriginY - _lastHitDistance) - _lastProbeRadius;

        float dist = feet.y - groundY; // cuánto “flotamos” realmente

        if (dist > 0f && dist <= maxGroundSnap)
        {
            cc.Move(Vector3.down * dist);
            if (yVelocity < 0f) yVelocity = -2f; // pegado al piso
        }
    }

    // ====== Acciones ======
    void TryJump()
    {
        bool canJump = cc.isGrounded || (Time.time - lastGroundedTime <= coyoteTime);
        if (canJump)
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    void QuickDrop()
    {
        // Corta salto y acelera caída para enganchar la plataforma inferior
        if (yVelocity > 0f) yVelocity = 0f;
        yVelocity -= dropBoost;
    }

    // ====== Boost de cambio de carril (API pública) ======
    /// <summary>
    /// Aplica un boost temporal al cambio de carril (factor > 1 acelera).
    /// </summary>
    public void ApplyLaneBoost(float factor, float duration)
    {
        if (laneBoostCo != null) StopCoroutine(laneBoostCo);
        laneBoostCo = StartCoroutine(LaneBoostRoutine(factor, duration));
    }

    private System.Collections.IEnumerator LaneBoostRoutine(float factor, float duration)
    {
        laneChangeSpeed = laneChangeSpeedBase * Mathf.Max(0.01f, factor);
        yield return new WaitForSeconds(duration);
        laneChangeSpeed = laneChangeSpeedBase;
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
