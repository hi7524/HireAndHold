using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private bool isInitialized = false;

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

        if (successRoot != null)
            successRoot.SetActive(false);

        if (alertOk != null)
        {
            alertOk.onClick.RemoveAllListeners();
            alertOk.onClick.AddListener(() =>
            {
                if (alertRoot != null)
                    alertRoot.SetActive(false);
            });
        }

        if (successOk != null)
        {
            successOk.onClick.RemoveAllListeners();
            successOk.onClick.AddListener(() =>
            {
                if (successRoot != null)
                    successRoot.SetActive(false);
            });
        }

        isInitialized = true;
    }

    public void ShowAlert(string message)
    {
        if (!isInitialized)
            Initialize();

        if (alertMessage != null)
            alertMessage.text = message;

        if (alertRoot != null)
            alertRoot.SetActive(true);
    }

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
}
