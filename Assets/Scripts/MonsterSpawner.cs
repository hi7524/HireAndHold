using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private ObjectPoolManager poolManager;
    [SerializeField] private string monsterKey = "Monster";
    [SerializeField] private string bossKey = "BossMonster"; 

    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float horizontalRange = 2f;

    [SerializeField] private float rushInterval = 150f;
    [SerializeField] private float rushDuration = 30f;
    [SerializeField] private float rushSpawnInterval = 0.2f;

    [SerializeField] private WindowUI bossClearUI;

    private int maxSpawnCount;
    private int currentSpawnCount = 0;
    private bool spawning = false;
    private bool isRushing = false;
    private bool bossSpawned = false;

    private int bossSpawnCount = 0;
    private float elapsedTime = 0f;
    private float nextRushTime;

    private CancellationTokenSource cts;
    private Monster currentBoss;

    private void Start()
    {
        var item = poolManager.poolItems.Find(x => x.key == monsterKey);
        if (item != null)
        {
            maxSpawnCount = item.maxSize;
        }

        cts = new CancellationTokenSource();
        nextRushTime = rushInterval;

        StartSpawning(cts.Token);
        RushLoopAsync(cts.Token).Forget();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
    }

    public void StartSpawning(CancellationToken token)
    {
        spawning = true;
        SpawnLoopAsync(token).Forget();
    }

    private async UniTaskVoid SpawnLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (!spawning || bossSpawned)
            {
                await UniTask.Yield();
                continue;
            }

            if (currentSpawnCount < maxSpawnCount)
            {
                SpawnMonsters();
                currentSpawnCount++;
            }

            float interval = isRushing ? rushSpawnInterval : spawnInterval;
            await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: token);
        }
    }

    private async UniTaskVoid RushLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            float waitTime = Mathf.Max(0, nextRushTime - elapsedTime);
            await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: token);

            StartRush();
            await UniTask.Delay(TimeSpan.FromSeconds(rushDuration), cancellationToken: token);
            EndRush();

            if (!bossSpawned && bossSpawnCount < 3)
            {
                await SpawnBossAsync(token);
                bossSpawnCount++;
            }

            if (bossSpawnCount == 1)
            {
                nextRushTime += rushInterval;
            }
            else if (bossSpawnCount == 2)
            {
                nextRushTime += 600f;
            }
            else
            {
                Debug.Log("나올 보스 이제 없음, 완료");
                break;
            }
        }
    }

    private void StartRush()
    {
        isRushing = true;
        currentSpawnCount = 0;
    }

    private void EndRush()
    {
        isRushing = false;
        currentSpawnCount = 0;
    }

    private void SpawnMonsters()
    {
        GameObject monsters = poolManager.Get(monsterKey);
        if (monsters == null)
        {
            Debug.Log("Monster 풀 비어있음");
            return;
        }

        Vector3 spawnPos = spawnPoint.position;
        spawnPos.x += UnityEngine.Random.Range(-horizontalRange, horizontalRange);

        Monster monster = monsters.GetComponent<Monster>();
        monster.transform.position = spawnPos;
        monster.Initialize(poolManager, monsterKey);
    }

    private async UniTask SpawnBossAsync(CancellationToken token)
    {
        bossSpawned = true;
        spawning = false;

        Debug.Log($"보스 {bossSpawnCount + 1} 등장");

        await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: token);

        GameObject bossObj = poolManager.Get(bossKey);
        if (bossObj == null)
        {
            Debug.Log("보스 풀 비어있음");
            bossSpawned = false;
            spawning = true;
            return;
        }

        Vector3 spawnPos = spawnPoint.position;
        spawnPos.x += UnityEngine.Random.Range(-horizontalRange, horizontalRange);

        currentBoss = bossObj.GetComponent<Monster>();
        currentBoss.transform.position = spawnPos;
        currentBoss.Initialize(poolManager, bossKey, boss: true);

        await WaitUntilBossDead(token);

        Debug.Log("보스 사망");
        bossSpawned = false;
        spawning = true;
    }

    private async UniTask WaitUntilBossDead(CancellationToken token)
    {
        while (currentBoss != null && currentBoss.gameObject.activeSelf && !token.IsCancellationRequested)
        {
            await UniTask.Yield();
        }

        bossClearUI.Show();

        await UniTask.WaitUntil(() => Time.timeScale > 0f, cancellationToken: token);
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}
