using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Cysharp.Threading.Tasks;
using GameData;

/// <summary>
/// 퀘스트 상세 정보 패널
/// 퀘스트 클릭 시 상세 정보 표시 및 보상 수령
/// </summary>
public class QuestDetailController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject detailPanel;

    [Header("Quest Info")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressSlider;

    [Header("Quest Type")]
    [SerializeField] private TextMeshProUGUI questTypeText;
    [SerializeField] private GameObject dailyIcon;
    [SerializeField] private GameObject weeklyIcon;

    [Header("Reward")]
    [SerializeField] private Image rewardIconImage;
    [SerializeField] private TextMeshProUGUI rewardAmountText;

    [Header("Buttons")]
    [SerializeField] private Button claimButton;
    [SerializeField] private TextMeshProUGUI claimButtonText;
    [SerializeField] private Button closeButton;

    [Header("Reward Icons")]
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite diamondIcon;
    [SerializeField] private Sprite enhanceStoneIcon;
    [SerializeField] private Sprite ticketIcon;
    [SerializeField] private Sprite defaultIcon;

    private QuestData currentQuest;
    private QuestProgress currentProgress;
    private Action onRewardClaimed;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimClicked);

        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    public void Show(QuestData data, QuestProgress progress, Action onClaimed)
    {
        currentQuest = data;
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

        currentQuest = null;
        currentProgress = null;
        onRewardClaimed = null;
    }

    private void UpdateUI()
    {
        if (currentQuest == null) return;

        int currentValue = currentProgress?.currentValue ?? 0;
        int targetValue = currentQuest.Condition_Value;
        bool isCompleted = currentProgress?.isCompleted ?? false;
        bool isRewarded = currentProgress?.isRewarded ?? false;

        // 제목
        if (titleText != null)
            titleText.text = DataTableManager.GetString(currentQuest.Quest_Name) ?? $"퀘스트 {currentQuest.Quest_ID}";

        // 설명
        if (descriptionText != null)
            descriptionText.text = DataTableManager.GetString(currentQuest.Quest_Desc) ?? "";

        // 퀘스트 타입
        if (questTypeText != null)
            questTypeText.text = currentQuest.IsDaily ? "일일 퀘스트" : "주간 퀘스트";

        if (dailyIcon != null)
            dailyIcon.SetActive(currentQuest.IsDaily);
        if (weeklyIcon != null)
            weeklyIcon.SetActive(currentQuest.IsWeekly);

        // 진행도
        if (progressText != null)
            progressText.text = $"{currentValue} / {targetValue}";

        if (progressSlider != null)
        {
            progressSlider.maxValue = targetValue;
            progressSlider.value = Mathf.Min(currentValue, targetValue);
        }

        // 보상 표시
        UpdateRewardDisplay();

        // 버튼 상태
        UpdateButtonState(isCompleted, isRewarded);
    }

    private void UpdateRewardDisplay()
    {
        if (rewardIconImage == null || currentQuest == null) return;

        Sprite icon = defaultIcon;

        if (currentQuest.Reward_Type == 1)
        {
            icon = goldIcon;
        }
        else if (currentQuest.Reward_Type == 2)
        {
            switch (currentQuest.Reward_ID)
            {
                case 5102:
                    icon = diamondIcon;
                    break;
                case 5103:
                    icon = ticketIcon;
                    break;
                case 5201:
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
            rewardAmountText.text = FormatNumber(currentQuest.Reward_Value);
    }

    private void UpdateButtonState(bool isCompleted, bool isRewarded)
    {
        if (claimButton == null) return;

        if (isRewarded)
        {
            claimButton.interactable = false;
            if (claimButtonText != null)
                claimButtonText.text = "수령 완료";
        }
        else if (isCompleted)
        {
            claimButton.interactable = true;
            if (claimButtonText != null)
                claimButtonText.text = "수령";
        }
        else
        {
            claimButton.interactable = false;
            if (claimButtonText != null)
                claimButtonText.text = "진행 중";
        }
    }

    private void OnClaimClicked()
    {
        if (currentQuest == null) return;
        if (currentProgress == null || !currentProgress.isCompleted || currentProgress.isRewarded) return;

        ClaimRewardAsync().Forget();
    }

    private async UniTaskVoid ClaimRewardAsync()
    {
        if (claimButton != null)
            claimButton.interactable = false;

        bool success = await QuestManager.ClaimRewardAsync(currentQuest.Quest_ID);

        if (success)
        {
            // 진행도 업데이트
            currentProgress = QuestManager.GetProgress(currentQuest.Quest_ID);
            UpdateUI();

            // 콜백 호출
            onRewardClaimed?.Invoke();

            // 패널 닫기
            Close();
        }
        else
        {
            // 실패 시 버튼 다시 활성화
            if (claimButton != null)
                claimButton.interactable = true;
        }
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
