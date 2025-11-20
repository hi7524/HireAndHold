using UnityEngine;
using UnityEngine.UI;

public class PauseUiBtn : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [Space]
    [SerializeField] private Image icon;
    [SerializeField] private Sprite pauseSprite;
    [SerializeField] private Sprite playSprite;


    public void OnClickBtn()
    {
        // 정지일 경우 재개
        if (gameManager.IsPausedGame)
        {
            icon.sprite = pauseSprite;
            gameManager.ResumeGame();
        }
        // 플레이중일 경우 정지
        else
        {
            icon.sprite = playSprite;
            gameManager.PauseGame();
        }
        
    }
}