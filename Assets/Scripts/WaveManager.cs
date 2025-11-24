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

        Debug.Log($"[WaveManager] 스테이지 {stageId}: 총 {TotalWaves}개 웨이브 로드");


        RegisterWaveEvents();
    }


    private void RegisterWaveEvents()
    {
        foreach (var wave in currentStageWaves)
        {
            if (wave.WAVE_TYPE == 2)
            {
                int warningTime = wave.WAVE_START_T - 5; // 5초 전
                if (warningTime >= 0)
                {
                    int warnMinutes = warningTime / 60;
                    int warnSeconds = warningTime % 60;

                    // 클로저 캡처 방지
                    WaveData currentWave = wave;

                    var warningEvent = gameManager.AddTimeEvent(warnMinutes, warnSeconds, () =>
                    {
                        Debug.Log($"[WaveManager] 워닝 패널 표시! 시간: {warnMinutes}:{warnSeconds}");
                        stageUiManager.ShowWarningPanel();
                    });
                    registeredEvents.Add(warningEvent);
                }
            }

            // 웨이브 시작 이벤트 등록
            int startMinutes = wave.WAVE_START_T / 60;
            int startSeconds = wave.WAVE_START_T % 60;

            WaveData startWave = wave;

            var startEvent = gameManager.AddTimeEvent(startMinutes, startSeconds, () =>
            {
                StartWave(startWave);
            });
            registeredEvents.Add(startEvent);

            // 웨이브 종료 이벤트 등록
            int endMinutes = wave.WAVE_END_T / 60;
            int endSeconds = wave.WAVE_END_T % 60;

            WaveData endWave = wave;

            var endEvent = gameManager.AddTimeEvent(endMinutes, endSeconds, () =>
            {
                Debug.Log($"[WaveManager] 🔔 웨이브 종료 이벤트 실행! Wave {endWave.WAVE_NUM}, Type: {endWave.WAVE_TYPE}, 시간: {endMinutes}:{endSeconds}");

                // WAVE_TYPE 2면 워닝 타임 종료 처리
                if (endWave.WAVE_TYPE == 2)
                {
                    Debug.Log($"[WaveManager] 워닝 타임 종료 감지! Wave {endWave.WAVE_NUM}");
                    OnWarningTimeEnd(endWave);
                }

                EndWave(endWave);
            });
            registeredEvents.Add(endEvent);
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
        Debug.Log($"[Wave {wave.WAVE_NUM}] 종료");

        completedWaves++;
        OnWaveComplete?.Invoke(wave.WAVE_NUM);

        // 모든 웨이브 완료 체크
        if (completedWaves >= TotalWaves)
        {
            Debug.Log("[WaveManager] 모든 웨이브 완료!");
            OnAllWavesComplete?.Invoke();

            // StageManager에게 스테이지 클리어 알림
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

        Debug.Log($"[WaveManager] 몬스터 ID {monsterId} - {count}마리를 {interval:F2}초 간격으로 스폰");

        // 일정 간격으로 스폰
        for (int i = 0; i < count; i++)
        {
            float spawnDelay = i * interval;
            int spawnMinutes = (int)(startTime + spawnDelay) / 60;
            int spawnSeconds = (int)(startTime + spawnDelay) % 60;

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
            stageUiManager.ShowRewardPanel();
        }
    }

    private void SpawnBoss(WaveData wave)
    {
        Debug.Log($"[WaveManager] 중간보스 웨이브 {wave.WAVE_NUM} - 보스 스폰!");
        gameManager.IsBoss = true;
        int bossId = wave.SPAWN_MON1_ID;
        if (bossId <= 0)
        {
            Debug.LogError("[WaveManager] 보스 ID가 없습니다!");
            return;
        }
        Monster boss = monsterSpawner.SpawnBossById(bossId);

        if (boss != null)
        {
            MonsterData bossData = DataTableManager.MonsterTable.Get(bossId);
            string bossName = bossData?.MON_NAME ?? "중간보스";
            stageUiManager.ShowBossHealthBar(boss, bossName);
            monsterSpawner.WaitForBossDeath(boss, () => OnBossDeath(wave));
        }
    }
    private void OnBossDeath(WaveData wave)
    {
        Debug.Log($"[WaveManager] 중간보스 처치! Wave {wave.WAVE_NUM} ");
        gameManager.IsBoss = false;
        stageUiManager.HideBossHealthBar();
        if (stageUiManager != null)
        {
            stageUiManager.ShowRewardPanel();
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
}
