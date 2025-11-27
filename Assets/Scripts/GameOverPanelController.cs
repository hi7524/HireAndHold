using UnityEngine;
using TMPro;

public class GameOverPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject panelRoot;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI expRewardText;
    [SerializeField] private TextMeshProUGUI goldRewardText;
    [SerializeField] private TextMeshProUGUI messageText;
    
    public void Show(int expReward, int goldReward)
    {
        SetData(expReward, goldReward);
        panelRoot.SetActive(true);
        gameManager?.PauseGame();
    }
    
    private void SetData(int expReward, int goldReward)
    {
        if (titleText != null)
            titleText.text = "스테이지 실패";
        
        if (messageText != null)
            messageText.text = "추가 아이템";
        
        if (expRewardText != null)
            expRewardText.text = $"경험치+{expReward:N0}";
        
        if (goldRewardText != null)
            goldRewardText.text = $"골드+{goldReward:N0}";
    }
    
    public void OnConfirmButtonClick()
    {
        
        panelRoot.SetActive(false);
        gameManager?.ResumeGame();
    }
}
