using UnityEngine;
using UnityEngine.UI;

public class WindowUI : MonoBehaviour
{
    [SerializeField] private GameObject pricePanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private SkillSelectUi skillSelectUi;
    [SerializeField] private GameManager gameManager;



    private void OnEnable()
    {
        pricePanel.SetActive(true);
        gameManager.PauseGame();
        confirmButton.onClick.AddListener(OnConfirm);
    }

    private void OnConfirm()
    {
        pricePanel.SetActive(false);
        skillSelectUi.Show();
    }
    private void OnDisable()
    {
        confirmButton.onClick.RemoveListener(OnConfirm);
        
    }
}
