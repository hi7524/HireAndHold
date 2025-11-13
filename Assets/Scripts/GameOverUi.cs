using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUi : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    private void Start()
    {
        gameOverPanel.SetActive(false);

        restartButton.onClick.AddListener(Restart);
    }

    public void GameOver()
    {
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void Restart()
    {
        gameOverPanel.SetActive(false);
        SceneManager.LoadSceneAsync("DevScene_Lobby");
    }
}
