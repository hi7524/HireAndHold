using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class MonsterSpawner : MonoBehaviour
{
    public event Action<Enemy> OnMonsterDeath;

    [SerializeField] private ObjectPoolManager poolManager;
    [SerializeField] private string monsterKey = "Monster";
    [SerializeField] private string bossKey = "BossMonster";
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float horizontalRange = 2f;

    private List<Enemy> activeMonsters = new List<Enemy>();

    public void SpawnMonsterById(int monsterId, bool isBoss = false)
    {
        SpawnMonsterByIdAsync(monsterId, isBoss).Forget();
    }

    private async UniTaskVoid SpawnMonsterByIdAsync(int monsterId, bool isBoss = false)
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

        // Addressable로 MON_MODEL 로드 및 적용
        if (!string.IsNullOrEmpty(data.MON_MODEL))
        {
            await monster.LoadVisualAsync(data.MON_MODEL);
        }

        // Enemy 사망 이벤트 구독
        monster.OnDeath += OnMonsterRemoved;

        if (!activeMonsters.Contains(monster))
        {
            activeMonsters.Add(monster);
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

    activeMonsters.Clear();
}

    // 보스 전용 스폰 (Monster 참조 반환) - 비동기 버전
    public async UniTask<Enemy> SpawnBossByIdAsync(int bossId)
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

        // Addressable로 MON_MODEL 로드 및 적용
        if (!string.IsNullOrEmpty(data.MON_MODEL))
        {
            await boss.LoadVisualAsync(data.MON_MODEL);
        }

        // Enemy 사망 이벤트 구독
        boss.OnDeath += OnMonsterRemoved;

        if (!activeMonsters.Contains(boss))
        {
            activeMonsters.Add(boss);
        }

        return boss;
    }

    // 보스 전용 스폰 (Fire and forget 방식 - 콜백 사용)
    public void SpawnBossById(int bossId, Action<Enemy> onSpawned = null)
    {
        SpawnBossByIdAsync(bossId).ContinueWith(boss => onSpawned?.Invoke(boss)).Forget();
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

        // 리스트에서 제거
        if (boss != null)
        {
            OnMonsterRemoved(boss);
        }

        // 콜백 실행
        onDeath?.Invoke();
    }

}
