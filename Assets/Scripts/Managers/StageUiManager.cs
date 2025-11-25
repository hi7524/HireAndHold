using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class StageUiManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [Space]
    [SerializeField] private GameObject gameControllBtns;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI speedLevelText;
    [SerializeField] private TextMeshProUGUI infoText;
    [Space]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private GameObject skillSelectPanel;
    [SerializeField] private BossHPBar bossHealthBar;
    [SerializeField] private StageClearPanelController stageClearPanel;
    [SerializeField] private RewardPanelController rewardPanel;

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
    public void ShowRewardPanel()
    {
       
        if (rewardPanel != null)
        {
            Debug.Log("[StageUiManager] rewardPanel 활성화!");
            
            gameManager.PauseGame(); // 게임 일시정지
        }
    }

    public void ActiveSkillSelectPanel()
    {
        skillSelectPanel.SetActive(true);
    }
    
    public void SetGameControllBtnsActive(bool isActive)
    {
        gameControllBtns.SetActive(isActive);
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
    public void ShowBossHealthBar(Monster boss, string bossName)
    {
        if (bossHealthBar != null)
        {
            bossHealthBar.ShowBossHealthBar(boss, bossName);
        }
    }

    // 보스 체력바 숨김
    public void HideBossHealthBar()
    {
        if (bossHealthBar != null)
        {
            bossHealthBar.HideBossHealthBar();
        }
    }

    public void ShowStageClearPanel(string stageName, int exp, int gold, int stars)
    {
        stageClearPanel?.Show(stageName, exp, gold, stars);
    }

    public void UpdateInfoText(string msg, Color? color = null)
    {
        infoText.text = msg;
        if (color == null)
            infoText.color = Color.yellow;
        infoText.gameObject.SetActive(true);
    }
}
