using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameData;

public class PostAreaController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject postAreaPanel;
    [SerializeField] private Transform postListContent;
    [SerializeField] private GameObject postElementPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button claimAllButton;
    [SerializeField] private TextMeshProUGUI claimAllButtonText;

    [Header("Detail Panel")]
    [SerializeField] private PostDetailController postDetailController;

    [Header("Badge")]
    [SerializeField] private GameObject unreadBadge;
    [SerializeField] private TextMeshProUGUI unreadCountText;

    private List<PostElementUI> postElements = new List<PostElementUI>();

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (claimAllButton != null)
            claimAllButton.onClick.AddListener(OnClaimAllClicked);
    }

    private void OnEnable()
    {
        PlayData.OnMailsChanged += RefreshMailList;
        RefreshMailList();
    }

    private void OnDisable()
    {
        PlayData.OnMailsChanged -= RefreshMailList;
    }

    public void Open()
    {
        if (postAreaPanel != null)
            postAreaPanel.SetActive(true);

        RefreshMailList();
    }

    public void Close()
    {
        if (postAreaPanel != null)
            postAreaPanel.SetActive(false);

        if (postDetailController != null)
            postDetailController.Close();
    }

    public void RefreshMailList()
    {
        if (DatabaseManager.Instance == null) return;

        // 기존 요소 정리
        ClearPostElements();

        // 유효한 메일 가져오기 (개인 + 전역 통합)
        var mails = DatabaseManager.Instance.GetAllMailsWithGlobal();

        // 메일 요소 생성
        foreach (var mail in mails)
        {
            CreatePostElement(mail);
        }

        // 일괄 수령 버튼 상태 업데이트
        UpdateClaimAllButton();

        // 뱃지 업데이트
        UpdateBadge();
    }

    private void CreatePostElement(MailData mail)
    {
        if (postElementPrefab == null || postListContent == null) return;

        var go = Instantiate(postElementPrefab, postListContent);
        var element = go.GetComponent<PostElementUI>();

        if (element != null)
        {
            element.Setup(mail, OnMailClicked);
            postElements.Add(element);
        }
    }

    private void ClearPostElements()
    {
        foreach (var element in postElements)
        {
            if (element != null)
                Destroy(element.gameObject);
        }
        postElements.Clear();
    }

    private void OnMailClicked(MailData mail)
    {
        // 읽음 처리
        MarkMailAsRead(mail.mailId).Forget();

        // 상세 패널 열기
        if (postDetailController != null)
        {
            postDetailController.Show(mail, OnRewardClaimed);
        }
    }

    private async UniTaskVoid MarkMailAsRead(string mailId)
    {
        await DatabaseManager.Instance.MarkMailAsReadAsync(mailId);
    }

    private void OnRewardClaimed()
    {
        RefreshMailList();
    }

    private async void OnClaimAllClicked()
    {
        if (claimAllButton != null)
            claimAllButton.interactable = false;

        // 개인 메일 일괄 수령
        int personalClaimedCount = await DatabaseManager.Instance.ClaimAllMailRewardsAsync();

        // 전역 메일 일괄 수령
        int globalClaimedCount = await DatabaseManager.Instance.ClaimAllGlobalMailRewardsAsync();

        int totalClaimed = personalClaimedCount + globalClaimedCount;
        if (totalClaimed > 0)
        {
            Debug.Log($"[PostArea] {totalClaimed}개 메일 보상 수령 완료 (개인:{personalClaimedCount}, 전역:{globalClaimedCount})");
        }

        RefreshMailList();

        if (claimAllButton != null)
            claimAllButton.interactable = true;
    }

    private void UpdateClaimAllButton()
    {
        // 개인 + 전역 통합
        int claimableCount = DatabaseManager.Instance?.GetTotalClaimableMailCount() ?? 0;

        if (claimAllButton != null)
            claimAllButton.interactable = claimableCount > 0;

        if (claimAllButtonText != null)
            claimAllButtonText.text = claimableCount > 0 ? $"일괄 수령 ({claimableCount})" : "일괄 수령";
    }

    private void UpdateBadge()
    {
        // 개인 + 전역 통합
        int unreadCount = DatabaseManager.Instance?.GetTotalUnreadMailCount() ?? 0;

        if (unreadBadge != null)
            unreadBadge.SetActive(unreadCount > 0);

        if (unreadCountText != null)
            unreadCountText.text = unreadCount > 99 ? "99+" : unreadCount.ToString();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (claimAllButton != null)
            claimAllButton.onClick.RemoveListener(OnClaimAllClicked);
    }
}
