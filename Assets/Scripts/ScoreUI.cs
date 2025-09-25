using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    void Start()
    {
        if (ScoreSystem.Instance == null)
        {
            Debug.LogError("ScoreUI: No hay ScoreSystem en la escena.");
            return;
        }

        ScoreSystem.Instance.OnScoreChanged += UpdateUI;
        UpdateUI(ScoreSystem.Instance.Score);
    }

    void OnDestroy()
    {
        if (ScoreSystem.Instance != null)
            ScoreSystem.Instance.OnScoreChanged -= UpdateUI;
    }

    private void UpdateUI(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }
}
