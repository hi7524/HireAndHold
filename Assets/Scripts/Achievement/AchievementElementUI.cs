using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using GameData;

/// <summary>
/// 개별 업적 아이템 UI
/// 업적 목록의 각 항목을 표시
/// </summary>
public class AchievementElementUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;

    [Header("State Indicators")]
    [SerializeField] private GameObject claimableIndicator;  // 수령 가능 표시
    [SerializeField] private GameObject completedIndicator;  // 완료 표시
    [SerializeField] private GameObject rewardIcon;

    [Header("Reward Display")]
    [SerializeField] private Image rewardIconImage;
    [SerializeField] private TextMeshProUGUI rewardAmountText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.84f, 0.62f, 0.37f, 1f);
    [SerializeField] private Color claimableColor = new Color(0.4f, 0.8f, 0.4f, 1f);
    [SerializeField] private Color completedColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);

    [Header("Reward Icons")]
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite diamondIcon;
    [SerializeField] private Sprite enhanceStoneIcon;
    [SerializeField] private Sprite ticketIcon;
    [SerializeField] private Sprite defaultIcon;

    private AchievementData achievementData;
    private AchievementProgress progressData;
    private Action<AchievementData> onClickCallback;

    public int AchievementId => achievementData?.Achievements_ID ?? 0;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClicked);
    }

    public void Setup(AchievementData data, AchievementProgress progress, Action<AchievementData> onClick)
    {
        achievementData = data;
        progressData = progress;
        onClickCallback = onClick;

        UpdateUI();
    }

    public void UpdateProgress(int currentValue)
    {
        if (progressData == null)
            progressData = new AchievementProgress(achievementData.Achievements_ID);

        progressData.currentValue = currentValue;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (achievementData == null) return;

        int currentValue = progressData?.currentValue ?? 0;
        int targetValue = achievementData.Condition_Value;
        bool isCompleted = progressData?.isCompleted ?? false;
        bool isRewarded = progressData?.isRewarded ?? false;

        // 제목 (로컬라이징 키 사용 가능)
        if (titleText != null)
        {
            string title = GetLocalizedText(achievementData.Achievement_Name);
            titleText.text = title;
        }

        // 설명
        if (descriptionText != null)
        {
            string desc = GetLocalizedText(achievementData.Achievement_Desc);
            descriptionText.text = desc;
        }

        // 진행도 텍스트
        if (progressText != null)
        {
            progressText.text = $"{currentValue} / {targetValue}";
        }

        // 진행도 슬라이더
        if (progressSlider != null)
        {
            progressSlider.maxValue = targetValue;
            progressSlider.value = Mathf.Min(currentValue, targetValue);
        }

        // 수령 가능 표시
        if (claimableIndicator != null)
            claimableIndicator.SetActive(isCompleted && !isRewarded);

        // 완료(수령됨) 표시
        if (completedIndicator != null)
            completedIndicator.SetActive(isRewarded);

        // 보상 아이콘
        UpdateRewardDisplay();

        // 배경색
        if (backgroundImage != null)
        {
            if (isRewarded)
                backgroundImage.color = completedColor;
            else if (isCompleted)
                backgroundImage.color = claimableColor;
            else
                backgroundImage.color = normalColor;
        }

        // 버튼 상호작용
        if (button != null)
            button.interactable = !isRewarded;
    }

    private void UpdateRewardDisplay()
    {
        if (rewardIconImage == null || achievementData == null) return;

        Sprite icon = defaultIcon;
        string amount = achievementData.Reward_Value.ToString();

        // 보상 타입에 따른 아이콘
        if (achievementData.Reward_Type == 1)
        {
            // 골드
            icon = goldIcon;
        }
        else if (achievementData.Reward_Type == 2)
        {
            // 아이템/재화
            switch (achievementData.Reward_ID)
            {
                case 5102: // 다이아
                    icon = diamondIcon;
                    break;
                case 5103: // 소환 티켓
                    icon = ticketIcon;
                    break;
                case 5201: // 강화석
                    icon = enhanceStoneIcon;
                    break;
                default:
                    icon = defaultIcon;
                    break;
            }
        }

        if (icon != null)
            rewardIconImage.sprite = icon;

        if (rewardAmountText != null)
            rewardAmountText.text = FormatNumber(achievementData.Reward_Value);

        if (rewardIcon != null)
            rewardIcon.SetActive(!(progressData?.isRewarded ?? false));
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

    private void OnClicked()
    {
        onClickCallback?.Invoke(achievementData);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
    }
}
