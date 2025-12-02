using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class StageClearPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
     [SerializeField] private StageManager stageManager;
    [SerializeField] private GameObject panelRoot;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private TextMeshProUGUI expRewardText;
    [SerializeField] private TextMeshProUGUI goldRewardText;
    [SerializeField] private Image[] starImages; 
    [SerializeField] private Button confirmButton;

     private int currentStageId;
    private int currentStars;
    private int currentGold;
    private int currentExp;
    private float currentClearTime;
    
     private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClick);
        }
    }

    public void Show(string stageName, int expReward, int goldReward, int stars = 3)
    {
         if (stageManager != null)
        {
            currentStageId = stageManager.CurrentStageId;
            currentClearTime = gameManager?.ElapsedTime ?? 0f;
        }
        
        currentStars = stars;
        currentGold = goldReward;
        currentExp = expReward;
        
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
    
    private async UniTask SaveStageResultAsync(int stageId, bool isCleared, int stars, int gold, int exp, float clearTime)
    {
        if (DatabaseManager.Instance == null || !DatabaseManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[StageClearPanel] DatabaseManager 없음");
            return;
        }
        
        string stageKey = stageId.ToString();
        int score = exp;
        
        Debug.Log($"[StageClearPanel] 스테이지 {stageKey} 결과 저장 중 (클리어: {isCleared})");
        
        if (isCleared)
        {
            // 성공: 클리어 기록
            bool saveSuccess = await DatabaseManager.Instance.RecordStageClearAsync(
                stageKey,
                score,
                clearTime,
                stars
            );
            
            if (saveSuccess)
            {
                // 재화 지급
                await DatabaseManager.Instance.AddGoldAsync(gold);
                
                // highestStage 갱신
                int clearedStageIndex = stageId - 701;
                var currentUser = DatabaseManager.Instance.CurrentUser;
                
                if (currentUser != null && clearedStageIndex >= currentUser.profile.highestStage)
                {
                    currentUser.profile.highestStage = clearedStageIndex + 1;
                    await DatabaseManager.Instance.SaveProfileAsync();
                    Debug.Log($"[StageClearPanel] 최고 스테이지 갱신: {currentUser.profile.highestStage}");
                }
                
                Debug.Log("[StageClearPanel] 클리어 데이터 저장 완료");
            }
        }
        else
        {
            // 실패: 플레이 카운트만 증가 + 획득한 재화 지급
            var currentUser = DatabaseManager.Instance.CurrentUser;
            if (currentUser != null)
            {
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
                
                Debug.Log($"[StageClearPanel] 실패 데이터 저장 완료 (플레이 횟수: {progress.playCount})");
            }
        }
    }
    
    public void Hide()
    {
        panelRoot?.SetActive(false);
    }
    
    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmButtonClick);
        }
    }

    private async void OnConfirmButtonClick()
    {
        Hide();
        Time.timeScale = 1f;
        
        LoadingRequest request = new LoadingRequest("Lobby");
        
        // 클리어 데이터 저장 Task
        request.AddTask("클리어 데이터 저장", async (ct) =>
        {
            await SaveStageResultAsync(
                currentStageId, 
                true,  // isCleared
                currentStars, 
                currentGold, 
                currentExp, 
                currentClearTime
            );
        }, weight: 1f);
        
        await LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }
}
