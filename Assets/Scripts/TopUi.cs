using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;


public class TopUi : MonoBehaviour
{
    [SerializeField] private GameObject stopPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button speedButton;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI timerText;

    private float[] speedLevels = { 1f, 2f, 3f };
    private int currentIndex = 0;

    private float playTime = 0f;

    private bool isActive = false;

    private void Awake()
    {
        stopPanel.SetActive(false);
        stopButton.onClick.AddListener(OnStop);
        restartButton.onClick.AddListener(OnRestart);
        UpdateSpeed();
        speedButton.onClick.AddListener(ChangeSpeed);
        lobbyButton.onClick.AddListener(OnLobbyButtonClick);
    }

    private void Update()
    {
        if (isActive)
        {
            return;
        }

        playTime += Time.deltaTime;

        int minutes = Mathf.FloorToInt((playTime % 3600) / 60);
        int seconds = Mathf.FloorToInt(playTime % 60);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void OnStop()
    {
        isActive = true;
        stopPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnRestart()
    {
        isActive = false;
        stopPanel.SetActive(false);
        Time.timeScale = speedLevels[currentIndex];
    }

    public int GetCurrentIndex => currentIndex;

    public float[] GetSpeedLevel => speedLevels;

    public void OnSpeed()
    {
        
    }

    private void ChangeSpeed()
    {
        currentIndex = (currentIndex + 1) % speedLevels.Length;
        UpdateSpeed();
    }

    private void UpdateSpeed()
    {
        if (!isActive)
        {
            Time.timeScale = speedLevels[currentIndex];
        }

        speedText.text = $"{speedLevels[currentIndex]}x";
    }

    private async void OnLobbyButtonClick()
    {
 
        await SceneManager.LoadSceneAsync("DevScene_Lobby").ToUniTask();

    }

    private void OnDestroy()
    {
        stopButton.onClick.RemoveListener(OnStop);
        restartButton.onClick.RemoveListener(OnRestart);
        speedButton.onClick.RemoveListener(ChangeSpeed);
        lobbyButton.onClick.RemoveListener(OnLobbyButtonClick);
    }
}
