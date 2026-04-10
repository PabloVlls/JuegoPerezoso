using UnityEngine;
using System;

/// <summary>
/// Sistema de combo de rebote (Bounce).
/// Cuando el jugador hace QuickDrop (swipe abajo) y aterriza,
/// rebota automáticamente hacia arriba con impulso extra.
/// Plataformas con el tag "BouncePlatform" dan un rebote mayor.
///
/// 
/// </summary>
[RequireComponent(typeof(SlothMovement))]
public class BounceComboSystem : MonoBehaviour
{
    [Header("Rebote normal")]
    [Tooltip("Multiplicador de rebote en plataformas normales (sobre la velocidad base de salto)")]
    [SerializeField] float bounceMultiplier = 1.3f;

    [Header("Rebote en plataformas marcadas")]
    [Tooltip("Tag que identifica plataformas con rebote potenciado")]
    [SerializeField] string bouncePlatformTag = "BouncePlatform";

    [Tooltip("Multiplicador de rebote en plataformas marcadas")]
    [SerializeField] float superBounceMultiplier = 2.0f;

    [Header("Configuración")]
    [Tooltip("Tiempo máximo entre el QuickDrop y el aterrizaje para que cuente como rebote")]
    [SerializeField] float maxDropToLandTime = 2f;

    // ===================== Eventos para UI/VFX/Audio =====================
    /// <summary>Se dispara cuando se activa un rebote normal.</summary>
    public event Action OnBounce;

    /// <summary>Se dispara cuando se activa un rebote potenciado (plataforma marcada).</summary>
    public event Action OnSuperBounce;

    // ===================== Estado interno =====================
    SlothMovement movement;

    bool _didQuickDrop = false;
    float _quickDropTime = -999f;

    // ===================== Propiedades públicas (para UI) =====================
    public bool IsDropping => _didQuickDrop;

    void Awake()
    {
        movement = GetComponent<SlothMovement>();
    }

    void OnEnable()
    {
        movement.OnQuickDrop += HandleQuickDrop;
        movement.OnLand += HandleLand;
    }

    void OnDisable()
    {
        movement.OnQuickDrop -= HandleQuickDrop;
        movement.OnLand -= HandleLand;
    }

    void Update()
    {
        if (_didQuickDrop && (Time.time - _quickDropTime) > maxDropToLandTime)
        {
            _didQuickDrop = false;
        }
    }

    void HandleQuickDrop()
    {
        _didQuickDrop = true;
        _quickDropTime = Time.time;
    }

    void HandleLand(Collider groundCollider)
    {
        if (!_didQuickDrop) return;

        _didQuickDrop = false;

        if ((Time.time - _quickDropTime) > maxDropToLandTime) return;

        // Detectar si la plataforma tiene el tag especial directamente del collider
        bool isSuperBounce = false;
        if (groundCollider != null)
        {
            isSuperBounce = groundCollider.CompareTag(bouncePlatformTag);
        }

        // Calcular velocidad de rebote
        float multiplier = isSuperBounce ? superBounceMultiplier : bounceMultiplier;
        float bounceVel = movement.BaseJumpVelocity * multiplier;

        // Aplicar el rebote
        movement.ApplyBounce(bounceVel);

        // Disparar eventos
        if (isSuperBounce)
            OnSuperBounce?.Invoke();
        else
            OnBounce?.Invoke();
    }
}
