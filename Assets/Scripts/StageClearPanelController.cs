using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using Tutorial;

public class StageClearPanelController : MonoBehaviour
{
    private const int ENHANCE_STONE_ITEM_ID = 5201;

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
    private System.Collections.Generic.Dictionary<int, int> currentItems;
    
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

    public void Show(string stageName, int expReward, int goldReward, int stars = 3, System.Collections.Generic.Dictionary<int, int> items = null)
    {
         if (stageManager != null)
        {
            currentStageId = stageManager.CurrentStageId;
            currentClearTime = gameManager?.ElapsedTime ?? 0f;
        }

        currentStars = stars;
        currentGold = goldReward;
        currentExp = expReward;
        currentItems = items != null ? new System.Collections.Generic.Dictionary<int, int>(items) : null;

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

                // 업적 연동: 스테이지 클리어
                int stageNumber = stageId - 700; // STAGE_ID_START = 701, 스테이지 1번 = 701
                await AchievementManager.UpdateStageMaxClearAsync(stageNumber);

                // 업적 연동: 골드 획득
                if (gold > 0)
                    await AchievementManager.AddGoldGetAsync(gold);

                // 업적 연동: 무피해 클리어 (3성 = 벽 체력 100%)
                if (stars == 3)
                    await AchievementManager.CompleteBarrierNoDamageAsync();

                // 퀘스트 연동: 스테이지 클리어
                await QuestManager.AddStageClearAsync(1);

                // highestStage 갱신 (스테이지 ID로 저장)
                var currentUser = DatabaseManager.Instance.CurrentUser;

                if (currentUser != null && stageId >= currentUser.profile.highestStage)
                {
                    currentUser.profile.highestStage = stageId + 1;  // 다음 스테이지 ID 저장
                    await DatabaseManager.Instance.SaveProfileAsync();
                }

                // 획득 아이템 저장
                await SaveAccumulatedItemsAsync();
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
            }
        }
    }
    
    // 획득 아이템 DB 저장
    private async UniTask SaveAccumulatedItemsAsync()
    {
        Debug.Log($"[StageClear] SaveAccumulatedItemsAsync 시작 - 보유 강화석: {PlayData.EnhanceStone}");

        if (currentItems == null || currentItems.Count == 0)
        {
            Debug.Log("[StageClear] currentItems가 비어있음");
            return;
        }

        Debug.Log($"[StageClear] 저장할 아이템 수: {currentItems.Count}");
        foreach (var item in currentItems)
        {
            int itemId = item.Key;
            int count = item.Value;
            Debug.Log($"[StageClear] 아이템: {itemId} x{count}");

            // 강화석(5201)은 currency.enhanceStone으로 처리
            if (itemId == ENHANCE_STONE_ITEM_ID)
            {
                Debug.Log($"[StageClear] 강화석 저장 시도: {count}개");
                bool success = await DatabaseManager.Instance.AddEnhanceStoneAsync(count);
                Debug.Log($"[StageClear] 강화석 저장 결과: {success}, 저장 후 보유량: {PlayData.EnhanceStone}");
                if (!success)
                {
                    Debug.LogWarning($"  - 강화석 저장 실패: {count}개");
                }
                continue;
            }

            // 일반 아이템
            bool itemSuccess = await DatabaseManager.Instance.AddItemAsync(itemId, count);
            if (!itemSuccess)
            {
                Debug.LogWarning($"  - 아이템 저장 실패: {itemId} x{count}");
            }
        }

        // PlayData 동기화
        PlayData.SyncItemsFromDatabase();
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
        // 튜토리얼에 버튼 터치 알림 (confirmButton이지만 LobbyButton으로 알림)
        TutorialManager.Instance?.NotifyButtonTouched(TutorialButtons.LobbyButton);

        Hide();
        Time.timeScale = 1f;

        LoadingRequest request = new LoadingRequest("Lobby");
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
