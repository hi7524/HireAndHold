using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class MonsterSpawner : MonoBehaviour
{
    //pool
    [SerializeField] private ObjectPoolManager poolManager;
    [SerializeField] private string monsterKey = "Monster";

    //basic spawn
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private Transform spawnPoint;
    private int currentSpawnCount = 0;
    private bool spawning = false;
    private int maxSpawnCount;
    private float horizontalRange = 2f;

    //monster rush
    [SerializeField] private float rushInterval = 10f;   
    [SerializeField] private float rushDuration = 30f;    
    [SerializeField] private float rushSpawnInterval = 0.2f;
    [SerializeField] private int rushMaxSpawnCount = 100; 


    private bool isRushing = false;
    private float elapsedTime = 0f;
    private float nextRushTime;
    private CancellationTokenSource cts;

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

    public void StopSpawning()
    {
        spawning = false; 
    }
    public void StartSpawning(CancellationToken token)
    {
        spawning = true;
        SpawnLoopAsync(token).Forget();
    }

    private void OnDestroy()
    {
        cts.Cancel();
        cts.Dispose();
    }
    private async UniTaskVoid SpawnLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (spawning && currentSpawnCount < maxSpawnCount)
                {
                    SpawnMonsters();
                    currentSpawnCount++;
                }

                float interval = isRushing ? rushSpawnInterval : spawnInterval;
                await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private async UniTaskVoid RushLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                float waitTime = Mathf.Max(0, nextRushTime - elapsedTime);
                await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: token);

                StartRush(rushMaxSpawnCount);
                await UniTask.Delay(TimeSpan.FromSeconds(rushDuration), cancellationToken: token);
                EndRush();

                nextRushTime += rushInterval;
            }
        }
        catch (OperationCanceledException) { }
    }


    public void StartRush(int rushMaxSpawn)
    {
        isRushing = true;
        currentSpawnCount = 0; 
        maxSpawnCount = rushMaxSpawn;
    }

    public void EndRush()
    {
        isRushing = false;
        currentSpawnCount = 0;
    }

    private void SpawnMonsters()
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
