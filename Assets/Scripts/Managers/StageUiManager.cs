using TMPro;
using UnityEngine;

public class StageUiManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [Space]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI speedLevelText;
    [SerializeField] private TextMeshProUGUI infoText;
    [Space]
    [SerializeField] private GameObject gameOverPanel;

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

    public void ActiveGameOverPanel()
    {
        gameOverPanel.SetActive(true);
    }

    public void UpdateStageGoldText(int curGold)
    {
        goldText.text = $"{curGold:N0}G";
    }

    public void UpdateInfoText(string msg)
    {
        infoText.text = msg;
        infoText.gameObject.SetActive(true);
    }
}