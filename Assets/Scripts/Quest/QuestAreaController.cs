using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GameData;

/// <summary>
/// 퀘스트 패널 컨트롤러
/// 일일/주간 퀘스트 목록을 표시
/// </summary>
public class QuestAreaController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject questAreaPanel;
    [SerializeField] private GameObject questElementPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button claimAllButton;
    [SerializeField] private TextMeshProUGUI claimAllButtonText;

    [Header("Daily Quest")]
    [SerializeField] private GameObject dailyQuestPanel;
    [SerializeField] private Transform dailyQuestContent;

    [Header("Weekly Quest")]
    [SerializeField] private GameObject weeklyQuestPanel;
    [SerializeField] private Transform weeklyQuestContent;

    [Header("Tab Buttons")]
    [SerializeField] private Button dailyTabButton;
    [SerializeField] private Button weeklyTabButton;
    [SerializeField] private Image dailyTabImage;
    [SerializeField] private Image weeklyTabImage;

    [Header("Tab Colors")]
    [SerializeField] private Color tabSelectedColor = new Color(1f, 0.8f, 0.4f, 1f);
    [SerializeField] private Color tabNormalColor = new Color(0.7f, 0.7f, 0.7f, 1f);


    [Header("Badge")]
    [SerializeField] private GameObject claimableBadge;
    [SerializeField] private TextMeshProUGUI claimableCountText;
    [SerializeField] private GameObject dailyBadge;
    [SerializeField] private TextMeshProUGUI dailyBadgeText;
    [SerializeField] private GameObject weeklyBadge;
    [SerializeField] private TextMeshProUGUI weeklyBadgeText;

    private List<QuestElementUI> dailyQuestElements = new List<QuestElementUI>();
    private List<QuestElementUI> weeklyQuestElements = new List<QuestElementUI>();
    private bool showingDaily = true; // true = 일일, false = 주간

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (claimAllButton != null)
            claimAllButton.onClick.AddListener(OnClaimAllClicked);

        if (dailyTabButton != null)
            dailyTabButton.onClick.AddListener(OnDailyTabClicked);

        if (weeklyTabButton != null)
            weeklyTabButton.onClick.AddListener(OnWeeklyTabClicked);
    }

    private void OnEnable()
    {
        // 퀘스트 이벤트 구독
        QuestManager.OnQuestCompleted += OnQuestChanged;
        QuestManager.OnQuestProgressChanged += OnProgressChanged;
        QuestManager.OnQuestRewardClaimed += OnQuestChanged;
        QuestManager.OnQuestsReset += OnQuestsReset;

        if (questAreaPanel != null)
            questAreaPanel.SetActive(true);

        // 일일 탭으로 초기화
        showingDaily = true;
        UpdateTabUI();

        // 초기화 및 리셋 체크
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        await QuestManager.InitializeAsync();
        RefreshQuestList();
    }

    private void OnDisable()
    {
        QuestManager.OnQuestCompleted -= OnQuestChanged;
        QuestManager.OnQuestProgressChanged -= OnProgressChanged;
        QuestManager.OnQuestRewardClaimed -= OnQuestChanged;
        QuestManager.OnQuestsReset -= OnQuestsReset;
    }

    public void Open()
    {
        if (questAreaPanel != null)
            questAreaPanel.SetActive(true);

        showingDaily = true;
        UpdateTabUI();
        RefreshQuestList();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnDailyTabClicked()
    {
        if (showingDaily) return;
        showingDaily = true;
        UpdateTabUI();
        RefreshQuestList();
    }

    private void OnWeeklyTabClicked()
    {
        if (!showingDaily) return;
        showingDaily = false;
        UpdateTabUI();
        RefreshQuestList();
    }

    private void UpdateTabUI()
    {
        // 탭 버튼 색상 변경
        if (dailyTabImage != null)
            dailyTabImage.color = showingDaily ? tabSelectedColor : tabNormalColor;
        if (weeklyTabImage != null)
            weeklyTabImage.color = showingDaily ? tabNormalColor : tabSelectedColor;

        // 패널 전환
        if (dailyQuestPanel != null)
            dailyQuestPanel.SetActive(showingDaily);
        if (weeklyQuestPanel != null)
            weeklyQuestPanel.SetActive(!showingDaily);
    }

    private void OnQuestChanged(int questId)
    {
        RefreshQuestList();
    }

    private void OnProgressChanged(int questId, int currentValue)
    {
        // 일일 퀘스트에서 찾기
        foreach (var element in dailyQuestElements)
        {
            if (element != null && element.QuestId == questId)
            {
                element.UpdateProgress(currentValue);
                UpdateClaimAllButton();
                UpdateBadges();
                return;
            }
        }
        // 주간 퀘스트에서 찾기
        foreach (var element in weeklyQuestElements)
        {
            if (element != null && element.QuestId == questId)
            {
                element.UpdateProgress(currentValue);
                UpdateClaimAllButton();
                UpdateBadges();
                return;
            }
        }
    }

    private void OnQuestsReset()
    {
        RefreshQuestList();
    }

    public void RefreshQuestList()
    {
        if (DataTableManager.QuestTable == null) return;

        // 일일 퀘스트 갱신
        RefreshDailyQuests();
        // 주간 퀘스트 갱신
        RefreshWeeklyQuests();

        UpdateClaimAllButton();
        UpdateBadges();
    }

    private void RefreshDailyQuests()
    {
        ClearQuestElements(dailyQuestElements);

        var quests = DataTableManager.QuestTable.GetDailyQuests();
        var sortedQuests = quests
            .Select(data => new { Data = data, Progress = QuestManager.GetProgress(data.Quest_ID) })
            .OrderBy(x => GetSortOrder(x.Progress))
            .ThenBy(x => x.Data.Sort_Order)
            .Select(x => x.Data);

        foreach (var questData in sortedQuests)
        {
            CreateQuestElement(questData, dailyQuestContent, dailyQuestElements);
        }
    }

    private void RefreshWeeklyQuests()
    {
        ClearQuestElements(weeklyQuestElements);

        var quests = DataTableManager.QuestTable.GetWeeklyQuests();
        var sortedQuests = quests
            .Select(data => new { Data = data, Progress = QuestManager.GetProgress(data.Quest_ID) })
            .OrderBy(x => GetSortOrder(x.Progress))
            .ThenBy(x => x.Data.Sort_Order)
            .Select(x => x.Data);

        foreach (var questData in sortedQuests)
        {
            CreateQuestElement(questData, weeklyQuestContent, weeklyQuestElements);
        }
    }

    /// <summary>
    /// 정렬 순서: 0=수령가능, 1=진행중, 2=완료(수령됨)
    /// </summary>
    private int GetSortOrder(QuestProgress progress)
    {
        if (progress == null)
            return 1; // 진행중 (아직 시작 안함)

        if (progress.isRewarded)
            return 2; // 완료(수령됨) - 제일 아래

        if (progress.isCompleted)
            return 0; // 수령가능 - 제일 위

        return 1; // 진행중
    }

    private void CreateQuestElement(QuestData data, Transform content, List<QuestElementUI> elementList)
    {
        if (questElementPrefab == null || content == null) return;

        var go = Instantiate(questElementPrefab, content);
        var element = go.GetComponent<QuestElementUI>();

        if (element != null)
        {
            var progress = QuestManager.GetProgress(data.Quest_ID);
            element.Setup(data, progress, OnQuestClicked);
            elementList.Add(element);
        }
    }

    private void ClearQuestElements(List<QuestElementUI> elementList)
    {
        foreach (var element in elementList)
        {
            if (element != null && element.gameObject != null)
                Destroy(element.gameObject);
        }
        elementList.Clear();
    }

    private void OnQuestClicked(QuestData data)
    {
        var progress = QuestManager.GetProgress(data.Quest_ID);

        // 완료되었고 보상 미수령이면 바로 수령
        if (progress != null && progress.isCompleted && !progress.isRewarded)
        {
            ClaimRewardAsync(data.Quest_ID).Forget();
        }
        // 진행 중인 퀘스트는 클릭해도 아무 동작 없음
    }

    private async UniTaskVoid ClaimRewardAsync(int questId)
    {
        bool success = await QuestManager.ClaimRewardAsync(questId);
        if (success)
        {
            RefreshQuestList();
        }
    }


    private void OnClaimAllClicked()
    {
        if (claimAllButton != null)
            claimAllButton.interactable = false;

        // 낙관적 업데이트: 로컬 즉시 처리, Firebase는 백그라운드
        int claimedCount;
        if (showingDaily)
        {
            claimedCount = QuestManager.ClaimAllDailyRewardsOptimistic(out var saveTask);
        }
        else
        {
            claimedCount = QuestManager.ClaimAllWeeklyRewardsOptimistic(out var saveTask);
        }

        if (claimedCount > 0)
        {
            Debug.Log($"[Quest] 일괄 수령 완료: {claimedCount}개");
        }

        // UI 즉시 갱신
        RefreshQuestList();

        if (claimAllButton != null)
            claimAllButton.interactable = true;
    }

    private void UpdateClaimAllButton()
    {
        int claimableCount = showingDaily
            ? QuestManager.GetClaimableDailyCount()
            : QuestManager.GetClaimableWeeklyCount();

        if (claimAllButton != null)
            claimAllButton.interactable = claimableCount > 0;

        if (claimAllButtonText != null)
            claimAllButtonText.text = claimableCount > 0 ? $"일괄 수령 ({claimableCount})" : "일괄 수령";
    }

    private void UpdateBadges()
    {
        int totalClaimable = QuestManager.GetClaimableCount();
        int dailyClaimable = QuestManager.GetClaimableDailyCount();
        int weeklyClaimable = QuestManager.GetClaimableWeeklyCount();

        // 전체 뱃지
        if (claimableBadge != null)
            claimableBadge.SetActive(totalClaimable > 0);
        if (claimableCountText != null)
            claimableCountText.text = totalClaimable > 99 ? "99+" : totalClaimable.ToString();

        // 일일 탭 뱃지
        if (dailyBadge != null)
            dailyBadge.SetActive(dailyClaimable > 0);
        if (dailyBadgeText != null)
            dailyBadgeText.text = dailyClaimable > 99 ? "99+" : dailyClaimable.ToString();

        // 주간 탭 뱃지
        if (weeklyBadge != null)
            weeklyBadge.SetActive(weeklyClaimable > 0);
        if (weeklyBadgeText != null)
            weeklyBadgeText.text = weeklyClaimable > 99 ? "99+" : weeklyClaimable.ToString();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (claimAllButton != null)
            claimAllButton.onClick.RemoveListener(OnClaimAllClicked);

        if (dailyTabButton != null)
            dailyTabButton.onClick.RemoveListener(OnDailyTabClicked);

        if (weeklyTabButton != null)
            weeklyTabButton.onClick.RemoveListener(OnWeeklyTabClicked);
    }
}
