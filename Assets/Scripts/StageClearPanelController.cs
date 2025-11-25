using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StageClearPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject panelRoot;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private TextMeshProUGUI expRewardText;
    [SerializeField] private TextMeshProUGUI goldRewardText;
    [SerializeField] private Image[] starImages; 
    
    public void Show(string stageName, int expReward, int goldReward, int stars = 3)
    {
        SetData(stageName, expReward, goldReward, stars);
        panelRoot.SetActive(true);
        gameManager?.PauseGame();
    }
    
    
    private void SetData(string stageName, int expReward, int goldReward, int stars)
    {
        if (stageNameText != null)
            stageNameText.text = stageName;
        
        if (expRewardText != null)
            expRewardText.text = $"+{expReward:N0}";
        
        if (goldRewardText != null)
            goldRewardText.text = $"+{goldReward:N0}";
        
        SetStars(stars);
    }
    
   
    private void SetStars(int count)
    {
        if (starImages == null || starImages.Length == 0) return;
        
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].enabled = i < count;
            }
        }
    }
    
    public void Hide()
    {
        panelRoot?.SetActive(false);
    }
}
