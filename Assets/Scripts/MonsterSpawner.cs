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

    private List<Enemy> activeMonsters = new List<Enemy>();

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

    public void SpawnMonsterById(int monsterId, bool isBoss = false)
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
        spawnPos.x += UnityEngine.Random.Range(-horizontalRange, horizontalRange);
        monsterObj.transform.position = spawnPos;

        Enemy monster = monsterObj.GetComponent<Enemy>();
        monster.transform.position = spawnPos;
        monster.InitializeWithData(poolManager, key, data, isBoss);

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

    // 보스 사망 대기 (UniTask)
    public void WaitForBossDeath(Enemy boss, Action onDeath)
    {
        WaitForBossDeathAsync(boss, onDeath).Forget();
    }

    private async UniTaskVoid WaitForBossDeathAsync(Enemy boss, Action onDeath)
    {
        // 보스가 죽을 때까지 대기
        await UniTask.WaitUntil(() => boss == null || !boss.gameObject.activeSelf || boss.IsDead);
        // 콜백 실행
        onDeath?.Invoke();
    }

}
