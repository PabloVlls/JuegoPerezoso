using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoostOnMilestone : MonoBehaviour
{
    [Header("Refs")]
    public SlothMovement slothMovement; // tu script real de movimiento

    [Header("Boost config")]
    [Tooltip("Factor de velocidad para el cambio de carril (ej: 1.8 = 80% más rápido).")]
    public float laneChangeBoostFactor = 1.8f;

    [Tooltip("Duración del boost en segundos.")]
    public float boostDuration = 2f;

    private bool _subscribed = false;

    void Reset()
    {
        slothMovement = GetComponent<SlothMovement>();
    }

    void OnEnable()
    {
        TrySubscribe();
    }

    void Start()
    {
        // Por si el ScoreSystem se inicializa después de este componente
        TrySubscribe();
    }

    void OnDisable()
    {
        TryUnsubscribe();
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;
        if (ScoreSystem.Instance == null) return;

        ScoreSystem.Instance.OnMilestoneReached += HandleMilestone;
        _subscribed = true;
    }

    private void TryUnsubscribe()
    {
        if (!_subscribed) return;
        if (ScoreSystem.Instance != null)
            ScoreSystem.Instance.OnMilestoneReached -= HandleMilestone;

        _subscribed = false;
    }

    private void HandleMilestone(int totalBeans)
    {
        if (slothMovement == null) return;

        // Usamos la API pública del movimiento (encapsulado)
        slothMovement.ApplyLaneBoost(laneChangeBoostFactor, boostDuration);

        // Debug opcional:
        // Debug.Log($"[BoostOnMilestone] Milestone {totalBeans} → Boost x{laneChangeBoostFactor} por {boostDuration}s");
    }
}
