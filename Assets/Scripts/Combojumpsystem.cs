using UnityEngine;
using System;

/// <summary>
/// Sistema de combo de saltos consecutivos.
/// Escucha los eventos OnJump y OnLand de SlothMovement.
/// Si el jugador salta 3 veces consecutivas (aterrizando y saltando
/// dentro de la ventana de tiempo), el 3er salto se convierte en Super Jump.
/// 
/// Setup: Colócalo en el mismo GameObject que SlothMovement (el Player).
/// </summary>
[RequireComponent(typeof(SlothMovement))]
public class ComboJumpSystem : MonoBehaviour
{
    [Header("Combo Config")]
    [Tooltip("Tiempo máximo entre aterrizar y volver a saltar para que cuente como combo")]
    [SerializeField] float comboWindow = 0.5f;

    [Tooltip("Cantidad de saltos encadenados para activar el super jump")]
    [SerializeField] int comboTarget = 3;

    [Tooltip("Multiplicador de altura del super jump (1.8 = 80% más alto)")]
    [SerializeField] float superJumpMultiplier = 1.8f;

    // ===================== Eventos para UI/VFX/Audio =====================
    /// <summary>Se dispara cada vez que el combo avanza (comboCount actualizado).</summary>
    public event Action<int> OnComboStep;

    /// <summary>Se dispara cuando el combo se completa y se activa el super jump.</summary>
    public event Action OnComboComplete;

    /// <summary>Se dispara cuando el combo se rompe (timeout o no saltó a tiempo).</summary>
    public event Action OnComboReset;

    // ===================== Estado interno =====================
    SlothMovement movement;
    int comboCount = 0;          // saltos encadenados actuales
    float lastLandTime = -999f;  // cuándo fue el último aterrizaje
    bool waitingForJump = false; // true después de aterrizar, esperando el siguiente salto

    // ===================== Propiedades públicas (para UI) =====================
    /// <summary>Cantidad actual de saltos en el combo (0 a comboTarget).</summary>
    public int ComboCount => comboCount;

    /// <summary>Objetivo de saltos para completar el combo.</summary>
    public int ComboTarget => comboTarget;

    /// <summary>Tiempo restante para encadenar el siguiente salto (0 si no hay combo activo).</summary>
    public float TimeRemaining
    {
        get
        {
            if (!waitingForJump) return 0f;
            return Mathf.Max(0f, comboWindow - (Time.time - lastLandTime));
        }
    }

    void Awake()
    {
        movement = GetComponent<SlothMovement>();
    }

    void OnEnable()
    {
        movement.OnJump += HandleJump;
        movement.OnLand += HandleLand;
    }

    void OnDisable()
    {
        movement.OnJump -= HandleJump;
        movement.OnLand -= HandleLand;
    }

    void Update()
    {
        // Si estamos esperando un salto y se acabó el tiempo, resetear combo
        if (waitingForJump && (Time.time - lastLandTime) > comboWindow)
        {
            ResetCombo();
        }
    }

    void HandleLand()
    {
        // Al aterrizar, empezamos a contar el tiempo para el próximo salto
        lastLandTime = Time.time;
        waitingForJump = true;
    }

    void HandleJump()
    {
        // ¿Saltamos dentro de la ventana de combo?
        if (waitingForJump && (Time.time - lastLandTime) <= comboWindow)
        {
            comboCount++;
            waitingForJump = false;

            // Notificar que el combo avanzó
            OnComboStep?.Invoke(comboCount);

            // ¿Llegamos al objetivo?
            if (comboCount >= comboTarget)
            {
                // Preparar super jump: el multiplicador se aplica en SlothMovement
                // NOTA: el salto actual YA se ejecutó con velocidad normal.
                // El super jump se aplica al SIGUIENTE salto que se acaba de hacer.
                // Para que funcione en el salto actual, seteamos el multiplicador
                // ANTES de que SlothMovement calcule la velocidad.
                // Como los eventos se disparan DESPUÉS del cálculo, necesitamos
                // aplicar la velocidad extra directamente aquí.
                ApplySuperJumpRetroactively();

                OnComboComplete?.Invoke();
                comboCount = 0; // resetear para el siguiente combo
            }
        }
        else
        {
            // Primer salto de una nueva cadena (o fuera de ventana)
            comboCount = 1;
            waitingForJump = false;
            OnComboStep?.Invoke(comboCount);
        }
    }

    void ApplySuperJumpRetroactively()
    {
        // El salto ya se calculó en SlothMovement con velocidad normal.
        // Multiplicamos la velocidad vertical actual para convertirlo en super jump.
        // Accedemos al CharacterController indirectamente a través del campo público.
        //
        // Alternativa más limpia: setear jumpMultiplier ANTES del salto.
        // Pero como el evento se dispara después, hacemos el ajuste aquí.
        //
        // Lo que hacemos: la velocidad del salto normal es sqrt(2*g*h).
        // Queremos que sea sqrt(2*g*h) * multiplier.
        // Como ya se aplicó la velocidad normal, multiplicamos lo que hay.

        // Usamos reflexión ligera: accedemos al campo yVelocity
        // Mejor approach: hacemos que SlothMovement exponga un método
        // Para mantenerlo simple, usamos el jumpMultiplier que ya existe
        // y forzamos un recálculo.

        // En realidad, la forma más limpia es que el ComboJumpSystem
        // setee el jumpMultiplier ANTES de que se ejecute el salto.
        // Lo hacemos en Update() chequeando si el combo está listo.
    }

    // Llamado desde LateUpdate para preparar el multiplicador ANTES del siguiente frame
    void LateUpdate()
    {
        // Si el combo está en comboTarget - 1, el próximo salto será el super jump
        // Seteamos el multiplicador preventivamente
        if (comboCount == comboTarget - 1 && waitingForJump)
        {
            movement.jumpMultiplier = superJumpMultiplier;
        }
        else
        {
            // Solo resetear si no estamos a punto de hacer super jump
            if (movement.jumpMultiplier != 1f && comboCount < comboTarget - 1)
            {
                movement.jumpMultiplier = 1f;
            }
        }
    }

    void ResetCombo()
    {
        if (comboCount > 0)
        {
            OnComboReset?.Invoke();
        }
        comboCount = 0;
        waitingForJump = false;
        movement.jumpMultiplier = 1f;
    }
}
