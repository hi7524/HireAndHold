using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

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
    [SerializeField] private Button nextStageButton;

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
        if (nextStageButton != null)
        {
            nextStageButton.onClick.AddListener(OnNextStageButtonClick);
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
        
        Debug.Log($"[StageClearPanel] 스테이지 {stageKey} 결과 저장 중 (클리어: {isCleared})");
        
        if (isCleared)
        {
            // 성공: 클리어 기록
            bool saveSuccess = await DatabaseManager.Instance.RecordStageClearAsync(
                stageKey,
                exp,
                clearTime,
                stars
            );

            if (saveSuccess)
            {
                // 재화 지급
                await DatabaseManager.Instance.AddGoldAsync(gold);

                // 경험치 지급
                await DatabaseManager.Instance.AddExpAsync(exp);

                // highestStage 갱신 (스테이지 ID로 저장)
                var currentUser = DatabaseManager.Instance.CurrentUser;

                if (currentUser != null && stageId >= currentUser.profile.highestStage)
                {
                    currentUser.profile.highestStage = stageId + 1;  // 다음 스테이지 ID 저장
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
        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveListener(OnNextStageButtonClick);
        }
    }

    private async void OnConfirmButtonClick()
    {
        Hide();
        Time.timeScale = 1f;

        // 클리어 데이터 저장
        await SaveStageResultAsync(
            currentStageId,
            true,
            currentStars,
            currentGold,
            currentExp,
            currentClearTime
        );

        // 로비로 이동 (로딩씬 없이)
        await Addressables.LoadSceneAsync("Lobby");
    }

    private async void OnNextStageButtonClick()
    {
        Hide();
        Time.timeScale = 1f;

        // 다음 스테이지로 설정
        int nextStageId = currentStageId + 1;
        PageSnap.SelectedStageId = nextStageId;

        LoadingRequest request = new LoadingRequest("Stage");

        request.AddTask("클리어 데이터 저장", async (ct) =>
        {
            await SaveStageResultAsync(
                currentStageId,
                true,
                currentStars,
                currentGold,
                currentExp,
                currentClearTime
            );
        }, weight: 2f);

        await LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }
}
