using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

public class SkillCardUi : BaseCardUi
{
    [SerializeField] private Image skillIcon;  // 스킬 아이콘 이미지
    [SerializeField] private Image[] starIcons;
    [SerializeField] private GameObject focusImg;
    [Space]
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color filledColor;

    private LevelUpRewardController levelUpRewardController;
    private PassiveSkillManager passiveSkillManager;
    private int currentSkillId = -1;
    private bool isSelected = false;

    private AsyncOperationHandle<Sprite> iconHandle;

    // SkillSelectUi용 콜백
    public Action OnCardClickedCallback;

    public void SetFocus(bool value)
    {
        isSelected = value;
        if (focusImg != null)
            focusImg.SetActive(value);
    }

    public bool IsSelected => isSelected;

    public void SetPassiveSkillId(int skillId)
    {
        currentSkillId = skillId;
        UpdateSkillUI();
        SetFocus(false);
    }

    public void SetPlayerSkillId(int skillId)
    {
        currentSkillId = skillId;
        UpdatePlayerSkillUI();
        SetFocus(false);
    }

    private async void UpdatePlayerSkillUI()
    {
        if (currentSkillId == -1) return;

        SkillData skillData = DataTableManager.SkillTable.Get(currentSkillId);
        if (skillData == null)
        {
            return;
        }

        // 스킬 이름 설정
        if (text != null)
            text.text = skillData.SKILL_NAME;

        // PlayerSkill은 별 레벨이 없으므로 별 UI 숨김 (0개)
        UpdateStarUI(0);

        // 스킬 아이콘 로드
        await LoadSkillIconAsync(skillData.SKILL_ICON);
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
        // LevelUpRewardController가 있으면 그쪽으로
        if (levelUpRewardController != null)
        {
            levelUpRewardController.OnSkillCardSelected(this);
        }
        // SkillSelectUi 콜백이 있으면 그쪽으로
        else if (OnCardClickedCallback != null)
        {
            OnCardClickedCallback.Invoke();
        }
    }

    // 실제 스킬 적용 (확인 버튼 클릭 시 호출) - PassiveSkill 전용
    public bool ApplySkill()
    {
        if (passiveSkillManager == null)
            return false;

        bool success = passiveSkillManager.AddOrUpgradePassiveSkill(currentSkillId);
        return success;
    }

    public void SetPassiveSkillManager(PassiveSkillManager manager)
    {
        this.passiveSkillManager = manager;
    }

    public int GetCurrentSkillId()
    {
        return currentSkillId;
    }

    private async void UpdateSkillUI()
    {
        if (currentSkillId == -1) return;

        SkillData skillData = DataTableManager.SkillTable.Get(currentSkillId);
        if (skillData == null)
        {
            return;
        }

        EffectData effectData = DataTableManager.EffectTable.Get(skillData.SKILL_EFFECT1_ID);
        if (effectData == null)
        {
            return;
        }

        if (text != null)
            text.text = effectData.EFFECT_NAME_KR;

        int starLevel = GetStarLevelFromSkillId(currentSkillId);
        UpdateStarUI(starLevel - 1);

        // 스킬 아이콘 로드
        await LoadSkillIconAsync(skillData.SKILL_ICON);
    }

    private async UniTask LoadSkillIconAsync(string iconAddress)
    {
        // 기존 아이콘 해제
        if (iconHandle.IsValid())
        {
            Addressables.Release(iconHandle);
        }

        if (string.IsNullOrEmpty(iconAddress))
        {
            return;
        }

        // 공백 및 특수문자 제거
        iconAddress = iconAddress.Trim();

        // Addressable로 Sprite 로드
        iconHandle = Addressables.LoadAssetAsync<Sprite>(iconAddress);
        var sprite = await iconHandle.ToUniTask();

        if (iconHandle.Status == AsyncOperationStatus.Succeeded && skillIcon != null)
        {
            skillIcon.sprite = sprite;
        }
       
    }

    private int GetStarLevelFromSkillId(int skillId)
    {
        if (skillId < 22070 || skillId > 22087) return 1;

        return (skillId - 22070) / 6 + 1;
    }

    private void SetIconColor(Image img, Color color)
    {
        if (img != null)
            img.color = color;
    }

    private void OnDestroy()
    {
        // Addressable 리소스 해제
        if (iconHandle.IsValid())
        {
            Addressables.Release(iconHandle);
        }
    }
}