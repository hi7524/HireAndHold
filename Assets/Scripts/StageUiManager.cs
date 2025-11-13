using TMPro;
using UnityEngine;

public class StageUiManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [Space]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI speedLevelText;

    private void Update()
    {
        if (!gameManager.IsGameStarted)
            return;

        UpdateTimerText(gameManager.ElapsedTime);
    }

    private void UpdateTimerText(float elapsedTime)
    {
        int minutes = Mathf.FloorToInt((elapsedTime % 3600) / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void UpdateSpeedLevelText()
    {
        speedLevelText.text = $"X{gameManager.CurSpeedLevel}";
    }
}