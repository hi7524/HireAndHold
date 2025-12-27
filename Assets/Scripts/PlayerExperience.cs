using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Tutorial;
using UnityEngine;
using UnityEngine.UI;

// 플레이어 레벨 및 경험치 관리
public class PlayerExperience : MonoBehaviour
{
    [SerializeField] private Slider expBar;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private float expBarFillDuration = 0.5f;
    [Space]
    [SerializeField] private PassiveSkillManager passiveSkillManager;
    [SerializeField] private StageUiManager uiManager;
 
    public int Level { get; private set; }

    public event Action OnLevelUp;

    private float curPlayerExp;
    private float expRequired;  // 초기값 없이 선언


    private async UniTaskVoid Start()
    {
        Level = 1;
        curPlayerExp = 0f;

        UpdateLevelTextUI();
        expBar.value = 0f;

        await DataTableManager.InitAsync();
        UpdateExpRequired();
    }

    private void UpdateExpRequired()
    {
        // 다음 레벨 데이터 가져오기
        var nextLevelData = DataTableManager.StageLevelTable.GetByLevel(Level + 1);

        if (nextLevelData != null)
        {
            expRequired = nextLevelData.STAGE_EXP_NEED;
        }
        else
        {
            // 최대 레벨 도달 시 (레벨 50 이후)
            Debug.LogWarning($"[PlayerExperience] 레벨 {Level}은 최대 레벨입니다.");
            expRequired = float.MaxValue;
        }
    }

    public void AddExp(int amount)
    {
        float finalAmount = amount;
        float bonusPercent = 0f;

        if (passiveSkillManager != null)
        {
            PassiveSkillEffects effects = passiveSkillManager.GetCurrentEffects();
            bonusPercent = effects.expBonus;
            finalAmount = amount * (1f + bonusPercent / 100f);
        }

        // 최초 경험치 획득 시 튜토리얼 조건 알림
        if (curPlayerExp == 0f && finalAmount > 0f)
        {
            TutorialManager.Instance?.NotifyConditionMet("FIRST_EXP");
        }

        curPlayerExp += finalAmount;

        AnimateExpBar();
    }


    public void Cheat_LevelUp()
    {
        LevelUp();
    }

    private void AnimateExpBar()
    {
        expBar.DOKill();

        if (curPlayerExp >= expRequired)
        {
            expBar.DOValue(1f, expBarFillDuration).SetEase(Ease.OutCubic).SetUpdate(UpdateType.Normal, false).OnComplete(() =>
            {
                LevelUp();
                AnimateExpBar();
            });
        }
        else
        {
            float targetValue = curPlayerExp / expRequired;
            expBar.DOValue(targetValue, expBarFillDuration).SetEase(Ease.OutCubic);
        }
    }

    private void LevelUp()
    {
        Level++;
        curPlayerExp -= expRequired;
        UpdateExpRequired();
        uiManager.SetLevelUpRewardPanelActive(true);
        OnLevelUp?.Invoke();
        UpdateLevelTextUI();
        expBar.value = 0f;

        // 레벨업 시 튜토리얼 체크
        var stageManager = FindObjectOfType<StageManager>();
        int stageId = stageManager != null ? stageManager.CurrentStageId : 0;
        TutorialManager.Instance?.CheckAndStartTutorialAsync(
            TutorialTriggerType.OnLevelUp, stageId, Level).Forget();
    }

    private void UpdateLevelTextUI()
    {
        levelText.text = $"Lv.{Level}";
    }
}