using UnityEngine;
using UnityEngine.UI;

public class WindowUI : MonoBehaviour
{
    [SerializeField] private GameObject pricePanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private SkillSelectUi skillSelectUi;
    [SerializeField] private GameManager gameManager;


    private void Awake()
    {
        pricePanel.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirm);
    }

    public void Show()
    {
        pricePanel.SetActive(true);
        gameManager.PauseGame();
    }

    private void OnConfirm()
    {
        pricePanel.SetActive(false);
        skillSelectUi.Show();
    }
}
