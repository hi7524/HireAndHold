using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Cysharp.Threading.Tasks;
using GameData;

/// <summary>
/// 업적 상세보기 팝업 컨트롤러
/// </summary>
public class AchievementDetailController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject detailPanel;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressSlider;

    [Header("Reward Display")]
    [SerializeField] private GameObject rewardArea;
    [SerializeField] private Image rewardIconImage;
    [SerializeField] private TextMeshProUGUI rewardNameText;
    [SerializeField] private TextMeshProUGUI rewardAmountText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button claimButton;
    [SerializeField] private TextMeshProUGUI claimButtonText;

    [Header("Reward Icons")]
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite diamondIcon;
    [SerializeField] private Sprite enhanceStoneIcon;
    [SerializeField] private Sprite ticketIcon;
    [SerializeField] private Sprite defaultIcon;

    private AchievementData currentAchievement;
    private AchievementProgress currentProgress;
    private Action onRewardClaimed;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimClicked);
    }

    public void Show(AchievementData data, AchievementProgress progress, Action onClaimed = null)
    {
        currentAchievement = data;
        currentProgress = progress;
        onRewardClaimed = onClaimed;

        UpdateUI();

        if (detailPanel != null)
            detailPanel.SetActive(true);
    }

    public void Close()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);

        currentAchievement = null;
        currentProgress = null;
    }

    private void UpdateUI()
    {
        if (currentAchievement == null) return;

        int currentValue = currentProgress?.currentValue ?? 0;
        int targetValue = currentAchievement.Condition_Value;
        bool isCompleted = currentProgress?.isCompleted ?? false;
        bool isRewarded = currentProgress?.isRewarded ?? false;

        // 제목
        if (titleText != null)
        {
            string title = GetLocalizedText(currentAchievement.Achievement_Name);
            titleText.text = title;
        }

        // 설명
        if (descriptionText != null)
        {
            string desc = GetLocalizedText(currentAchievement.Achievement_Desc);
            descriptionText.text = desc;
        }

        // 진행도
        if (progressText != null)
        {
            progressText.text = $"진행도: {currentValue} / {targetValue}";
        }

        if (progressSlider != null)
        {
            progressSlider.maxValue = targetValue;
            progressSlider.value = Mathf.Min(currentValue, targetValue);
        }

        // 보상 표시
        UpdateRewardDisplay();

        // 수령 버튼 상태
        UpdateClaimButton();
    }

    private void UpdateRewardDisplay()
    {
        if (currentAchievement == null) return;

        if (rewardArea != null)
            rewardArea.SetActive(true);

        Sprite icon = defaultIcon;
        string rewardName = "보상";

        // 보상 타입에 따른 아이콘 및 이름
        if (currentAchievement.Reward_Type == 1)
        {
            icon = goldIcon;
            rewardName = "골드";
        }
        else if (currentAchievement.Reward_Type == 2)
        {
            switch (currentAchievement.Reward_ID)
            {
                case 5102:
                    icon = diamondIcon;
                    rewardName = "다이아";
                    break;
                case 5103:
                    icon = ticketIcon;
                    rewardName = "소환 티켓";
                    break;
                case 5201:
                    icon = enhanceStoneIcon;
                    rewardName = "강화석";
                    break;
                default:
                    var itemData = DataTableManager.ItemTable?.Get(currentAchievement.Reward_ID);
                    if (itemData != null)
                    {
                        // 아이템 이름 (StringTable에서)
                        rewardName = DataTableManager.GetString(itemData.ITEM_NAME) ?? $"아이템 {currentAchievement.Reward_ID}";

                        // 아이템 아이콘 (비동기 로드)
                        LoadItemIconAsync(itemData.ITEM_ICON).Forget();
                    }
                    else
                    {
                        rewardName = $"아이템 {currentAchievement.Reward_ID}";
                    }
                    break;
            }
        }

        if (rewardIconImage != null && icon != null)
            rewardIconImage.sprite = icon;

        if (rewardNameText != null)
            rewardNameText.text = rewardName;

        if (rewardAmountText != null)
            rewardAmountText.text = $"x{FormatNumber(currentAchievement.Reward_Value)}";
    }

    private async UniTaskVoid LoadItemIconAsync(string iconAddress)
    {
        if (string.IsNullOrEmpty(iconAddress) || iconAddress == "폴더 경로") return;
        if (rewardIconImage == null) return;

        var loadedIcon = await SpriteCache.Instance.LoadSpriteAsync(iconAddress);
        if (loadedIcon != null)
            rewardIconImage.sprite = loadedIcon;
    }

    private void UpdateClaimButton()
    {
        if (claimButton == null) return;

        bool isCompleted = currentProgress?.isCompleted ?? false;
        bool isRewarded = currentProgress?.isRewarded ?? false;

        bool canClaim = isCompleted && !isRewarded;

        claimButton.gameObject.SetActive(true);
        claimButton.interactable = canClaim;

        if (claimButtonText != null)
        {
            if (isRewarded)
                claimButtonText.text = "수령 완료";
            else if (isCompleted)
                claimButtonText.text = "수령";
            else
                claimButtonText.text = "진행 중";
        }
    }

    private async void OnClaimClicked()
    {
        if (currentAchievement == null) return;

        if (claimButton != null)
            claimButton.interactable = false;

        bool success = await AchievementManager.ClaimRewardAsync(currentAchievement.Achievements_ID);

        if (success)
        {
            if (currentProgress == null)
                currentProgress = new AchievementProgress(currentAchievement.Achievements_ID);

            currentProgress.isRewarded = true;
            UpdateClaimButton();
            onRewardClaimed?.Invoke();
        }
        else
        {
            if (claimButton != null)
                claimButton.interactable = true;
        }
    }

    private string GetLocalizedText(int textId)
    {
        // StringTable에서 텍스트 가져오기
        var text = DataTableManager.GetString(textId);
        return text ?? $"업적 {textId}";
    }

    private string FormatNumber(int number)
    {
        if (number >= 1000000)
            return $"{number / 1000000f:F1}M";
        if (number >= 1000)
            return $"{number / 1000f:F1}K";
        return number.ToString();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (claimButton != null)
            claimButton.onClick.RemoveListener(OnClaimClicked);
    }
}
