using UnityEngine;

/// <summary>
/// Imán de frutas/coleccionables. Atrae objetos con MagnetTarget dentro del radio.
///
/// Setup:
/// 1. Colócalo en el Player (o como hijo).
/// 2. Crea un Layer llamado "Collectible" y asígnalo a tus frutas/items.
/// 3. En el Inspector, setea targetMask = "Collectible" solamente.
/// 4. El imán empieza desactivado; se activa al recoger un FruitMagnetPickup.
/// </summary>
public class FruitMagnet : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] float baseRadius = 8f;
    [SerializeField] float pullSpeed = 16f;
    [Tooltip("IMPORTANTE: Asigna SOLO el layer de coleccionables, no 'Everything'")]
    [SerializeField] LayerMask targetMask;
    [SerializeField] int nonAllocSize = 64;

    [Header("Suavizado")]
    [Range(0f, 1f)]
    [SerializeField] float proximityBoost = 0.6f;
    [SerializeField] float stopDistance = 0.02f;

    [Header("VFX (opcional)")]
    [SerializeField] GameObject magnetVfx;

    // Runtime
    Collider[] _hits;
    float _expireTime = -1f;
    float _activeRadius;

    /// <summary>True si el imán está activo.</summary>
    public bool IsActive => Time.time < _expireTime;

    void Awake()
    {
        _hits = new Collider[nonAllocSize];
        SetVfx(false);
        enabled = false; // solo corre Update cuando esté activo
    }

    /// <summary>Activa o renueva el imán por 'duration' segundos.</summary>
    public void Activate(float duration, float extraRadius = 0f)
    {
        _activeRadius = baseRadius + Mathf.Max(0f, extraRadius);
        float end = Time.time + Mathf.Max(0.01f, duration);
        _expireTime = Mathf.Max(_expireTime, end); // permite renovar sin acortar

        SetVfx(true);
        enabled = true;
    }

    void Update()
    {
        // Verificar expiración
        if (!IsActive)
        {
            Deactivate();
            return;
        }

        PullTargets(Time.deltaTime);
    }

    void PullTargets(float dt)
    {
        Vector3 origin = transform.position;

        int count = Physics.OverlapSphereNonAlloc(
            origin, _activeRadius, _hits, targetMask, QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < count; i++)
        {
            var col = _hits[i];
            if (col == null || !col.gameObject.activeInHierarchy) continue;

            // Buscar MagnetTarget en el objeto o su padre
            var target = col.GetComponent<MagnetTarget>();
            if (target == null) target = col.GetComponentInParent<MagnetTarget>();
            if (target == null || !target.eligible) continue;

            // Desparentar del Section/spawner para que el pull no sea
            // cancelado por el movimiento del padre procedural
            Transform moveTf = target.transform;
            if (moveTf.parent != null)
            {
                moveTf.SetParent(null);
            }

            Vector3 pos = target.GetWorldPosition();
            Vector3 toPlayer = origin - pos;
            float dist = toPlayer.magnitude;

            if (dist <= stopDistance) continue;

            Vector3 dir = toPlayer / dist;

            // Más tirón cuanto más cerca del jugador
            float proximity = 1f + proximityBoost * Mathf.Clamp01(1f - (dist / _activeRadius));
            float step = pullSpeed * proximity * dt;

            // Mover con Rigidbody si existe, si no, directo al transform
            if (moveTf.TryGetComponent<Rigidbody>(out var rb) && rb != null)
                rb.MovePosition(moveTf.position + dir * step);
            else
                moveTf.position += dir * step;
        }
    }

    void Deactivate()
    {
        _expireTime = -1f;
        SetVfx(false);
        enabled = false;
    }

    void SetVfx(bool on)
    {
        if (magnetVfx != null) magnetVfx.SetActive(on);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        float r = (Application.isPlaying && IsActive) ? _activeRadius : baseRadius;
        Gizmos.DrawWireSphere(transform.position, r);
    }
#endif
}
