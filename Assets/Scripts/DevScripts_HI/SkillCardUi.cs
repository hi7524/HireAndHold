
using UnityEngine;
using UnityEngine.UI;

public class SkillCardUi : BaseCardUi
{
    public int count; // 테스트용 수정 필요 (PlayerData와 연결 필요)** 

    [SerializeField] private Image[] starIcons;
    [Space]
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color filledColor;

    private LevelUpRewardController levelUpRewardController;
    private PassiveSkillManager passiveSkillManager;          // ← 추가
    private int currentSkillId = -1;


    private void Start()
    {
        // 초기 색상 설정
        for (int i = 0; i < count; i++)
        {
            SetIconColor(starIcons[i], filledColor);
        }
    }
    public void SetPassiveSkillId(int skillId)
    {
        currentSkillId = skillId;  
        UpdateSkillUI();           
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

    public void SelectSkill()
    {
        // 1. 유효성 체크
        if (currentSkillId == -1)
        {
            Debug.LogWarning("선택할 스킬이 없습니다.");
            return;
        }

        if (passiveSkillManager == null)
        {
            Debug.LogError("PassiveSkillManager가 없습니다!");
            return;
        }

        // 2. 패시브 스킬 적용!
        bool success = passiveSkillManager.AddOrUpgradePassiveSkill(currentSkillId);

        // 3. 성공하면 게임 재개
        if (success)
        {
            Debug.Log($"패시브 스킬 선택 완료: {currentSkillId}");
            levelUpRewardController.OnClickConfirmBtn();  // 보상창 닫기
        }
        else
        {
            Debug.LogWarning("패시브 스킬 적용 실패");
        }
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
        UpdateStarUI(starLevel );
    }
    private int GetStarLevelFromSkillId(int skillId)
    {
        if (skillId < 222070 || skillId > 222087) return 1;
        return (skillId - 222070) / 6 + 1;
    }

    private void SetIconColor(Image img, Color color)
    {
        img.color = color;
    }
}