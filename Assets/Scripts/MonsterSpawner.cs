using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    public static MonsterSpawner Instance { get; private set; }

    public event Action<Enemy> OnMonsterDeath;

    [SerializeField] private ObjectPoolManager poolManager;
    [SerializeField] private string monsterKey = "Monster";
    [SerializeField] private string bossKey = "BossMonster";
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float horizontalRange = 2f;

    [Header("Enemy Scene References")]
    [SerializeField] private Transform wallTransform;
    [SerializeField] private ExperienceCollector expCollector;

    [Header("Screen Bounds")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float screenPaddingPixels = 150f;

    [Header("Spawn Distribution")]
    [SerializeField] private float minSpawnDistance = 0.5f;
    [SerializeField] private int maxRetryCount = 5;

    private List<Enemy> activeMonsters = new List<Enemy>();
    private float lastSpawnX;

    private void Awake()
    {
        Instance = this;

        // Enemy에서 사용할 씬 참조 설정
        Enemy.SetSceneReferences(wallTransform, expCollector);
    }

    private void OnDestroy()
    {
        Enemy.ClearSceneReferences();
    }

    /// <summary>
    /// 현재 활성화된 모든 몬스터 리스트 반환 (읽기 전용)
    /// </summary>
    public IReadOnlyList<Enemy> GetActiveMonsters() => activeMonsters;

    public void SpawnMonsterById(int monsterId, bool isBoss = false, float hpMultiplier = 1f, float expMultiplier = 1f, float speedMultiplier = 1f)
    {
        MonsterData data = DataTableManager.MonsterTable.Get(monsterId);
        if (data == null)
        {
            Debug.LogError($"몬스터 ID {monsterId} 없음!");
            return;
        }

        string key = isBoss ? "BossMonster" : "Monster";
        GameObject monsterObj = poolManager.Get(key);
        if (monsterObj == null) return;

        Vector3 spawnPos = spawnPoint.position;
        spawnPos.x = GetDistributedSpawnX(spawnPos);

        monsterObj.transform.position = spawnPos;

        Enemy monster = monsterObj.GetComponent<Enemy>();
        monster.transform.position = spawnPos;
        monster.InitializeWithData(poolManager, key, data, isBoss, hpMultiplier, expMultiplier, speedMultiplier);

        // Enemy 사망 이벤트 구독
        monster.OnDeath += OnMonsterRemoved;

        if (!activeMonsters.Contains(monster))
        {
            activeMonsters.Add(monster);
        }

        // 캐시에서 MON_MODEL 동기 로드
        if (!string.IsNullOrEmpty(data.MON_MODEL))
        {
            monster.LoadVisual(data.MON_MODEL);
        }
    }

    public void OnMonsterRemoved(Enemy monster)
    {
        if (activeMonsters.Contains(monster))
        {
            activeMonsters.Remove(monster);

            // 이벤트 구독 해제
            monster.OnDeath -= OnMonsterRemoved;
            OnMonsterDeath?.Invoke(monster);
        }
    }
    // 모든 활성 몬스터 즉시 제거
    public void KillAllMonsters()
    {
        var monstersToKill = new List<Enemy>(activeMonsters);

        foreach (var monster in monstersToKill)
        {
            if (monster != null && monster.gameObject.activeSelf)
            {

                monster.TakeDamage(999999f);
            }
        }
    }

    // 모든 몬스터 제거 후 사망 애니메이션 완료까지 대기
    public async UniTask KillAllMonstersAsync()
    {
        var monstersToKill = new List<Enemy>(activeMonsters);

        if (monstersToKill.Count == 0)
            return;

        int completedCount = 0;
        int totalCount = monstersToKill.Count;
        var tcs = new UniTaskCompletionSource();

        foreach (var monster in monstersToKill)
        {
            // 유효하고 아직 살아있는 몬스터만 처리
            if (monster != null && monster.gameObject.activeSelf && !monster.IsDead)
            {
                monster.OnDeathAnimationComplete += (e) =>
                {
                    completedCount++;
                    if (completedCount >= totalCount)
                    {
                        tcs.TrySetResult();
                    }
                };
                monster.TakeDamage(999999f);
            }
            else
            {
                // 이미 죽었거나 비활성화된 몬스터는 바로 완료 처리
                completedCount++;
                if (completedCount >= totalCount)
                {
                    tcs.TrySetResult();
                }
            }
        }

        // 타임아웃 3초 (안전장치)
        var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(3));
        await UniTask.WhenAny(tcs.Task, timeoutTask);
    }

    // 보스 전용 스폰 (Monster 참조 반환) - 동기 버전
    public Enemy SpawnBossById(int bossId)
    {
        MonsterData data = DataTableManager.MonsterTable.Get(bossId);
        if (data == null)
        {
            return null;
        }

        GameObject bossObj = poolManager.Get(bossKey);
        if (bossObj == null)
        {
            return null;
        }

        Vector3 spawnPos = spawnPoint.position;

        // X축만 SafeArea 안으로 클램프
        spawnPos.x = ClampToSafeAreaX(spawnPos.x, spawnPos);

        bossObj.transform.position = spawnPos;

        Enemy boss = bossObj.GetComponent<Enemy>();
        boss.transform.position = spawnPos;
        boss.InitializeWithData(poolManager, bossKey, data, true); // isBoss = true

        // Enemy 사망 이벤트 구독
        boss.OnDeath += OnMonsterRemoved;

        // 캐시에서 MON_MODEL 동기 로드
        if (!string.IsNullOrEmpty(data.MON_MODEL))
        {
            boss.LoadVisual(data.MON_MODEL);
        }

        if (!activeMonsters.Contains(boss))
        {
            activeMonsters.Add(boss);
        }

        return boss;
    }

    // 보스 사망 대기 (UniTask) - 사망 애니메이션 완료까지 대기
    public void WaitForBossDeath(Enemy boss, Action onDeath)
    {
        WaitForBossDeathAsync(boss, onDeath).Forget();
    }

    private async UniTaskVoid WaitForBossDeathAsync(Enemy boss, Action onDeath)
    {
        if (boss == null)
        {
            onDeath?.Invoke();
            return;
        }

        var tcs = new UniTaskCompletionSource();

        // 사망 애니메이션 완료 이벤트 구독
        boss.OnDeathAnimationComplete += (e) =>
        {
            tcs.TrySetResult();
        };

        // 보스가 죽을 때까지 대기 (예외 처리: 이미 죽어있거나 비활성화된 경우)
        await UniTask.WaitUntil(() => boss == null || !boss.gameObject.activeSelf || boss.IsDead);

        // 이미 비활성화된 경우 바로 콜백 실행
        if (boss == null || !boss.gameObject.activeSelf)
        {
            onDeath?.Invoke();
            return;
        }

        // 사망 애니메이션 완료 대기
        await tcs.Task;

        // 콜백 실행
        onDeath?.Invoke();
    }

    private bool GetSafeAreaScreenBounds(out float minX, out float maxX)
    {
        if (mainCamera == null)
        {
            minX = 0;
            maxX = 0;
            return false;
        }

        Rect safeArea = Screen.safeArea;
        minX = safeArea.xMin + screenPaddingPixels;
        maxX = safeArea.xMax - screenPaddingPixels;
        return true;
    }

    private float ClampToSafeAreaX(float worldX, Vector3 referencePos)
    {
        if (!GetSafeAreaScreenBounds(out float minScreenX, out float maxScreenX))
            return worldX;

        // 참조 위치의 Y, Z를 유지하면서 X만 변경
        Vector3 worldPos = new Vector3(worldX, referencePos.y, referencePos.z);
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        // SafeArea 범위로 클램프
        screenPos.x = Mathf.Clamp(screenPos.x, minScreenX, maxScreenX);

        // 다시 월드 좌표로 변환 (Z 거리 유지)
        Vector3 clampedWorldPos = mainCamera.ScreenToWorldPoint(screenPos);

        return clampedWorldPos.x;
    }

    private float GetDistributedSpawnX(Vector3 referencePos)
    {
        float baseX = spawnPoint.position.x;
        float candidateX = baseX;

        for (int i = 0; i < maxRetryCount; i++)
        {
            candidateX = baseX + UnityEngine.Random.Range(-horizontalRange, horizontalRange);
            candidateX = ClampToSafeAreaX(candidateX, referencePos);

            // 최근 스폰 위치와 충분히 떨어져 있으면 사용
            if (Mathf.Abs(candidateX - lastSpawnX) >= minSpawnDistance)
            {
                break;
            }
        }

        lastSpawnX = candidateX;
        return candidateX;
    }

}
