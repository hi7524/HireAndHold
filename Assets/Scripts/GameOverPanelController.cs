using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class GameOverPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private GameObject panelRoot;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI expRewardText;
    [SerializeField] private TextMeshProUGUI goldRewardText;
    [SerializeField] private TextMeshProUGUI messageText;
     [SerializeField] private Button retryButton;
    [SerializeField] private Button lobbyButton;
    
    private int currentStageId;
    private int currentGold;
    private int currentExp;
    private float currentPlayTime;
    
    private void Awake()
    {
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryButtonClick);
        }
        
        if (lobbyButton != null)
        {
            lobbyButton.onClick.AddListener(OnLobbyButtonClick);
        }
    }

    public void Show(int expReward, int goldReward)
    {
        currentGold = goldReward;
        currentExp = expReward;
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

    private void OnRetryButtonClick()
    {
        Hide();
        Time.timeScale = 1f;
        
        // Stage 씬 재로드 (저장 없음)
        LoadingSceneManager.Instance.LoadSceneWithLoading(
            new LoadingRequest("Stage")
        );
    }

    private async void OnLobbyButtonClick()
    {
        Hide();
        Time.timeScale = 1f;

        LoadingRequest request = new LoadingRequest("Lobby");

        // 실패 데이터 저장 Task
        request.AddTask("플레이 데이터 저장", async (ct) =>
        {
            await SaveStageFailDataAsync(currentStageId, currentGold, currentExp);
        }, weight: 0.5f);

        request.AddTask("유저 데이터 로드", async (ct) =>
        {
            if (DatabaseManager.Instance != null && DatabaseManager.Instance.IsInitialized)
            {
                await DatabaseManager.Instance.LoadUserDataAsync();
                Debug.Log("[GameOverPanel] 유저 데이터 갱신 완료");
            }
        }, weight: 0.5f);

        await LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }
    
    // 실패 데이터 저장
    private async UniTask SaveStageFailDataAsync(int stageId, int gold, int exp)
    {
        if (DatabaseManager.Instance == null || !DatabaseManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[GameOverPanel] DatabaseManager 없음");
            return;
        }
        
        string stageKey = stageId.ToString();
        var currentUser = DatabaseManager.Instance.CurrentUser;
        
        if (currentUser != null)
        {
            Debug.Log($"[GameOverPanel] 스테이지 {stageKey} 실패 데이터 저장 중...");
            
            // 플레이 카운트 증가
            if (!currentUser.stageProgress.TryGetValue(stageKey, out var progress))
            {
                progress = new GameData.StageProgress();
                currentUser.stageProgress[stageKey] = progress;
            }
            
            progress.playCount++;
            
            await DatabaseManager.Instance.SaveStageProgressAsync(stageKey);
            
            // 획득한 재화 지급
            if (gold > 0)
            {
                await DatabaseManager.Instance.AddGoldAsync(gold);
            }
            
            Debug.Log($"[GameOverPanel] 실패 데이터 저장 완료 (플레이 횟수: {progress.playCount}, 골드: +{gold})");
        }
    }
    
    public void Hide()
    {
        panelRoot?.SetActive(false);
    }
    
    private void OnDestroy()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryButtonClick);
        }
        
        if (lobbyButton != null)
        {
            lobbyButton.onClick.RemoveListener(OnLobbyButtonClick);
        }
    }
}
