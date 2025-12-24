using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using GameData;

/// <summary>
/// 개별 퀘스트 아이템 UI
/// 퀘스트 목록의 각 항목을 표시
/// </summary>
public class QuestElementUI : MonoBehaviour
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

    [Header("Quest Type Badge")]
    [SerializeField] private GameObject dailyBadge;   // 일일 퀘스트 표시
    [SerializeField] private GameObject weeklyBadge;  // 주간 퀘스트 표시

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

    private QuestData questData;
    private QuestProgress progressData;
    private Action<QuestData> onClickCallback;

    public int QuestId => questData?.Quest_ID ?? 0;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClicked);
    }

    public void Setup(QuestData data, QuestProgress progress, Action<QuestData> onClick)
    {
        questData = data;
        progressData = progress;
        onClickCallback = onClick;

        UpdateUI();
    }

    public void UpdateProgress(int currentValue)
    {
        if (progressData == null)
            progressData = new QuestProgress(questData.Quest_ID);

        progressData.currentValue = currentValue;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (questData == null) return;

        int currentValue = progressData?.currentValue ?? 0;
        int targetValue = questData.Condition_Value;
        bool isCompleted = progressData?.isCompleted ?? false;
        bool isRewarded = progressData?.isRewarded ?? false;

        // 제목
        if (titleText != null)
        {
            titleText.text = questData.Quest_Name;
        }

        // 설명
        if (descriptionText != null)
        {
            descriptionText.text = questData.Quest_Desc;
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

        // 퀘스트 타입 뱃지
        if (dailyBadge != null)
            dailyBadge.SetActive(questData.IsDaily);
        if (weeklyBadge != null)
            weeklyBadge.SetActive(questData.IsWeekly);

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
        if (rewardIconImage == null || questData == null) return;

        Sprite icon = defaultIcon;

        // 보상 타입에 따른 아이콘
        if (questData.Reward_Type == 1)
        {
            // 골드
            icon = goldIcon;
        }
        else if (questData.Reward_Type == 2)
        {
            // 아이템/재화
            switch (questData.Reward_ID)
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
            rewardAmountText.text = FormatNumber(questData.Reward_Value);

        if (rewardIcon != null)
            rewardIcon.SetActive(!(progressData?.isRewarded ?? false));
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
        onClickCallback?.Invoke(questData);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
    }
}
