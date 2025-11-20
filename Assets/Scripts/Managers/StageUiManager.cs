using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class StageUiManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [Space]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI speedLevelText;
    [Space]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private GameObject bossPricePanel;
    [SerializeField] private GameObject skillSelectPanel;

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
    public void ActiveBossPricePanel()
    {
        bossPricePanel.SetActive(true);
    }
    public void ActiveSkillSelectPanel()
    {
        skillSelectPanel.SetActive(true);
    }

    public void UpdateStageGoldText(int curGold)
    {
        goldText.text = $"{curGold:N0}G";
    }
    public void ShowWarningPanel()
    {
        warningPanel.SetActive(true);
        HideWarningPanel(5f).Forget();
    }
    public async UniTask HideWarningPanel(float duration)
    {
        await UniTask.Delay((int)(duration * 1000));
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }
    
}