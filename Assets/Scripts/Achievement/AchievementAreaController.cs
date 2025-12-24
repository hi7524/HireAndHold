using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GameData;

/// <summary>
/// 업적 패널 컨트롤러
/// PriceArea 하위에 배치하여 업적 목록을 표시
/// </summary>
public class AchievementAreaController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject achievementAreaPanel;
    [SerializeField] private Transform achievementListContent;
    [SerializeField] private GameObject achievementElementPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button claimAllButton;
    [SerializeField] private TextMeshProUGUI claimAllButtonText;

    [Header("Detail Panel")]
    [SerializeField] private AchievementDetailController achievementDetailController;

    [Header("Badge")]
    [SerializeField] private GameObject claimableBadge;
    [SerializeField] private TextMeshProUGUI claimableCountText;

    private List<AchievementElementUI> achievementElements = new List<AchievementElementUI>();

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (claimAllButton != null)
            claimAllButton.onClick.AddListener(OnClaimAllClicked);
    }

    private void OnEnable()
    {
        // 업적 이벤트 구독
        AchievementManager.OnAchievementCompleted += OnAchievementChanged;
        AchievementManager.OnAchievementProgressChanged += OnProgressChanged;
        AchievementManager.OnAchievementRewardClaimed += OnAchievementChanged;

        if (achievementAreaPanel != null)
            achievementAreaPanel.SetActive(true);

        RefreshAchievementList();
    }

    private void OnDisable()
    {
        AchievementManager.OnAchievementCompleted -= OnAchievementChanged;
        AchievementManager.OnAchievementProgressChanged -= OnProgressChanged;
        AchievementManager.OnAchievementRewardClaimed -= OnAchievementChanged;
    }

    public void Open()
    {
        if (achievementAreaPanel != null)
            achievementAreaPanel.SetActive(true);

        RefreshAchievementList();
    }

    public void Close()
    {
        if (achievementDetailController != null)
            achievementDetailController.Close();

        gameObject.SetActive(false);
    }

    private void OnAchievementChanged(int achievementId)
    {
        RefreshAchievementList();
    }

    private void OnProgressChanged(int achievementId, int currentValue)
    {
        // 특정 요소만 업데이트
        foreach (var element in achievementElements)
        {
            if (element != null && element.AchievementId == achievementId)
            {
                element.UpdateProgress(currentValue);
                break;
            }
        }
        UpdateClaimAllButton();
        UpdateBadge();
    }

    public void RefreshAchievementList()
    {
        if (DataTableManager.AchievementTable == null) return;

        ClearAchievementElements();

        // UI에 노출되는 업적만 가져오기
        var achievements = DataTableManager.AchievementTable.GetExposedAchievements();

        // 정렬: 수령가능 > 진행중 > 완료(수령됨)
        var sortedAchievements = achievements
            .Select(data => new { Data = data, Progress = AchievementManager.GetProgress(data.Achievements_ID) })
            .OrderBy(x => GetSortOrder(x.Progress))
            .ThenBy(x => x.Data.Sort_Order)
            .Select(x => x.Data);

        foreach (var achievementData in sortedAchievements)
        {
            CreateAchievementElement(achievementData);
        }

        UpdateClaimAllButton();
        UpdateBadge();
    }

    /// <summary>
    /// 정렬 순서: 0=수령가능, 1=진행중, 2=완료(수령됨)
    /// </summary>
    private int GetSortOrder(AchievementProgress progress)
    {
        if (progress == null)
            return 1; // 진행중 (아직 시작 안함)

        if (progress.isRewarded)
            return 2; // 완료(수령됨) - 제일 아래

        if (progress.isCompleted)
            return 0; // 수령가능 - 제일 위

        return 1; // 진행중
    }

    private void CreateAchievementElement(AchievementData data)
    {
        if (achievementElementPrefab == null || achievementListContent == null) return;

        var go = Instantiate(achievementElementPrefab, achievementListContent);
        var element = go.GetComponent<AchievementElementUI>();

        if (element != null)
        {
            var progress = AchievementManager.GetProgress(data.Achievements_ID);
            element.Setup(data, progress, OnAchievementClicked);
            achievementElements.Add(element);
        }
    }

    private void ClearAchievementElements()
    {
        foreach (var element in achievementElements)
        {
            if (element != null && element.gameObject != null)
                DestroyImmediate(element.gameObject);
        }
        achievementElements.Clear();
    }

    private void OnAchievementClicked(AchievementData data)
    {
        var progress = AchievementManager.GetProgress(data.Achievements_ID);

        // 완료되었고 보상 미수령이면 바로 수령
        if (progress != null && progress.isCompleted && !progress.isRewarded)
        {
            ClaimRewardAsync(data.Achievements_ID).Forget();
        }
        else if (achievementDetailController != null)
        {
            // 상세 패널 열기
            achievementDetailController.Show(data, progress, OnRewardClaimed);
        }
    }

    private async UniTaskVoid ClaimRewardAsync(int achievementId)
    {
        bool success = await AchievementManager.ClaimRewardAsync(achievementId);
        if (success)
        {
            Debug.Log($"[Achievement] 보상 수령 완료: {achievementId}");
            RefreshAchievementList();
        }
    }

    private void OnRewardClaimed()
    {
        RefreshAchievementList();
    }

    private async void OnClaimAllClicked()
    {
        if (claimAllButton != null)
            claimAllButton.interactable = false;

        var claimable = AchievementManager.GetClaimableAchievements();
        int claimedCount = 0;

        foreach (var achievement in claimable)
        {
            bool success = await AchievementManager.ClaimRewardAsync(achievement.Achievements_ID);
            if (success) claimedCount++;
        }

        if (claimedCount > 0)
        {
            Debug.Log($"[Achievement] {claimedCount}개 업적 보상 일괄 수령 완료");
        }

        await UniTask.Yield();
        RefreshAchievementList();

        if (claimAllButton != null)
            claimAllButton.interactable = true;
    }

    private void UpdateClaimAllButton()
    {
        int claimableCount = AchievementManager.GetClaimableCount();

        if (claimAllButton != null)
            claimAllButton.interactable = claimableCount > 0;

        if (claimAllButtonText != null)
            claimAllButtonText.text = claimableCount > 0 ? $"일괄 수령 ({claimableCount})" : "일괄 수령";
    }

    private void UpdateBadge()
    {
        int claimableCount = AchievementManager.GetClaimableCount();

        if (claimableBadge != null)
            claimableBadge.SetActive(claimableCount > 0);

        if (claimableCountText != null)
            claimableCountText.text = claimableCount > 99 ? "99+" : claimableCount.ToString();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (claimAllButton != null)
            claimAllButton.onClick.RemoveListener(OnClaimAllClicked);
    }
}
