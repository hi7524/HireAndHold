using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class UIPopupManager : MonoBehaviour
{
    [Header("Alert Popup")]
    [SerializeField] private GameObject alertRoot;
    [SerializeField] private TextMeshProUGUI alertMessage;
    [SerializeField] private Button alertOk;

    [Header("Success Popup")]
    [SerializeField] private GameObject successRoot;
    [SerializeField] private TextMeshProUGUI successTitle;
    [SerializeField] private TextMeshProUGUI successDetail;
    [SerializeField] private Button successOk;

    [Header("Confirm Popup")]
    [SerializeField] private GameObject confirmRoot;
    [SerializeField] private TextMeshProUGUI confirmTitle;
    [SerializeField] private TextMeshProUGUI confirmMessage;
    [SerializeField] private Button confirmYes;
    [SerializeField] private Button confirmNo;

    private bool isInitialized = false;
    private Action currentAlertCallback;
    private Action currentConfirmCallback;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        // Alert 초기화
        if (alertRoot != null)
            alertRoot.SetActive(false);

        if (alertOk != null)
        {
            alertOk.onClick.RemoveAllListeners();
            alertOk.onClick.AddListener(OnAlertOkClicked);
        }

        // Success 초기화
        if (successRoot != null)
            successRoot.SetActive(false);

        if (successOk != null)
        {
            successOk.onClick.RemoveAllListeners();
            successOk.onClick.AddListener(() =>
            {
                if (successRoot != null)
                    successRoot.SetActive(false);
            });
        }

        // Confirm 초기화
        if (confirmRoot != null)
            confirmRoot.SetActive(false);

        if (confirmYes != null)
        {
            confirmYes.onClick.RemoveAllListeners();
            confirmYes.onClick.AddListener(OnConfirmYesClicked);
        }

        if (confirmNo != null)
        {
            confirmNo.onClick.RemoveAllListeners();
            confirmNo.onClick.AddListener(OnConfirmNoClicked);
        }

        isInitialized = true;
    }

    /// <summary>
    /// 기본 알림 팝업 (콜백 없음)
    /// </summary>
    public void ShowAlert(string message)
    {
        ShowAlert(message, null);
    }

    /// <summary>
    /// 알림 팝업 (콜백 있음)
    /// </summary>
    public void ShowAlert(string message, Action onOk)
    {
        if (!isInitialized)
            Initialize();

        currentAlertCallback = onOk;

        if (alertMessage != null)
            alertMessage.text = message;

        if (alertRoot != null)
            alertRoot.SetActive(true);
    }

    /// <summary>
    /// 성공 팝업
    /// </summary>
    public void ShowSuccess(string title, string detail)
    {
        if (!isInitialized)
            Initialize();

        if (successTitle != null)
            successTitle.text = title;

        if (successDetail != null)
            successDetail.text = detail;

        if (successRoot != null)
        {
            successRoot.SetActive(true);
        }
    }

    /// <summary>
    /// 확인 팝업 (예/아니오)
    /// </summary>
    public void ShowConfirm(string title, string message, Action onConfirm, Action onCancel = null)
    {
        if (!isInitialized)
            Initialize();

        // Confirm 팝업이 설정되지 않았으면 Alert로 대체
        if (confirmRoot == null)
        {
            Debug.LogWarning("[UIPopupManager] Confirm 팝업이 설정되지 않았습니다. Alert로 대체합니다.");
            ShowAlert($"{title}\n{message}\n다시 한번 클릭하세요.", onConfirm);
            return;
        }

        currentConfirmCallback = onConfirm;

        if (confirmTitle != null)
            confirmTitle.text = title;

        if (confirmMessage != null)
            confirmMessage.text = message;

        if (confirmRoot != null)
            confirmRoot.SetActive(true);
    }

    private void OnAlertOkClicked()
    {
        if (alertRoot != null)
            alertRoot.SetActive(false);

        // 콜백 실행
        currentAlertCallback?.Invoke();
        currentAlertCallback = null;
    }

    private void OnConfirmYesClicked()
    {
        if (confirmRoot != null)
            confirmRoot.SetActive(false);

        // 확인 콜백 실행
        currentConfirmCallback?.Invoke();
        currentConfirmCallback = null;
    }

    private void OnConfirmNoClicked()
    {
        if (confirmRoot != null)
            confirmRoot.SetActive(false);

        // 취소 콜백은 없음 (필요시 추가 가능)
        currentConfirmCallback = null;
    }
}
