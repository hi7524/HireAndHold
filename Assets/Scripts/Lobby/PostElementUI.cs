using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using GameData;

public class PostElementUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI expireDateText;
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;

    [Header("State Indicators")]
    [SerializeField] private GameObject unreadIndicator;
    [SerializeField] private GameObject claimedIndicator;
    [SerializeField] private GameObject rewardIcon;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.84f, 0.62f, 0.37f, 0.61f);
    [SerializeField] private Color claimedColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);

    private MailData mailData;
    private Action<MailData> onClickCallback;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClicked);
    }

    public void Setup(MailData mail, Action<MailData> onClick)
    {
        mailData = mail;
        onClickCallback = onClick;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (mailData == null) return;

        // 제목
        if (titleText != null)
            titleText.text = mailData.title ?? "우편";

        // 내용 미리보기
        if (descriptionText != null)
        {
            string preview = mailData.content ?? "";
            if (preview.Length > 30)
                preview = preview.Substring(0, 30) + "...";
            descriptionText.text = preview;
        }

        // 유효기간
        if (expireDateText != null)
        {
            if (mailData.expireAt > 0)
            {
                var expireDate = DateTimeOffset.FromUnixTimeMilliseconds(mailData.expireAt);
                var remaining = expireDate - DateTimeOffset.UtcNow;

                if (remaining.TotalDays >= 1)
                    expireDateText.text = $"{(int)remaining.TotalDays}일 남음";
                else if (remaining.TotalHours >= 1)
                    expireDateText.text = $"{(int)remaining.TotalHours}시간 남음";
                else if (remaining.TotalMinutes >= 1)
                    expireDateText.text = $"{(int)remaining.TotalMinutes}분 남음";
                else
                    expireDateText.text = "곧 만료";
            }
            else
            {
                expireDateText.text = "무기한";
            }
        }

        // 읽지 않음 표시
        if (unreadIndicator != null)
            unreadIndicator.SetActive(!mailData.isRead);

        // 수령 완료 표시
        if (claimedIndicator != null)
            claimedIndicator.SetActive(mailData.isClaimed);

        // 보상 아이콘
        if (rewardIcon != null)
        {
            bool hasReward = mailData.reward != null && mailData.reward.HasReward() && !mailData.isClaimed;
            rewardIcon.SetActive(hasReward);
        }

        // 배경색 (수령 완료 시 연하게)
        if (backgroundImage != null)
        {
            backgroundImage.color = mailData.isClaimed ? claimedColor : normalColor;
        }
    }

    private void OnClicked()
    {
        onClickCallback?.Invoke(mailData);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
    }
}
