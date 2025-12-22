using Cysharp.Threading.Tasks;
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

    private void Start()
    {
        alertRoot.SetActive(false);
        successRoot.SetActive(false);

        alertOk.onClick.AddListener(() => alertRoot.SetActive(false));
        successOk.onClick.AddListener(() =>
        {
            successRoot.SetActive(false);
            mainUI?.RefreshUI();
        });

        mainUI = GetComponentInParent<UnitInfoUI>();
    }

    public void ShowAlert(string message)
    {
        alertMessage.text = message;
        alertRoot.SetActive(true);
    }

    public void ShowSuccess(string title, string detail)
    {
        successTitle.text = title;
        successDetail.text = detail;
        successRoot.SetActive(true);
    }
}
