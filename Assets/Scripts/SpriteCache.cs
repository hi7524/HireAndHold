using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressables 스프라이트 캐싱 시스템
/// - 중복 로딩 방지
/// - 동시 요청 병합
/// - 잘못된 키 완벽 처리
/// </summary>
public class SpriteCache : MonoBehaviour
{
    private static SpriteCache instance;
    public static SpriteCache Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[SpriteCache]");
                instance = go.AddComponent<SpriteCache>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // 캐시된 스프라이트
    private Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // 로딩 중인 작업 (중복 요청 병합)
    private Dictionary<string, UniTask<Sprite>> loadingTasks = new Dictionary<string, UniTask<Sprite>>();

    // 실패한 키 추적 (재시도 방지)
    private HashSet<string> failedKeys = new HashSet<string>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 스프라이트 로드 (캐시 우선)
    /// </summary>
    public async UniTask<Sprite> LoadSpriteAsync(string address)
    {
        if (string.IsNullOrEmpty(address))
            return null;

        // 1. 캐시 확인 - 즉시 반환
        if (cache.TryGetValue(address, out Sprite cached))
            return cached;

        // 2. 이전에 실패한 키면 다시 시도하지 않음
        if (failedKeys.Contains(address))
            return null;

        // 3. 이미 로딩 중이면 해당 작업 대기
        if (loadingTasks.TryGetValue(address, out UniTask<Sprite> loadingTask))
            return await loadingTask;

        // 4. 새로 로딩 시작
        var task = LoadSpriteInternalAsync(address);
        loadingTasks[address] = task;

        try
        {
            return await task;
        }
        finally
        {
            loadingTasks.Remove(address);
        }
    }

    private async UniTask<Sprite> LoadSpriteInternalAsync(string address)
    {
        AsyncOperationHandle<Sprite> handle = default;

        try
        {
            // ⭐ Addressables 로드 - 모든 예외를 catch로 잡음
            handle = Addressables.LoadAssetAsync<Sprite>(address);

            // await 대신 직접 완료 대기
            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            // 성공 확인
            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                cache[address] = handle.Result;
                return handle.Result;
            }
            else
            {
                // 실패 처리
                failedKeys.Add(address);

                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                return null;
            }
        }
        catch (System.Exception)
        {
            // ⭐ 모든 예외를 조용히 처리
            failedKeys.Add(address);

            if (handle.IsValid())
            {
                try
                {
                    Addressables.Release(handle);
                }
                catch
                {
                    // Release 실패도 무시
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 여러 스프라이트 동시 프리로드
    /// </summary>
    public async UniTask PreloadSpritesAsync(IEnumerable<string> addresses)
    {
        var tasks = new List<UniTask<Sprite>>();
        int totalCount = 0;

        foreach (var address in addresses)
        {
            totalCount++;

            if (!string.IsNullOrEmpty(address) &&
                !cache.ContainsKey(address) &&
                !failedKeys.Contains(address))
            {
                tasks.Add(LoadSpriteAsync(address));
            }
        }

        if (tasks.Count > 0)
        {
            // ⭐ 모든 태스크 완료 대기 (예외 무시)
            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch
            {
                // WhenAll 예외도 무시
            }

            int successCount = cache.Count;
            int failCount = failedKeys.Count;

            Debug.Log($"[SpriteCache] 프리로드 완료 - 총: {totalCount}개, 성공: {successCount}개, 실패: {failCount}개");
        }
    }

    /// <summary>
    /// 즉시 스프라이트 가져오기 (캐시만)
    /// </summary>
    public Sprite GetCachedSprite(string address)
    {
        return cache.TryGetValue(address, out Sprite sprite) ? sprite : null;
    }

    /// <summary>
    /// 캐시 여부 확인
    /// </summary>
    public bool IsCached(string address)
    {
        return cache.ContainsKey(address);
    }

    /// <summary>
    /// 전체 캐시 클리어
    /// </summary>
    public void ClearCache()
    {
        cache.Clear();
        failedKeys.Clear();

        Debug.Log("[SpriteCache] 캐시 클리어 완료");
    }

    public int CachedCount => cache.Count;
    public int FailedCount => failedKeys.Count;

    private void OnDestroy()
    {
        ClearCache();
    }
}
