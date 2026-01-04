using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;

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

    private bool isInitialized;
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

        if (alertRoot != null)
            alertRoot.SetActive(false);

        // ⭐ TutorialTarget 호환: RemoveListener + AddListener 패턴
        if (alertOk != null)
        {
            alertOk.onClick.RemoveListener(OnAlertOkClicked);
            alertOk.onClick.AddListener(OnAlertOkClicked);
        }

        if (successRoot != null)
            successRoot.SetActive(false);

        if (successOk != null)
        {
            successOk.onClick.RemoveListener(OnSuccessOkClicked);
            successOk.onClick.AddListener(OnSuccessOkClicked);
        }

        if (confirmRoot != null)
            confirmRoot.SetActive(false);

        if (confirmYes != null)
        {
            confirmYes.onClick.RemoveListener(OnConfirmYesClicked);
            confirmYes.onClick.AddListener(OnConfirmYesClicked);
        }

        if (confirmNo != null)
        {
            confirmNo.onClick.RemoveListener(OnConfirmNoClicked);
            confirmNo.onClick.AddListener(OnConfirmNoClicked);
        }

        isInitialized = true;
    }


    public void ShowAlert(string message)
    {
        _ = ShowAlertAsync(message, null);
    }

    public void ShowAlert(string message, Action onOk)
    {
        _ = ShowAlertAsync(message, onOk);
    }

    public async UniTask ShowAlertAsync(string message, Action onOk = null)
    {
        if (!isInitialized)
            Initialize();

        Debug.Log($"[UIPopupManager] ShowAlertAsync: {message}");

        currentAlertCallback = onOk;

        if (alertMessage != null)
            alertMessage.text = message;

        if (alertRoot != null)
        {
            alertRoot.SetActive(true);
            ForceImmediateUIUpdate(alertRoot);
        }

        await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
    }

    private void OnAlertOkClicked()
    {
        Debug.Log("[UIPopupManager] OnAlertOkClicked");

        if (alertRoot != null)
            alertRoot.SetActive(false);

        currentAlertCallback?.Invoke();
        currentAlertCallback = null;
    }

    public void ShowSuccess(string title, string detail)
    {
        _ = ShowSuccessAsync(title, detail);
    }

    public async UniTask ShowSuccessAsync(string title, string detail)
    {
        if (!isInitialized)
            Initialize();

        Debug.Log($"[UIPopupManager] ShowSuccessAsync: {title} - {detail}");

        if (successTitle != null)
            successTitle.text = title;

        if (successDetail != null)
            successDetail.text = detail;

        if (successRoot != null)
        {
            successRoot.SetActive(true);
            ForceImmediateUIUpdate(successRoot);
        }

        await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
    }

    // ⭐ 분리된 메서드로 변경
    private void OnSuccessOkClicked()
    {
        Debug.Log("[UIPopupManager] OnSuccessOkClicked");

        if (successRoot != null)
            successRoot.SetActive(false);
    }


    public void ShowConfirm(string title, string message, Action onConfirm, Action onCancel = null)
    {
        _ = ShowConfirmAsync(title, message, onConfirm, onCancel);
    }

    public async UniTask ShowConfirmAsync(
        string title,
        string message,
        Action onConfirm,
        Action onCancel = null)
    {
        if (!isInitialized)
            Initialize();

        Debug.Log($"[UIPopupManager] ShowConfirmAsync: {title} - {message}");

        if (confirmRoot == null)
        {
            await ShowAlertAsync($"{title}\n{message}", onConfirm);
            return;
        }

        currentConfirmCallback = onConfirm;

        if (confirmTitle != null)
            confirmTitle.text = title;

        if (confirmMessage != null)
            confirmMessage.text = message;

        confirmRoot.SetActive(true);
        ForceImmediateUIUpdate(confirmRoot);

        await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
    }

    private void OnConfirmYesClicked()
    {
        Debug.Log("[UIPopupManager] OnConfirmYesClicked");

        if (confirmRoot != null)
            confirmRoot.SetActive(false);

        currentConfirmCallback?.Invoke();
        currentConfirmCallback = null;
    }

    private void OnConfirmNoClicked()
    {
        Debug.Log("[UIPopupManager] OnConfirmNoClicked");

        if (confirmRoot != null)
            confirmRoot.SetActive(false);

        currentConfirmCallback = null;
    }


    private static void ForceImmediateUIUpdate(GameObject root)
    {
        if (root == null)
            return;

        Canvas.ForceUpdateCanvases();

        var rect = root.GetComponent<RectTransform>();
        if (rect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }
}
