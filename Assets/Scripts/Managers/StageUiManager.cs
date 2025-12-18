using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class StageUiManager : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] private GameManager gameManager;
     [Header("Uis")]
    [SerializeField] private GameObject gameControllBtns;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI creditText;
    [SerializeField] private TextMeshProUGUI speedLevelText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private BossHPBar bossHealthBar;
    [SerializeField] private GameOverPanelController gameOverPanelController;
     [Header("Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private GameObject skillSelectPanel;
    [SerializeField] private StageClearPanelController stageClearPanel;
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private GameObject levelUpRewardPanel;

    private FadeAndUpEffect infoTextFadeEffect;

    private void Awake()
    {
        // FadeAndUpEffect 컴포넌트 캐싱
        if (infoText != null)
            infoTextFadeEffect = infoText.GetComponent<FadeAndUpEffect>();
    }

    private void Start()
    {
        // 시작시 초기 활성화
        levelUpRewardPanel.SetActive(true);
    }

    private void Update()
    {
        if (!gameManager.IsGameStarted)
            return;

        UpdateTimerText(gameManager.SchedulerTime);
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
    
    public void ShowGameOverPanel(int exp, int gold, Dictionary<int, int> items = null)
    {
        if (gameOverPanelController != null)
        {
            gameOverPanelController.Show(exp, gold, items);
        }
        else
        {
            // Fallback: Controller가 없으면 기본 패널만 표시
            gameOverPanel.SetActive(true);
        }
    }

    public void SetLevelUpRewardPanelActive(bool value)
    {
        Debug.Log($"[StageUiManager] SetLevelUpRewardPanelActive({value}) 호출됨");
        levelUpRewardPanel.SetActive(value);
        Debug.Log($"[StageUiManager] levelUpRewardPanel.activeSelf = {levelUpRewardPanel.activeSelf}");
    }

    public void ActiveSkillSelectPanel()
    {
        skillSelectPanel.SetActive(true);
    }
    
    public void SetGameControllBtnsActive(bool isActive)
    {
        gameControllBtns.SetActive(isActive);
    }

    public void UpdateCreditText(int curGold)
    {
        creditText.text = $"{curGold:N0}";
    }

    public void ShowWarningPanel()
    {
        warningPanel.SetActive(true);
        HideWarningPanel(5f).Forget();
    }
    public void ShowBossRewardPanel()
    {
       rewardPanel.SetActive(true);
       rewardPanel.GetComponent<RewardPanelController>().ShowBossReward();
       gameManager.PauseGame();
    }
    public void ShowWarningReward()
    {
        rewardPanel.SetActive(true);
        rewardPanel.GetComponent<RewardPanelController>().ShowWarningReward();
        gameManager.PauseGame();
    }

    public async UniTask HideWarningPanel(float duration)
    {
        await UniTask.Delay((int)(duration * 1000));
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }
    public void ShowBossHealthBar(Enemy boss, string bossName)
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

    public void ShowStageClearPanel(string stageName, int exp, int gold, int stars, Dictionary<int, int> items = null)
    {
        stageClearPanel?.Show(stageName, exp, gold, stars, items);
    }

    public void UpdateInfoText(string msg, Color? color = null)
    {
        infoText.text = msg;
        if (color == null)
            infoText.color = Color.yellow;
        else
            infoText.color = color.Value;

        if (infoText.gameObject.activeSelf)
        {
            if (infoTextFadeEffect != null)
                infoTextFadeEffect.PlayAnimation();
        }
        else
        {
            infoText.gameObject.SetActive(true);
        }
    }
}
