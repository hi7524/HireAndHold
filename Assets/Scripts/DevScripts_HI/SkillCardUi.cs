
using UnityEngine;
using UnityEngine.UI;

public class SkillCardUi : BaseCardUi
{
    [SerializeField] private Image[] starIcons;
    [SerializeField] private GameObject focusImg;
    [Space]
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color filledColor;

    private LevelUpRewardController levelUpRewardController;
    private PassiveSkillManager passiveSkillManager;
    private int currentSkillId = -1;
    private bool isSelected = false;


    private void Awake()
    {
        // 초기 색상 설정
        for (int i = 0; i < starIcons.Length; i++)
        {
            SetIconColor(starIcons[i], defaultColor);
        }
    }

    public void SetFocus(bool value)
    {
        isSelected = value;
        focusImg.SetActive(value);
    }

    public bool IsSelected => isSelected;

    public void SetPassiveSkillId(int skillId)
    {
        currentSkillId = skillId;
        UpdateSkillUI();
        SetFocus(false);
    }

    public void UpdateStarUI(int starCount)
    {
        if (starIcons == null || starIcons.Length == 0) return;

        for (int i = 0; i < starIcons.Length; i++)
        {
            if (i < starCount)
                SetIconColor(starIcons[i], filledColor);
            else
                SetIconColor(starIcons[i], defaultColor);
        }
    }

    public void SetLevelUpRewardController(LevelUpRewardController levelUpRewardController)
    {
        this.levelUpRewardController = levelUpRewardController;
    }

    // 카드 클릭 시 선택
    public void OnCardClicked()
    {
        if (levelUpRewardController != null)
            levelUpRewardController.OnSkillCardSelected(this);
    }

    // 실제 스킬 적용 (확인 버튼 클릭 시 호출)
    public bool ApplySkill()
    {
        if (passiveSkillManager == null)
            return false;

        bool success = passiveSkillManager.AddOrUpgradePassiveSkill(currentSkillId);

        if (success)
        {
            Debug.Log($"패시브 스킬 적용 완료: {currentSkillId}");
        }
        else
        {
            Debug.LogWarning("패시브 스킬 적용 실패");
        }

        return success;
    }
    
    public void SetPassiveSkillManager(PassiveSkillManager manager)
    {
        this.passiveSkillManager = manager;
    }

    private void UpdateSkillUI()
    {
        if (currentSkillId == -1) return;

        SkillData skillData = DataTableManager.SkillTable.Get(currentSkillId);
        if (skillData == null)
        {
            Debug.LogError($"스킬 데이터 없음: {currentSkillId}");
            return;
        }

        EffectData effectData = DataTableManager.EffectTable.Get(skillData.SKILL_EFFECT1_ID);
        if (effectData == null)
        {
            Debug.LogError($"이펙트 데이터 없음: {skillData.SKILL_EFFECT1_ID}");
            return;
        }

        if (text != null)
            text.text = effectData.EFFECT_NAME_KR;

        int starLevel = GetStarLevelFromSkillId(currentSkillId);
        UpdateStarUI(starLevel - 1);
    }

    private int GetStarLevelFromSkillId(int skillId)
    {
        if (skillId < 22070 || skillId > 22087) return 1;
        
        return (skillId - 22070) / 6 + 1;
    }

    private void SetIconColor(Image img, Color color)
    {
        img.color = color;
    }
}