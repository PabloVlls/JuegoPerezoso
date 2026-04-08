using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Feedback visual del sistema de combo de saltos.
/// Muestra un contador de combo, una barra de tiempo, y un flash al completar.
///
/// Setup:
/// 1. Crea un Canvas (Screen Space - Overlay).
/// 2. Dentro del Canvas crea:
///    - Un Text (o TextMeshPro) para el contador → asígnalo a comboText.
///    - Una Image (tipo Filled, horizontal) para el timer → asígnalo a timerBar.
///    - Una Image de pantalla completa (color blanco, alpha 0) → asígnalo a flashImage.
/// 3. Coloca este script en el mismo GameObject que ComboJumpSystem (el Player).
/// 4. Arrastra las referencias de UI en el Inspector.
/// </summary>
[RequireComponent(typeof(ComboJumpSystem))]
public class ComboJumpUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Texto que muestra 'x1', 'x2', 'SUPER!'")]
    [SerializeField] TextMeshProUGUI comboText;

    [Tooltip("Imagen tipo Filled que muestra el tiempo restante para encadenar")]
    [SerializeField] Image timerBar;

    [Tooltip("Imagen de pantalla completa para el flash del super jump (alpha 0 por defecto)")]
    [SerializeField] Image flashImage;

    [Header("Configuración visual")]
    [SerializeField] float textPunchScale = 1.3f;     // escala del "punch" al avanzar combo
    [SerializeField] float textPunchDuration = 0.15f;  // duración del punch
    [SerializeField] float flashAlpha = 0.6f;          // alpha máximo del flash
    [SerializeField] float flashDuration = 0.3f;       // duración del flash

    [Header("Colores del combo")]
    [SerializeField] Color comboColor1 = Color.white;       // x1
    [SerializeField] Color comboColor2 = Color.yellow;      // x2
    [SerializeField] Color comboColorSuper = Color.red;     // SUPER!
    [SerializeField] Color timerColorNormal = Color.green;
    [SerializeField] Color timerColorUrgent = Color.red;    // cuando queda poco tiempo

    ComboJumpSystem comboSystem;
    Vector3 _originalTextScale;
    Coroutine _punchCoroutine;
    Coroutine _flashCoroutine;

    void Awake()
    {
        comboSystem = GetComponent<ComboJumpSystem>();

        // Guardar escala original del texto
        if (comboText)
            _originalTextScale = comboText.transform.localScale;

        // Ocultar UI al inicio
        SetUIVisible(false);
    }

    void OnEnable()
    {
        comboSystem.OnComboStep += HandleComboStep;
        comboSystem.OnComboComplete += HandleComboComplete;
        comboSystem.OnComboReset += HandleComboReset;
    }

    void OnDisable()
    {
        comboSystem.OnComboStep -= HandleComboStep;
        comboSystem.OnComboComplete -= HandleComboComplete;
        comboSystem.OnComboReset -= HandleComboReset;
    }

    void Update()
    {
        // Actualizar barra de tiempo si el combo está activo
        if (comboSystem.ComboCount > 0 && timerBar)
        {
            float remaining = comboSystem.TimeRemaining;
            float total = 0.5f; // comboWindow (idealmente leerlo del sistema)
            float fill = Mathf.Clamp01(remaining / total);

            timerBar.fillAmount = fill;

            // Cambiar color cuando queda poco tiempo
            timerBar.color = fill < 0.3f ? timerColorUrgent : timerColorNormal;
        }
    }

    void HandleComboStep(int count)
    {
        SetUIVisible(true);

        if (comboText)
        {
            comboText.text = $"x{count}";

            // Color según el paso del combo
            if (count >= comboSystem.ComboTarget)
                comboText.color = comboColorSuper;
            else if (count >= 2)
                comboText.color = comboColor2;
            else
                comboText.color = comboColor1;

            // Efecto "punch" en el texto
            if (_punchCoroutine != null)
                StopCoroutine(_punchCoroutine);
            _punchCoroutine = StartCoroutine(PunchScale(comboText.transform));
        }

        // Mostrar/actualizar barra de timer
        if (timerBar)
        {
            timerBar.fillAmount = 1f;
            timerBar.color = timerColorNormal;
            timerBar.gameObject.SetActive(true);
        }
    }

    void HandleComboComplete()
    {
        if (comboText)
        {
            comboText.text = "SUPER!";
            comboText.color = comboColorSuper;

            // Punch más grande para el super jump
            if (_punchCoroutine != null)
                StopCoroutine(_punchCoroutine);
            _punchCoroutine = StartCoroutine(PunchScale(comboText.transform, 1.6f));
        }

        // Flash de pantalla
        if (flashImage)
        {
            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashScreen());
        }

        // Ocultar timer (combo completado)
        if (timerBar)
            timerBar.gameObject.SetActive(false);

        // Ocultar texto después de un momento
        StartCoroutine(HideAfterDelay(0.8f));
    }

    void HandleComboReset()
    {
        SetUIVisible(false);
    }

    void SetUIVisible(bool visible)
    {
        if (comboText) comboText.gameObject.SetActive(visible);
        if (timerBar) timerBar.gameObject.SetActive(visible);
        // flashImage siempre existe pero con alpha 0
    }

    // ===================== Efectos con Coroutines =====================

    IEnumerator PunchScale(Transform target, float multiplierOverride = 0f)
    {
        float mult = multiplierOverride > 0f ? multiplierOverride : textPunchScale;
        Vector3 bigScale = _originalTextScale * mult;
        float elapsed = 0f;

        while (elapsed < textPunchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / textPunchDuration;
            // Ease out: empieza grande y vuelve a la escala original
            target.localScale = Vector3.Lerp(bigScale, _originalTextScale, t * t);
            yield return null;
        }

        target.localScale = _originalTextScale;
    }

    IEnumerator FlashScreen()
    {
        // Fade in rápido
        Color c = flashImage.color;
        float halfDuration = flashDuration * 0.3f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            c.a = Mathf.Lerp(0f, flashAlpha, t);
            flashImage.color = c;
            yield return null;
        }

        // Fade out más lento
        elapsed = 0f;
        float fadeOutDuration = flashDuration * 0.7f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            c.a = Mathf.Lerp(flashAlpha, 0f, t);
            flashImage.color = c;
            yield return null;
        }

        c.a = 0f;
        flashImage.color = c;
    }

    IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (comboSystem.ComboCount == 0)
        {
            SetUIVisible(false);
        }
    }
}
