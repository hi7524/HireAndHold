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

    private UnitInfoUI mainUI;
    private bool isInitialized = false;

    private static bool globalInitialized = false;

    private void Awake()
    {
        if (globalInitialized)
        {
            return;
        }

        Initialize();
        globalInitialized = true;
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }


        if (alertRoot != null && !alertRoot.activeSelf)
        {
            Debug.Log(" alertRoot 비활성화 ");
        }
        else if (alertRoot != null)
        {
            alertRoot.SetActive(false);
        }

        if (successRoot != null && !successRoot.activeSelf)
        {
            Debug.Log(" successRoot 비활성화 )");
        }
        else if (successRoot != null)
        {
            successRoot.SetActive(false);
        }

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
                mainUI?.RefreshUI();
            });
        }

        mainUI = GetComponentInParent<UnitInfoUI>();
        isInitialized = true;
    }

    public void ShowAlert(string message)
    {

        // 초기화 안됐으면 지금 초기화
        if (!isInitialized)
        {

            Initialize();
        }

        if (alertMessage != null)
        {
            alertMessage.text = message;
        }
        else
        {
            Debug.LogError("[UIPopupManager] alertMessage가 null!");
        }

        if (alertRoot != null)
        {
            Transform parent = alertRoot.transform.parent;


            // UnitInfoUI의 mainRoot 찾기
            UnitInfoUI unitInfoUI = GetComponentInParent<UnitInfoUI>();
            if (unitInfoUI != null)
            {
                // mainRoot가 private이므로 alertRoot의 최상위 부모로 추측
                Transform root = alertRoot.transform;
                while (root.parent != null && root.parent.GetComponent<UnitInfoUI>() == null)
                {
                    root = root.parent;
                }

            }

            alertRoot.SetActive(true);

            // Canvas 체크
            Canvas canvas = alertRoot.GetComponentInParent<Canvas>();

        }
    }

    public void ShowSuccess(string title, string detail)
    {

        // 초기화 안됐으면 지금 초기화
        if (!isInitialized)
        {

            Initialize();
        }

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
