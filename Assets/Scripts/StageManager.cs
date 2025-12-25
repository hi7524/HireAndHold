using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Tutorial;

public class StageManager : MonoBehaviour
{
    public int CurrentStageId { get; private set; }
    public StageData CurrentStageData { get; private set; }
    public int TotalMonsters { get; private set; }
    public int RemainingMonsters { get; private set; }

    private float accumulatedGold = 0f;
    private float accumulatedAccountExp = 0f;

    // 스테이지 중 획득한 아이템 누적 (itemId -> count)
    private Dictionary<int, int> accumulatedItems = new Dictionary<int, int>();

    public event Action<int> OnStageStart;
    public event Action<int> OnStageComplete;
    public event Action<int> OnStageFailed;
    public event Action<int> OnMonsterCountChanged; // (remaining

    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StageUiManager stageUiManager;
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private Wall wall;
    [SerializeField] private SpriteRenderer mapSpriteRenderer1;
    [SerializeField] private SpriteRenderer mapSpriteRenderer2;

    private void Awake()
    {
        // CurrentStageId 먼저 설정 (다른 컴포넌트가 Start에서 참조할 수 있도록)
        CurrentStageId = PageSnap.SelectedStageId;
        CurrentStageData = DataTableManager.StageTable.Get(CurrentStageId);
        if (CurrentStageData != null)
        {
            LoadStageMap(CurrentStageData.STAGE_MAP);
        }
    }

    private void Start()
    {
        // 로딩 씬에서 DataTableManager가 이미 초기화됨
        waveManager.Initialize(gameManager, this);
        gameManager.OnGameStart += () => StartStage(CurrentStageId);

        // MonsterSpawner 이벤트 구독
        if (monsterSpawner != null)
        {
            monsterSpawner.OnMonsterDeath += OnMonsterKilled;
        }

        // DropItem 이벤트 구독
        DropItem.OnItemCollected += OnItemCollected;

        // 스테이지 BGM 재생
        PlayStageBGM();
    }

    private void PlayStageBGM()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[StageManager] SoundManager.Instance is null");
            return;
        }

        if (AddressablePreloader.Instance == null)
        {
            Debug.LogWarning("[StageManager] AddressablePreloader.Instance is null");
            return;
        }

        var stageBGM = AddressablePreloader.Instance.GetCachedAudioClip("StageBGM");
        if (stageBGM != null)
        {
            Debug.Log("[StageManager] Playing StageBGM");
            SoundManager.Instance.PlayBGMWithFadeIn(stageBGM, 1f);
        }
        else
        {
            Debug.LogWarning("[StageManager] StageBGM AudioClip not found in cache");
        }
    }

    /// <summary>
    /// 보스 등장 시 BGM을 BossBGM으로 전환
    /// </summary>
    public void PlayBossBGM()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[StageManager] SoundManager.Instance is null (PlayBossBGM)");
            return;
        }

        if (AddressablePreloader.Instance == null)
        {
            Debug.LogWarning("[StageManager] AddressablePreloader.Instance is null (PlayBossBGM)");
            return;
        }

        var bossBGM = AddressablePreloader.Instance.GetCachedAudioClip("BossBGM");
        if (bossBGM != null)
        {
            Debug.Log("[StageManager] Playing BossBGM");
            SoundManager.Instance.StopBGMwithFadeOut(1f);
            // 페이드 아웃 후 보스 BGM 재생
            WaitAndPlayBossBGM(bossBGM).Forget();
        }
        else
        {
            Debug.LogWarning("[StageManager] BossBGM AudioClip not found in cache");
        }
    }

    private async UniTaskVoid WaitAndPlayBossBGM(AudioClip bossBGM)
    {
        await UniTask.Delay(1000); // 1초 페이드 아웃 대기
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGMWithFadeIn(bossBGM, 1f);
        }
    }

    /// <summary>
    /// 보스 처치 후 BGM을 StageBGM으로 복귀
    /// </summary>
    public void RestoreStageBGM()
    {
        if (SoundManager.Instance != null && AddressablePreloader.Instance != null)
        {
            var stageBGM = AddressablePreloader.Instance.GetCachedAudioClip("StageBGM");
            if (stageBGM != null)
            {
                SoundManager.Instance.StopBGMwithFadeOut(1f);
                // 페이드 아웃 후 스테이지 BGM 재생
                WaitAndPlayStageBGM(stageBGM).Forget();
            }
        }
    }

    private async UniTaskVoid WaitAndPlayStageBGM(AudioClip stageBGM)
    {
        await UniTask.Delay(1000); // 1초 페이드 아웃 대기
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGMWithFadeIn(stageBGM, 1f);
        }
    }


    public void StartStage(int stageId)
    {

        CurrentStageData = DataTableManager.StageTable.Get(stageId);

        if (CurrentStageData == null)
        {
            return;
        }

        CurrentStageId = stageId;

        // 맵 스프라이트 적용
        LoadStageMap(CurrentStageData.STAGE_MAP);

        // 총 적 수 계산
        TotalMonsters = CalculateTotalMonsters(stageId);
        RemainingMonsters = TotalMonsters;
        accumulatedGold = 0f;
        accumulatedAccountExp = 0f;
        accumulatedItems.Clear();
        OnMonsterCountChanged?.Invoke(RemainingMonsters);
        OnStageStart?.Invoke(stageId);
        waveManager.InitializeWaves(stageId);

        // 스테이지 시작 튜토리얼 체크
        CheckStageStartTutorialAsync().Forget();
    }

    private void LoadStageMap(string mapKey)
    {
        if (mapSpriteRenderer1 == null)
        {
            Debug.LogWarning("[StageManager] mapSpriteRenderer가 할당되지 않았습니다.");
            return;
        }

        if (string.IsNullOrEmpty(mapKey))
        {
            Debug.LogWarning("[StageManager] 맵 키가 비어있습니다.");
            return;
        }

        Sprite mapSprite = AddressablePreloader.Instance?.GetCachedMap(mapKey);
        if (mapSprite != null)
        {
            mapSpriteRenderer1.sprite = mapSprite;
            mapSpriteRenderer2.sprite = mapSprite;
        }
        else
        {
            Debug.LogWarning($"[StageManager] 맵 스프라이트를 찾을 수 없습니다: {mapKey}");
        }
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

            }
        }

        RemainingMonsters--;
        if (RemainingMonsters < 0) RemainingMonsters = 0;

        OnMonsterCountChanged?.Invoke(RemainingMonsters);
    }

    /// <summary>
    /// 스테이지 중 획득한 아이템 목록 반환
    /// </summary>
    public Dictionary<int, int> GetAccumulatedItems()
    {
        return new Dictionary<int, int>(accumulatedItems);
    }

    /// <summary>
    /// 보너스 골드 추가 (패시브 스킬 골드 카드 등)
    /// 스테이지 클리어 시 총 골드에 포함됨
    /// </summary>
    public void AddBonusGold(int amount)
    {
        if (amount <= 0) return;
        accumulatedGold += amount;
    }

    // 아이템 획득 시 호출
    private void OnItemCollected(DropItem dropItem)
    {
        int itemId = dropItem.ItemId;
        int count = dropItem.ItemCount;

        if (accumulatedItems.ContainsKey(itemId))
            accumulatedItems[itemId] += count;
        else
            accumulatedItems[itemId] = count;
    }

    public void CompleteStage()
    {
        int stars = CalculateStars();

        // 스테이지 클리어 튜토리얼 체크
        CheckStageClearTutorialAsync().Forget();

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
        OnStageComplete?.Invoke(CurrentStageId);

        // 클리어 패널 표시 (아이템 정보 전달 - 로딩 씬에서 DB 저장)
        if (stageUiManager != null && CurrentStageData != null)
        {
            stageUiManager.ShowStageClearPanel(
                CurrentStageData.StringName,
                totalExp,
                totalGold,
                stars,
                accumulatedItems
            );
        }
    }

    private int CalculateStars()
    {
        if (wall == null)
        {
            return 1;
        }

        float hpRatio = wall.CurrentHp / wall.MaxHp;



        if (hpRatio >= 1f) return 3;
        if (hpRatio >= 0.5f) return 2;
        return 1;
    }

    public void FailStage()
    {
        int totalGold = (int)accumulatedGold;
        int totalExp = (int)accumulatedAccountExp;
        OnStageFailed?.Invoke(CurrentStageId);

        if (stageUiManager != null)
        {
            stageUiManager.ShowGameOverPanel(totalExp, totalGold, accumulatedItems);
        }

        gameManager.GameEnd();
    }

    /// <summary>
    /// 디버그용: 스테이지 강제 클리어
    /// </summary>
    public void ForceCompleteStage()
    {
        // 남은 몬스터 수를 0으로 설정
        RemainingMonsters = 0;

        // 클리어 처리
        CompleteStage();
    }

    private void OnDestroy()
    {
        gameManager.OnGameStart -= () => StartStage(CurrentStageId);

        if (monsterSpawner != null)
        {
            monsterSpawner.OnMonsterDeath -= OnMonsterKilled;
        }

        // DropItem 이벤트 해제
        DropItem.OnItemCollected -= OnItemCollected;
    }

    #region 튜토리얼

    /// <summary>
    /// 스테이지 시작 튜토리얼 체크
    /// </summary>
    private async UniTaskVoid CheckStageStartTutorialAsync()
    {
        if (TutorialManager.Instance != null)
        {
            await TutorialManager.Instance.CheckAndStartTutorialAsync(
                TutorialTriggerType.OnStageStart,
                CurrentStageId
            );
        }
    }

    /// <summary>
    /// 스테이지 클리어 튜토리얼 체크
    /// </summary>
    private async UniTaskVoid CheckStageClearTutorialAsync()
    {
        if (TutorialManager.Instance != null)
        {
            await TutorialManager.Instance.CheckAndStartTutorialAsync(
                TutorialTriggerType.OnStageClear,
                CurrentStageId
            );
        }
    }

    #endregion
}
