using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ScoreSystem : MonoBehaviour
{
    public static ScoreSystem Instance { get; private set; }

    // ====== Puntaje ======
    [Header("Puntaje")]
    public int Score { get; private set; } = 0;

    // ====== Combo / Multiplicador ======
    [Header("Combo / Multiplicador")]
    [Tooltip("Tiempo máximo entre granos para mantener la racha (s).")]
    [SerializeField] private float comboWindowSeconds = 2.0f;
    [Tooltip("Cada cuántos hits en la racha sube el multiplicador +1.")]
    [SerializeField] private int comboStep = 5;
    [Tooltip("Tope del multiplicador (incluye el x1 base).")]
    [SerializeField] private int maxMultiplier = 5;

    /// <summary> Ventana de combo para la UI (solo lectura). </summary>
    public float ComboWindowSeconds => comboWindowSeconds;

    /// <summary> Momento (Time.time) del último bean recogido; -999 si nunca. </summary>
    public float LastBeanTime { get; private set; } = -999f;

    // Estado público de lectura
    public int CurrentStreak { get; private set; } = 0;
    public int CurrentMultiplier { get; private set; } = 1; // x1 por defecto
    public int TotalBeans { get; private set; } = 0;

    // ====== Hitos (milestones) ======
    [Header("Hitos (para boost, etc.)")]
    [Tooltip("Cada cuántos granos totales se dispara un hito.")]
    [SerializeField] private int milestoneEveryN = 10;

    private int _nextMilestone; // siguiente objetivo: N, 2N, 3N...

    // ====== Eventos ======
    public event Action<int> OnScoreChanged;
    public event Action<int> OnStreakChanged;
    public event Action<int> OnMultiplierChanged;
    public event Action<int> OnTotalBeansChanged;
    public event Action<int> OnMilestoneReached; // pasa TotalBeans cuando se alcanza

    // ====== Ciclo de vida ======
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Inicializar siguiente milestone
        _nextMilestone = Mathf.Max(1, milestoneEveryN);
    }

    // ====== API ======

    /// <summary>
    /// Resetea todos los contadores (útil al iniciar nivel/partida).
    /// </summary>
    public void ResetScore()
    {
        Score = 0;
        CurrentStreak = 0;
        CurrentMultiplier = 1;
        TotalBeans = 0;
        LastBeanTime = -999f;
        _nextMilestone = Mathf.Max(1, milestoneEveryN);

        OnScoreChanged?.Invoke(Score);
        OnStreakChanged?.Invoke(CurrentStreak);
        OnMultiplierChanged?.Invoke(CurrentMultiplier);
        OnTotalBeansChanged?.Invoke(TotalBeans);
    }

    /// <summary>
    /// Llamar cuando recoges un Grano de Café. Suma puntos base * multiplicador,
    /// actualiza racha/multiplicador y dispara milestones según TotalBeans.
    /// </summary>
    public void AddCoffeeBeanPoints(int basePoints)
    {
        float now = Time.time;

        // ¿Sigue el combo dentro de la ventana?
        if (now - LastBeanTime <= comboWindowSeconds)
            CurrentStreak++;
        else
            CurrentStreak = 1; // reinicia racha con este bean

        LastBeanTime = now;
        OnStreakChanged?.Invoke(CurrentStreak);

        // Recalcular multiplicador según la racha
        int newMult = 1 + (CurrentStreak - 1) / Mathf.Max(1, comboStep);
        newMult = Mathf.Clamp(newMult, 1, Mathf.Max(1, maxMultiplier));

        if (newMult != CurrentMultiplier)
        {
            CurrentMultiplier = newMult;
            OnMultiplierChanged?.Invoke(CurrentMultiplier);
        }

        // Sumar puntaje
        int gained = Mathf.Max(0, basePoints) * CurrentMultiplier;
        Score += gained;
        OnScoreChanged?.Invoke(Score);

        // Total de granos
        TotalBeans++;
        OnTotalBeansChanged?.Invoke(TotalBeans);

        // ¿Se alcanzó el siguiente hito?
        if (TotalBeans >= _nextMilestone)
        {
            OnMilestoneReached?.Invoke(TotalBeans);
            _nextMilestone += Mathf.Max(1, milestoneEveryN);
        }
    }

    // ====== (Opcional) Helpers de depuración ======
#if UNITY_EDITOR
    [ContextMenu("DEBUG: Forzar milestone ahora")]
    private void Debug_ForceMilestone()
    {
        OnMilestoneReached?.Invoke(TotalBeans);
    }

    [ContextMenu("DEBUG: AddCoffeeBeanPoints(1)")]
    private void Debug_AddBean()
    {
        AddCoffeeBeanPoints(1);
    }
#endif
}
