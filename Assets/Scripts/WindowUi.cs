using UnityEngine;
using UnityEngine.UI;

public class WindowUI : MonoBehaviour
{
    [SerializeField] private GameObject pricePanel;
    [SerializeField] private Button confirmButton;

    private bool isActive = false;

    private void Awake()
    {
        pricePanel.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirm);
    }

    public void Show()
    {
        if (isActive)
        {
            return;
        }

        isActive = true;
        pricePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void OnConfirm()
    {
        pricePanel.SetActive(false);
        Time.timeScale = 1f;
        isActive = false;
    }
}
