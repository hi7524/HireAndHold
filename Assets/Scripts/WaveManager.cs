using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public int CurrentWaveNum { get; private set; }
    public int TotalWaves { get; private set; }

    public event Action<int> OnWaveStart;
    public event Action<int> OnWaveComplete;
    public event Action OnAllWavesComplete;

    private GameManager gameManager;
    private StageManager stageManager;
    private List<WaveData> currentStageWaves;
    private List<TimeEvent> registeredEvents = new List<TimeEvent>();
    private int completedWaves = 0;
    private HashSet<int> completedWaveNums = new HashSet<int>(); // 중복 방지
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private StageUiManager stageUiManager;


    public void Initialize(GameManager gameManager, StageManager stageManager)
    {
        this.gameManager = gameManager;
        this.stageManager = stageManager;
    }


    public void InitializeWaves(int stageId)
    {

        ClearAllEvents();


        var waveTable = DataTableManager.WaveTable;
        currentStageWaves = waveTable.GetAll()
            .Where(w => w.STAGE_ID == stageId)
            .OrderBy(w => w.WAVE_START_T)
            .ToList();

        TotalWaves = currentStageWaves.Count;
        CurrentWaveNum = 0;
        completedWaves = 0;
        completedWaveNums.Clear(); // 초기화

        Debug.Log($"[WaveManager] 스테이지 {stageId}: 총 {TotalWaves}개 웨이브 로드");


        RegisterWaveEvents();
    }


    private void RegisterWaveEvents()
    {
        foreach (var wave in currentStageWaves)
        {
            // if (wave.WAVE_TYPE == 2)
            // {
            //     int warningTime = wave.WAVE_START_T - 5; // 5초 전
            //     if (warningTime >= 0)
            //     {
            //         int warnMinutes = warningTime / 60;
            //         int warnSeconds = warningTime % 60;

            //         // 클로저 캡처 방지
            //         WaveData currentWave = wave;

            //         var warningEvent = gameManager.AddTimeEvent(warnMinutes, warnSeconds, () =>
            //         {
            //             Debug.Log($"[WaveManager] 워닝 패널 표시! 시간: {warnMinutes}:{warnSeconds}");
            //             stageUiManager.ShowWarningPanel();
            //         });
            //         registeredEvents.Add(warningEvent);
            //     }
            // }

            // 웨이브 시작 이벤트 등록
            int startMinutes = wave.WAVE_START_T / 60;
            int startSeconds = wave.WAVE_START_T % 60;

            WaveData startWave = wave;

            var startEvent = gameManager.AddTimeEvent(startMinutes, startSeconds, () =>
            {
                StartWave(startWave);
            });
            registeredEvents.Add(startEvent);

            // 웨이브 종료 이벤트 등록 (보스 웨이브는 제외 - 보스 사망 시 처리)
            if (wave.WAVE_TYPE != 3 && wave.WAVE_TYPE != 4)
            {
                int endMinutes = wave.WAVE_END_T / 60;
                int endSeconds = wave.WAVE_END_T % 60;

                WaveData endWave = wave;

                var endEvent = gameManager.AddTimeEvent(endMinutes, endSeconds, () =>
                {
                    
                    // WAVE_TYPE 2면 워닝 타임 종료 처리
                    if (endWave.WAVE_TYPE == 2)
                    {
                        OnWarningTimeEnd(endWave);
                    }

                    EndWave(endWave);
                });
                registeredEvents.Add(endEvent);
            }
        }

        Debug.Log($"[WaveManager] {registeredEvents.Count}개의 웨이브 이벤트 등록 완료");
    }


    private void StartWave(WaveData wave)
    {
        CurrentWaveNum = wave.WAVE_NUM;
        OnWaveStart?.Invoke(wave.WAVE_NUM);
        SpawnWaveMonsters(wave);
    }


    private void EndWave(WaveData wave)
    {
        
        if (completedWaveNums.Contains(wave.WAVE_NUM))
        {
           
            return;
        }
        
        Debug.Log($"[Wave {wave.WAVE_NUM}] 종료");
        
        completedWaveNums.Add(wave.WAVE_NUM);
        completedWaves++;
        OnWaveComplete?.Invoke(wave.WAVE_NUM);

        if (completedWaves >= TotalWaves)
        {
            OnAllWavesComplete?.Invoke();


            stageManager?.CompleteStage();
        }
    }

    private void SpawnWaveMonsters(WaveData wave)
    {
        // WAVE_TYPE 3 = 중간보스
        if (wave.WAVE_TYPE == 3 || wave.WAVE_TYPE == 4)
        {
            SpawnBoss(wave);
            return;
        }

        // 일반 몬스터 스폰
        if (wave.SPAWN_MON1_ID > 0 && wave.MON1_COUNT > 0)
        {
            SpawnMonsters(wave.SPAWN_MON1_ID, wave.MON1_COUNT, wave.WAVE_SPEED, wave.WAVE_START_T, wave.WAVE_END_T);
        }

        if (wave.SPAWN_MON2_ID > 0 && wave.MON2_COUNT > 0)
        {
            SpawnMonsters(wave.SPAWN_MON2_ID, wave.MON2_COUNT, wave.WAVE_SPEED, wave.WAVE_START_T, wave.WAVE_END_T);
        }
    }


    private void SpawnMonsters(int monsterId, int count, float waveSpeed, int startTime, int endTime)
    {
        float duration = endTime - startTime;
        float interval = duration / count / waveSpeed;

        Debug.Log($"[WaveManager] 몬스터 ID {monsterId} - {count}마리를 {interval:F2}초 간격으로 스폰 (시작: {startTime}초, 종료: {endTime}초)");


        for (int i = 0; i < count; i++)
        {
            float spawnDelay = i * interval;
            float actualSpawnTime = startTime + spawnDelay;


            if (actualSpawnTime >= endTime)
            {
                Debug.LogWarning($"[WaveManager] 몬스터 {monsterId} - {i+1}번째 스폰이 종료 시간({endTime}초)을 초과하여 스킵됨 (예정 시간: {actualSpawnTime:F2}초)");
                continue;
            }

            int spawnMinutes = (int)actualSpawnTime / 60;
            int spawnSeconds = (int)actualSpawnTime % 60;

            // 클로저 캡처 방지
            int currentMonsterId = monsterId;

            var spawnEvent = gameManager.AddTimeEvent(spawnMinutes, spawnSeconds, () =>
            {
                SpawnSingleMonster(currentMonsterId);
            });
            registeredEvents.Add(spawnEvent);
        }
    }


    private void SpawnSingleMonster(int monsterId)
    {
        Debug.Log($"[WaveManager] 몬스터 {monsterId} 스폰!");

        monsterSpawner.SpawnMonsterById(monsterId);
    }

    private void OnWarningTimeEnd(WaveData wave)
    {
        // 모든 몬스터 제거
        if (monsterSpawner != null)
        {
            Debug.Log($"[WaveManager] MonsterSpawner.KillAllMonsters() 호출");
            monsterSpawner.KillAllMonsters();
        }
        // 보상 패널 표시
        if (stageUiManager != null)
        {
            Debug.Log($"[WaveManager] ShowRewardPanel() 호출");
            stageUiManager.ShowWarningReward();
        }

    }

    private void SpawnBoss(WaveData wave)
{
    bool isFinalBoss = wave.WAVE_TYPE == 4;
    string bossType = isFinalBoss ? "최종보스" : "중간보스";
    
    Debug.Log($"[WaveManager] {bossType} 웨이브 {wave.WAVE_NUM} - 보스 스폰!");
    gameManager.IsBoss = true;
    
    int bossId = wave.SPAWN_MON1_ID;
    if (bossId <= 0)
    {
        Debug.LogError($"[WaveManager] {bossType} ID가 없습니다!");
        return;
    }
    
    Enemy boss = monsterSpawner.SpawnBossById(bossId);

    if (boss != null)
    {
        MonsterData bossData = DataTableManager.MonsterTable.Get(bossId);
        string bossName = bossData?.MON_NAME ?? bossType;
        
       
            stageUiManager.ShowBossHealthBar(boss, bossName);
        
        
        monsterSpawner.WaitForBossDeath(boss, () => OnBossDeath(wave, isFinalBoss));
    }
}

private void OnBossDeath(WaveData wave, bool isFinalBoss)
{
    string bossType = isFinalBoss ? "최종보스" : "중간보스";
    Debug.Log($"[WaveManager] {bossType} 처치! Wave {wave.WAVE_NUM}");
    
    gameManager.IsBoss = false;
    stageUiManager.HideBossHealthBar();
    
    if (isFinalBoss)
    {
        
        
        EndWave(wave);

    }
    else
    {
        
        if (stageUiManager != null)
        {
            stageUiManager.ShowBossRewardPanel();
        }
        
        EndWave(wave);
    }
}
    private void ClearAllEvents()
    {
        if (gameManager == null) return;

        foreach (var timeEvent in registeredEvents)
        {
            gameManager.RemoveTimeEvent(timeEvent);
        }
        registeredEvents.Clear();
    }

    private void OnDestroy()
    {
        ClearAllEvents();
    }

    // 치트: 중간보스 즉시 소환
    public void CheatSpawnBoss()
    {
        // 중간보스 웨이브 찾기 (WAVE_TYPE == 3)
        WaveData bossWave = currentStageWaves?.FirstOrDefault(w => w.WAVE_TYPE == 3);
        
        if (bossWave == null)
        {
            Debug.LogWarning("[WaveManager] 중간보스 웨이브를 찾을 수 없습니다!");
            return;
        }

        // 기존 몬스터 제거
        if (monsterSpawner != null)
        {
            monsterSpawner.KillAllMonsters();
        }

        Debug.Log($"[WaveManager] 치트: 중간보스 소환! Wave {bossWave.WAVE_NUM}");
        
        // 현재 웨이브를 보스 웨이브로 설정
        CurrentWaveNum = bossWave.WAVE_NUM;
        OnWaveStart?.Invoke(bossWave.WAVE_NUM);
        
        // 보스 스폰
        SpawnBoss(bossWave);
    }
}
