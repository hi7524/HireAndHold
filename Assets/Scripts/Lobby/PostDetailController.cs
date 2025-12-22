using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Cysharp.Threading.Tasks;
using GameData;

public class PostDetailController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject detailPanel;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private TextMeshProUGUI expireDateText;

    [Header("Reward Display")]
    [SerializeField] private GameObject rewardArea;
    [SerializeField] private Transform rewardItemContainer;
    [SerializeField] private GameObject rewardItemPrefab;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button claimButton;
    [SerializeField] private TextMeshProUGUI claimButtonText;

    [Header("Reward Icons")]
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite diamondIcon;
    [SerializeField] private Sprite staminaIcon;
    [SerializeField] private Sprite enhanceStoneIcon;
    [SerializeField] private Sprite defaultItemIcon;

    private MailData currentMail;
    private Action onRewardClaimed;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimClicked);
    }

    public void Show(MailData mail, Action onClaimed = null)
    {
        currentMail = mail;
        onRewardClaimed = onClaimed;

        UpdateUI();

        if (detailPanel != null)
            detailPanel.SetActive(true);
    }

    public void Close()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);

        currentMail = null;
    }

    private void UpdateUI()
    {
        if (currentMail == null) return;

        // 제목
        if (titleText != null)
            titleText.text = currentMail.title ?? "우편";

        // 내용
        if (contentText != null)
            contentText.text = currentMail.content ?? "";

        // 유효기간
        if (expireDateText != null)
        {
            if (currentMail.expireAt > 0)
            {
                var expireDate = DateTimeOffset.FromUnixTimeMilliseconds(currentMail.expireAt);
                expireDateText.text = $"유효기간: {expireDate.ToLocalTime():yyyy-MM-dd HH:mm}";
            }
            else
            {
                expireDateText.text = "유효기간: 무기한";
            }
        }

        // 보상 표시
        UpdateRewardDisplay();

        // 수령 버튼 상태
        UpdateClaimButton();
    }

    private void UpdateRewardDisplay()
    {
        // 기존 보상 아이템 제거
        if (rewardItemContainer != null)
        {
            foreach (Transform child in rewardItemContainer)
            {
                Destroy(child.gameObject);
            }
        }

        var reward = currentMail?.reward;
        bool hasReward = reward != null && reward.HasReward();

        if (rewardArea != null)
            rewardArea.SetActive(hasReward);

        if (!hasReward) return;

        // 골드
        if (reward.gold > 0)
            CreateRewardItem(goldIcon, "골드", FormatNumber(reward.gold));

        // 다이아
        if (reward.diamond > 0)
            CreateRewardItem(diamondIcon, "다이아", reward.diamond.ToString());

        // 스태미나
        if (reward.stamina > 0)
            CreateRewardItem(staminaIcon, "스태미나", reward.stamina.ToString());

        // 강화석
        if (reward.enhanceStone > 0)
            CreateRewardItem(enhanceStoneIcon, "강화석", reward.enhanceStone.ToString());

        // 아이템
        if (reward.items != null && reward.items.Count > 0)
        {
            foreach (var item in reward.items)
            {
                var itemData = DataTableManager.ItemTable?.Get(item.Key);
                string itemName = itemData?.ITEM_NAME ?? $"아이템 {item.Key}";
                CreateRewardItem(defaultItemIcon, itemName, item.Value.ToString());
            }
        }
    }

    private void CreateRewardItem(Sprite icon, string itemName, string count)
    {
        if (rewardItemPrefab == null || rewardItemContainer == null) return;

        var go = Instantiate(rewardItemPrefab, rewardItemContainer);

        // 아이콘 설정
        var iconImage = go.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && icon != null)
            iconImage.sprite = icon;

        // 이름 설정
        var nameText = go.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = itemName;

        // 수량 설정
        var countText = go.transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
        if (countText != null)
            countText.text = $"x{count}";
    }

    private void UpdateClaimButton()
    {
        if (claimButton == null) return;

        bool canClaim = currentMail != null &&
                        !currentMail.isClaimed &&
                        !currentMail.IsExpired() &&
                        currentMail.reward != null &&
                        currentMail.reward.HasReward();

        claimButton.gameObject.SetActive(canClaim || !currentMail.isClaimed);
        claimButton.interactable = canClaim;

        if (claimButtonText != null)
        {
            if (currentMail.isClaimed)
                claimButtonText.text = "수령 완료";
            else if (!currentMail.reward.HasReward())
                claimButtonText.text = "확인";
            else
                claimButtonText.text = "수령";
        }
    }

    private async void OnClaimClicked()
    {
        if (currentMail == null) return;

        if (claimButton != null)
            claimButton.interactable = false;

        bool success;

        // 전역 메일인지 확인하고 적절한 메서드 호출
        if (DatabaseManager.Instance.IsGlobalMail(currentMail.mailId))
        {
            success = await DatabaseManager.Instance.ClaimGlobalMailRewardAsync(currentMail.mailId);
        }
        else
        {
            success = await DatabaseManager.Instance.ClaimMailRewardAsync(currentMail.mailId);
        }

        if (success)
        {
            Debug.Log($"[PostDetail] 메일 보상 수령 완료: {currentMail.mailId}");
            currentMail.isClaimed = true;
            UpdateClaimButton();
            onRewardClaimed?.Invoke();
        }
        else
        {
            if (claimButton != null)
                claimButton.interactable = true;
        }
    }

    private string FormatNumber(long number)
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
