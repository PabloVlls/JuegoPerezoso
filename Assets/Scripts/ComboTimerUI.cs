using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ComboTimerUI : MonoBehaviour
{
    [SerializeField] private Image fillImage; // Image con Fill Method
    [SerializeField] private Color safeColor = Color.white;
    [SerializeField] private Color dangerColor = Color.red;

    void Update()
    {
        var ss = ScoreSystem.Instance;
        if (ss == null || fillImage == null) return;

        float elapsed = Time.time - ss.LastBeanTime;
        float window = Mathf.Max(0.0001f, ss.ComboWindowSeconds);

        // Si nunca has tomado un bean, evita valores raros
        if (ss.LastBeanTime < 0f) { fillImage.fillAmount = 0f; return; }

        float remaining = Mathf.Clamp01(1f - (elapsed / window));
        fillImage.fillAmount = remaining;

        // Color: de safe → danger cuando se agota
        fillImage.color = Color.Lerp(dangerColor, safeColor, remaining);
    }
}
