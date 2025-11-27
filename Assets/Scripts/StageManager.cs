using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public int CurrentStageId { get; private set; }
    public StageData CurrentStageData { get; private set; }
    public int TotalMonsters { get; private set; }
    public int RemainingMonsters { get; private set; }
    
    private float accumulatedGold = 0f;
    private float accumulatedAccountExp = 0f;

    public event Action<int> OnStageStart;
    public event Action<int> OnStageComplete;
    public event Action<int> OnStageFailed;
    public event Action<int> OnMonsterCountChanged; // (remaining

    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StageUiManager stageUiManager;
    [SerializeField] private MonsterSpawner monsterSpawner;

    private async UniTaskVoid Start()
    {
        waveManager.Initialize(gameManager, this);
        if (!DataTableManager.IsInitialized)
        {
            await DataTableManager.InitAsync();
        }
        CurrentStageId = 701;
        gameManager.OnGameStart += () => StartStage(CurrentStageId);
        
        // MonsterSpawner 이벤트 구독
        if (monsterSpawner != null)
        {
            monsterSpawner.OnMonsterDeath += OnMonsterKilled;

        }
    }


    public void StartStage(int stageId)
    {

        CurrentStageData = DataTableManager.StageTable.Get(stageId);

        if (CurrentStageData == null)
        {
            Debug.LogError($"[StageManager] 스테이지 {stageId} 데이터를 찾을 수 없습니다!");
            return;
        }

        CurrentStageId = stageId;
        Debug.Log($"[StageManager] 스테이지 {stageId} 시작: {CurrentStageData.STAGE_NAME}");
        Debug.Log($"  - 제한시간: {CurrentStageData.STAGE_TIME_SEC}초");
        Debug.Log($"  - 총 웨이브: {CurrentStageData.TOTAL_WAVE}개");
        Debug.Log($"  - 맵: {CurrentStageData.STAGE_NAME}");

        // 총 적 수 계산
        TotalMonsters = CalculateTotalMonsters(stageId);
        RemainingMonsters = TotalMonsters;
        accumulatedGold = 0f;
        accumulatedAccountExp = 0f;
        Debug.Log($"  - 총 적 수: {TotalMonsters}마리");
        
        OnMonsterCountChanged?.Invoke(RemainingMonsters);
        OnStageStart?.Invoke(stageId);
        waveManager.InitializeWaves(stageId);

    }
    
    // 스테이지의 총 적 수 계산
    private int CalculateTotalMonsters(int stageId)
    {
        var waveTable = DataTableManager.WaveTable;
        var waves = waveTable.GetAll()
            .Where(w => w.STAGE_ID == stageId)
            .ToList();
        
        int totalCount = 0;
        
        foreach (var wave in waves)
        {
            // WAVE_TYPE 3, 4는 보스 (1마리)
            if (wave.WAVE_TYPE == 3 || wave.WAVE_TYPE == 4)
            {
                totalCount += 1;
            }
            else
            {
                // 일반 몬스터
                totalCount += wave.MON1_COUNT;
                totalCount += wave.MON2_COUNT;
            }
        }
        
        return totalCount;
    }
    
    // 적 사망 시 호출
    private void OnMonsterKilled(Enemy enemy)
    {
        if (enemy != null)
        {
            // 몰스터 데이터에서 보상 가져오기
            int monsterId = enemy.MonsterId;
            MonsterData monsterData = DataTableManager.MonsterTable.Get(monsterId);
            
            if (monsterData != null)
            {
                accumulatedGold += monsterData.MON_DROP_GOLD;
                accumulatedAccountExp += monsterData.MON_ACCOUNT_EXP;
                
                Debug.Log($"[StageManager] 몰스터 {monsterId} 사망 - 골드: +{monsterData.MON_DROP_GOLD}, 경험치: +{monsterData.MON_ACCOUNT_EXP}");
                Debug.Log($"[StageManager] 누적 - 골드: {accumulatedGold}, 경험치: {accumulatedAccountExp}");
            }
        }
        
        RemainingMonsters--;
        if (RemainingMonsters < 0) RemainingMonsters = 0;
        
        OnMonsterCountChanged?.Invoke(RemainingMonsters);
    }

    public void CompleteStage()
    {
        int stars = CalculateStars();
        
        // 기본 보상 + 누적 보상 + 별 보상
        int totalGold = CurrentStageData.STAGE_C_GOLD + (int)accumulatedGold;
        int totalExp = CurrentStageData.STAGE_C_EXP + (int)accumulatedAccountExp;
        
        // 별 보상 추가
        int starBonusGold = 0;
        int starBonusExp = 0;
        
        if (stars == 3)
        {
            starBonusGold = CurrentStageData.STAGE_C_3S_ID;
            starBonusExp = (int)CurrentStageData.STAGE_C_3S_CO;
        }
        else if (stars == 2)
        {
            starBonusGold = CurrentStageData.STAGE_C_2S_ID;
            starBonusExp = CurrentStageData.STAGE_C_2S_CO;
        }
        else if (stars == 1)
        {
            starBonusGold = CurrentStageData.STAGE_C_1S_ID;
            starBonusExp = CurrentStageData.STAGE_C_1S_CO;
        }
        
        totalGold += starBonusGold;
        totalExp += starBonusExp;
        
        Debug.Log($"[StageManager] 스테이지 {CurrentStageId} 클리어! ({stars}성)");
        Debug.Log($"  - 기본 보상 경험치: {CurrentStageData.STAGE_C_EXP}");
        Debug.Log($"  - 몰스터 보상 경험치: {(int)accumulatedAccountExp}");
        Debug.Log($"  - 별 보상 경험치: {starBonusExp}");
        Debug.Log($"  - 총 경험치: {totalExp}");
        Debug.Log($"  - 기본 보상 골드: {CurrentStageData.STAGE_C_GOLD}");
        Debug.Log($"  - 몰스터 보상 골드: {(int)accumulatedGold}");
        Debug.Log($"  - 별 보상 골드: {starBonusGold}");
        Debug.Log($"  - 총 골드: {totalGold}");

        OnStageComplete?.Invoke(CurrentStageId);

        // 클리어 패널 표시
        if (stageUiManager != null && CurrentStageData != null)
        {
            stageUiManager.ShowStageClearPanel(
                CurrentStageData.STAGE_NAME,
                totalExp,
                totalGold,
                stars
            );
        }
    }

    private int CalculateStars()
    {
        Wall wall = GameObject.FindWithTag("Wall")?.GetComponent<Wall>();

        if (wall == null)
        {
            return 1;
        }

        float hpRatio = wall.CurrentHp / wall.MaxHp;

        Debug.Log($"[StageManager] Wall 체력 비율: {hpRatio:P0} ({wall.CurrentHp}/{wall.MaxHp})");


        if (hpRatio >= 1f) return 3;
        if (hpRatio >= 0.5f) return 2;
        return 1;
    }

    public void FailStage()
    {
        int totalGold = (int)accumulatedGold;
        int totalExp = (int)accumulatedAccountExp;
        
        Debug.Log($"[StageManager] 스테이지 {CurrentStageId} 실패");
        Debug.Log($"  - 처치 보상 경험치: {totalExp}");
        Debug.Log($"  - 처치 보상 골드: {totalGold}");
        
        OnStageFailed?.Invoke(CurrentStageId);
        
        if (stageUiManager != null)
        {
            stageUiManager.ShowGameOverPanel(totalExp, totalGold);
        }
        
        gameManager.GameEnd();
    }
    public void RestartStage()
    {
        Debug.Log($"[StageManager] 스테이지 {CurrentStageId} 재시작");

        // 현재 씬을 다시 로드
        Time.timeScale = 1f;
        LoadingSceneManager.Instance.LoadSceneWithLoading(
            new LoadingRequest("Stage")
        );
    }
    private void OnDestroy()
    {
        gameManager.OnGameStart -= () => StartStage(CurrentStageId);
        
        if (monsterSpawner != null)
        {
            monsterSpawner.OnMonsterDeath -= OnMonsterKilled;
        }
    }
}
