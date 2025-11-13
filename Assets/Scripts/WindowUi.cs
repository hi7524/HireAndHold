using UnityEngine;
using UnityEngine.UI;

public class WindowUI : MonoBehaviour
{
    [SerializeField] private GameObject pricePanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private SkillSelectUi skillSelectUi;


    private void Awake()
    {
        pricePanel.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirm);
    }

    public void Show()
    {

        pricePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void OnConfirm()
    {
        pricePanel.SetActive(false);
        Time.timeScale = 0f;
        skillSelectUi.Show();
    }
}
