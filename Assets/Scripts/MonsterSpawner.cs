using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private ObjectPoolManager poolManager;
    [SerializeField] private string monsterKey = "Monster";
    [SerializeField] private float spawninterval = 1f;
    [SerializeField] private Transform spawnPoint;
    private int currentSpawnCount = 0;
    private bool spawning = false;
    private int maxSpawnCount;

    private float horizontalRange = 3f;

    private void Start()
    {
        var item = poolManager.poolItems.Find(x => x.key == monsterKey);
        if (item != null)
        {
            maxSpawnCount = item.maxSize;
        }

        SpawnLoopAsync().Forget();
    }
    private async UniTaskVoid SpawnLoopAsync()
    {
        spawning = true;
         while (spawning && currentSpawnCount < maxSpawnCount)
        {
            SpawnMosters();
            currentSpawnCount++;

            await UniTask.Delay(TimeSpan.FromSeconds(spawninterval));
        }
    }


    private void SpawnMosters()
    {
        GameObject monsters = poolManager.Get(monsterKey);
        if(monsters == null)
        {
            Debug.Log("Monster 가 풀에 없음");
            return;
        }

        Vector3 spawnPos = spawnPoint.position;
        spawnPos.x += UnityEngine.Random.Range(-horizontalRange, horizontalRange);

        Monster monster = monsters.GetComponent<Monster>();

        monster.transform.position = spawnPos;
        monster.transform.rotation = spawnPoint.rotation;

        
        if (monster != null)
        {
            monster.Initialize(poolManager, monsterKey);

        }


    }


}
