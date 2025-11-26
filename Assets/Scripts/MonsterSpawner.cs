using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private ObjectPoolManager poolManager;
    [SerializeField] private string monsterKey = "Monster";
    [SerializeField] private string bossKey = "BossMonster"; 
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float horizontalRange = 2f;

    private List<Enemy> activeMonsters = new List<Enemy>();

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
    //     monster.Initialize(poolManager, monsterKey);
        monster.InitializeWithData(poolManager, key, data, isBoss);
        if(!activeMonsters.Contains(monster))
        {
            activeMonsters.Add(monster);
        }
    }

    public void OnMonsterRemoved(Enemy monster)
    {
        if (activeMonsters.Contains(monster))
        {
            activeMonsters.Remove(monster);
        }
    }
    // 모든 활성 몬스터 즉시 제거
    public void KillAllMonsters()
    {
        Debug.Log($"[MonsterSpawner] 활성 몬스터 {activeMonsters.Count}마리 제거");

        var monstersToKill = new List<Enemy>(activeMonsters);

        foreach (var monster in monstersToKill)
        {
            if (monster != null && monster.gameObject.activeSelf)
            {
                monster.Die(); 
            }
        }

        activeMonsters.Clear();
    }

    // 보스 전용 스폰 (Monster 참조 반환)
    public Enemy SpawnBossById(int bossId)
    {
        MonsterData data = DataTableManager.MonsterTable.Get(bossId);
        if (data == null)
        {
            Debug.LogError($"보스 ID {bossId} 없음!");
            return null;
        }

        GameObject bossObj = poolManager.Get(bossKey);
        if (bossObj == null)
        {
            Debug.LogError("보스 풀 비어있음!");
            return null;
        }

        Vector3 spawnPos = spawnPoint.position;
        bossObj.transform.position = spawnPos;

        Enemy boss = bossObj.GetComponent<Enemy>();
        boss.transform.position = spawnPos;
        boss.InitializeWithData(poolManager, bossKey, data, true); // isBoss = true

        if (!activeMonsters.Contains(boss))
        {
            activeMonsters.Add(boss);
        }

        Debug.Log($"[MonsterSpawner] 중간보스 {data.MON_NAME} 스폰!");
        
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

        Debug.Log("[MonsterSpawner] 보스 사망 감지!");
        
        // 리스트에서 제거
        if (boss != null)
        {
            OnMonsterRemoved(boss);
        }
        
        // 콜백 실행
        onDeath?.Invoke();
    }

}
