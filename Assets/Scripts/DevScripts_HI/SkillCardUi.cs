using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using DG.Tweening;
using AssetKits.ParticleImage;

public class SkillCardUi : BaseCardUi
{
    public enum CardMode
    {
        PassiveSkill,   // 패시브 스킬 모드
        PlayerSkill,    // 플레이어 스킬 모드
        Gold            // 골드 보상 모드
    }

    [SerializeField] private Image skillIcon;  // 스킬 아이콘 이미지
    [SerializeField] private Image[] starIcons;
    [SerializeField] private GameObject focusImg;
    [SerializeField] private GameObject starsRoot;  // 별 아이콘들의 부모 (숨기기 위해)
    [SerializeField] private ParticleImage starEffect;
    [Space]
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color filledColor;
    [Header("Gold Card")]
    [SerializeField] private string goldIconAddress = "ItemIcon_Coin_Gold";  // 골드 아이콘 Addressable 주소
    private Sprite goldIconSprite;  // 로드된 골드 아이콘 스프라이트
    private AsyncOperationHandle<Sprite> goldIconHandle;

    private LevelUpRewardController levelUpRewardController;
    private PassiveSkillManager passiveSkillManager;
    private int currentSkillId = -1;
    private bool isSelected = false;
    private CardMode currentMode = CardMode.PassiveSkill;
    private int goldAmount = 0;

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
    public CardMode CurrentMode => currentMode;

    /// <summary>
    /// 패시브 스킬 카드로 설정
    /// </summary>
    public void SetAsPassiveSkillCard(int skillId)
    {
        currentMode = CardMode.PassiveSkill;
        currentSkillId = skillId;
        goldAmount = 0;
        ShowStars(true);
        UpdateSkillUI();
        SetFocus(false);
    }

    /// <summary>
    /// 기존 메서드 호환성 유지 (SetPassiveSkillId -> SetAsPassiveSkillCard)
    /// </summary>
    public void SetPassiveSkillId(int skillId)
    {
        SetAsPassiveSkillCard(skillId);
    }

    /// <summary>
    /// 플레이어 스킬 카드로 설정
    /// </summary>
    public void SetAsPlayerSkillCard(int skillId)
    {
        currentMode = CardMode.PlayerSkill;
        currentSkillId = skillId;
        goldAmount = 0;
        ShowStars(false);
        UpdatePlayerSkillUI();
        SetFocus(false);
    }

    /// <summary>
    /// 기존 메서드 호환성 유지 (SetPlayerSkillId -> SetAsPlayerSkillCard)
    /// </summary>
    public void SetPlayerSkillId(int skillId)
    {
        SetAsPlayerSkillCard(skillId);
    }

    /// <summary>
    /// 골드 카드로 설정
    /// </summary>
    public void SetAsGoldCard(int amount)
    {
        currentMode = CardMode.Gold;
        currentSkillId = -1;
        goldAmount = amount;
        ShowStars(false);
        UpdateGoldCardUI();
        SetFocus(false);
    }

    private void UpdateGoldCardUI()
    {
        // 텍스트를 골드 금액으로 표시
        if (text != null)
            text.text = $"{goldAmount}G";

        // 아이콘을 골드 이미지로 변경
        if (skillIcon != null)
        {
            // 캐시된 스프라이트가 있으면 즉시 사용
            if (goldIconSprite != null)
            {
                skillIcon.sprite = goldIconSprite;
            }
            else
            {
                // 캐시에 없으면 로드
                LoadGoldIconAsync().Forget();
            }
        }
    }

    private async UniTaskVoid LoadGoldIconAsync()
    {
        // 이미 로드 중이거나 로드 완료된 경우
        if (goldIconHandle.IsValid())
        {
            if (goldIconHandle.Status == AsyncOperationStatus.Succeeded)
            {
                goldIconSprite = goldIconHandle.Result;
                if (skillIcon != null)
                    skillIcon.sprite = goldIconSprite;
            }
            return;
        }

        // 캐시에서 먼저 확인
        var cachedSprite = AddressablePreloader.Instance?.GetCachedSprite(goldIconAddress);
        if (cachedSprite != null)
        {
            goldIconSprite = cachedSprite;
            if (skillIcon != null)
                skillIcon.sprite = goldIconSprite;
            return;
        }

        // Addressable로 로드
        goldIconHandle = Addressables.LoadAssetAsync<Sprite>(goldIconAddress);
        var sprite = await goldIconHandle.ToUniTask();

        if (goldIconHandle.Status == AsyncOperationStatus.Succeeded)
        {
            goldIconSprite = sprite;
            if (skillIcon != null)
                skillIcon.sprite = goldIconSprite;
        }
    }

    private void ShowStars(bool show)
    {
        if (starsRoot != null)
        {
            starsRoot.SetActive(show);
        }
        else if (starIcons != null)
        {
            foreach (var star in starIcons)
            {
                if (star != null)
                    star.gameObject.SetActive(show);
            }
        }
    }

    private void UpdatePlayerSkillUI()
    {
        if (currentSkillId == -1) return;

        SkillData skillData = DataTableManager.SkillTable.Get(currentSkillId);
        if (skillData == null)
        {
            return;
        }

        // 스킬 이름 설정 (StringTable에서 가져오기)
        if (text != null)
        {
            if (int.TryParse(skillData.SKILL_NAME, out int nameId))
                text.text = DataTableManager.StringTable.Get(nameId);
            else
                text.text = skillData.SKILL_NAME;
        }

        // PlayerSkill은 별 레벨이 없으므로 별 UI 숨김 (0개)
        UpdateStarUI(0);

        // 스킬 아이콘 로드 (캐시 우선)
        LoadSkillIcon(skillData.SKILL_ICON);
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

    /// <summary>
    /// 다음 별을 색칠하는 애니메이션 (스킬 레벨업용)
    /// </summary>
    /// <returns>색칠할 별이 있으면 true, 이미 최대 레벨이면 false</returns>
    public bool FillNextStar()
    {
        if (starIcons == null || starIcons.Length == 0) return false;

        // 현재 스킬 레벨 계산 (색칠된 별 개수)
        int currentStarLevel = GetStarLevelFromSkillId(currentSkillId);
        int currentFilledStars = currentStarLevel - 1; // 0-based index

        // 이미 최대 레벨 (3성)이면 색칠할 별이 없음
        if (currentFilledStars >= starIcons.Length)
            return false;

        // 다음 별을 색칠 (애니메이션 효과)
        int nextStarIndex = currentFilledStars;
        if (nextStarIndex < starIcons.Length)
        {
            // 별 색칠 애니메이션
            starIcons[nextStarIndex].transform.DOKill();

            // 파티클 이펙트를 별 위치로 이동 및 재생
            if (starEffect != null)
            {
                // RectTransform 위치 설정
                if (starEffect.transform is RectTransform starEffectRect && starIcons[nextStarIndex].transform is RectTransform starRect)
                {
                    starEffectRect.position = starRect.position;
                }
                else
                {
                    starEffect.transform.position = starIcons[nextStarIndex].transform.position;
                }

                starEffect.gameObject.SetActive(true);

                // 기존 재생 중인 파티클 정지 후 재생
                starEffect.Play();
            }

            // 시퀀스로 스케일과 색칠 동시에 처리
            Sequence starSequence = DOTween.Sequence();
            starSequence.SetUpdate(true);

            // 색칠과 동시에 커지기 시작
            starSequence.AppendCallback(() => SetIconColor(starIcons[nextStarIndex], filledColor));
            starSequence.Join(starIcons[nextStarIndex].transform.DOScale(1.5f, 0.2f).SetEase(Ease.OutQuad));

            // 원래 크기로 돌아오기
            starSequence.Append(starIcons[nextStarIndex].transform.DOScale(1f, 0.15f).SetEase(Ease.InQuad));

            return true;
        }

        return false;
    }

    public void SetLevelUpRewardController(LevelUpRewardController levelUpRewardController)
    {
        this.levelUpRewardController = levelUpRewardController;
    }

    // 카드 클릭 시 선택
    public void OnCardClicked()
    {
        // LevelUpRewardController가 있는 경우
        if (levelUpRewardController != null)
        {
            // 이미 선택 중이면 무시
            if (!levelUpRewardController.CanSelectSkillCard())
                return;

            // 테두리 활성화
            SetFocus(true);
            isSelected = true;

            // 골드 카드 모드일 때
            if (currentMode == CardMode.Gold)
            {
                levelUpRewardController.OnGoldCardAcquired(this, goldAmount);
                return;
            }

            // 스킬 카드 모드일 때
            bool success = ApplySkill();
            if (success)
            {
                // 즉시 획득 처리 (이 카드 제외하고 나머지 카드 비활성화)
                levelUpRewardController.OnSkillCardAcquired(this);
            }
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

    private void UpdateSkillUI()
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

        // 이펙트 이름 설정 (StringTable에서 가져오기)
        if (text != null)
        {
            if (int.TryParse(effectData.EFFECT_NAME, out int nameId))
                text.text = DataTableManager.StringTable.Get(nameId);
            else
                text.text = effectData.EFFECT_NAME;
        }

        int starLevel = GetStarLevelFromSkillId(currentSkillId);
        UpdateStarUI(starLevel - 1);

        // 스킬 아이콘 로드 (캐시 우선)
        LoadSkillIcon(skillData.SKILL_ICON);
    }

    private void LoadSkillIcon(string iconAddress)
    {
        if (string.IsNullOrEmpty(iconAddress))
        {
            return;
        }

        // 공백 및 특수문자 제거
        iconAddress = iconAddress.Trim();

        // 캐시된 스프라이트가 있으면 즉시 사용
        var cachedSprite = AddressablePreloader.Instance.GetCachedSprite(iconAddress);
        if (cachedSprite != null)
        {
            if (skillIcon != null)
            {
                skillIcon.sprite = cachedSprite;
            }
            return;
        }

        // 캐시에 없으면 비동기 로드 (폴백)
        LoadSkillIconAsync(iconAddress).Forget();
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
        if (goldIconHandle.IsValid())
        {
            Addressables.Release(goldIconHandle);
        }
    }
}