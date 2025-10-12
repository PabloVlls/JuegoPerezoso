using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitMagnet : MonoBehaviour
{
    [Header("Básicos")]
    [SerializeField] private float baseRadius = 8f;
    [SerializeField] private float pullSpeed = 16f;          // m/s hacia el jugador
    [SerializeField] private LayerMask targetMask = ~0;       // marca aquí "Collectible"
    [SerializeField] private int nonAllocSize = 128;

    [Header("Suavizado")]
    [Range(0f, 1f)] public float proximityBoost = 0.6f;      // más tirón cerca del jugador
    [SerializeField] private float stopDistance = 0.02f;      // evita jitter al llegar

    [Header("VFX (opcional)")]
    public GameObject magnetVfx;

    // runtime
    private Collider[] _hits;
    private float _expireTime = -1f;
    private float _activeRadius;
    public bool IsActive => Time.time < _expireTime;

    void Awake()
    {
        _hits = new Collider[nonAllocSize];
        if (magnetVfx) magnetVfx.SetActive(false);
        enabled = false; // solo corre Update cuando esté activo
    }
    
    void Start()
    {
        // SOLO PARA PRUEBA: imán encendido 60s sin pickup
        Activate(60f, 0f);
    }

    /// <summary>Activa/renueva el imán por 'duration' segundos. extraRadius suma al radio base.</summary>
    public void Activate(float duration, float extraRadius = 0f)
    {
        _activeRadius = baseRadius + Mathf.Max(0f, extraRadius);
        float end = Time.time + Mathf.Max(0.01f, duration);
        _expireTime = Mathf.Max(_expireTime, end);

        if (magnetVfx) magnetVfx.SetActive(true);
        enabled = true;

        // Debug opcional
        // Debug.Log($"[FruitMagnet] ON por {duration:F1}s. Radio={_activeRadius:F1}");
    }

    void Update()
    {
        if (!IsActive) return;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position, _activeRadius, _hits, targetMask, QueryTriggerInteraction.Collide
        );
        // Log mínimo (comentarlo luego)
        if (count == 0)
            Debug.Log("[FruitMagnet] 0 objetivos en radio");
        else
            Debug.Log($"[FruitMagnet] Detectados: {count}");
    
        ScanAndPull(Time.deltaTime);
    }

    private void ScanAndPull(float dt)
    {
        Vector3 origin = transform.position;

        int count = Physics.OverlapSphereNonAlloc(
            origin, _activeRadius, _hits, targetMask, QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < count; i++)
        {
            var col = _hits[i];
            if (!col || !col.gameObject.activeInHierarchy) continue;

            var target = col.GetComponent<MagnetTarget>() ?? col.GetComponentInParent<MagnetTarget>();
            if (target == null || !target.eligible) continue;

            Transform moveTf = target.transform;
            Vector3 pos = target.GetWorldPosition();

            Vector3 toPlayer = origin - pos;
            float dist = toPlayer.magnitude;
            if (dist <= stopDistance) continue;

            Vector3 dir = toPlayer / dist;
            float proximity = 1f + proximityBoost * Mathf.Clamp01(1f - (dist / _activeRadius));
            float step = pullSpeed * proximity * dt;

            if (moveTf.TryGetComponent<Rigidbody>(out var rb) && rb != null)
                rb.MovePosition(moveTf.position + dir * step);
            else
                moveTf.position += dir * step;
        }
    }
    
    [ContextMenu("DEBUG: Listar MagnetTargets en escena")]
    void Debug_ListTargets()
    {
        var all = FindObjectsOfType<MagnetTarget>(true);
        Debug.Log($"[FruitMagnet] MagnetTargets en escena: {all.Length}");
        foreach (var m in all)
            Debug.Log($"  - {m.name} (active={m.gameObject.activeInHierarchy})");
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
