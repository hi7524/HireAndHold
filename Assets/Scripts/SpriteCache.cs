using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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


    private readonly Dictionary<string, Sprite> cache = new();

    private readonly Dictionary<string, AsyncLazy<Sprite>> loadingTasks = new();

    private readonly HashSet<string> failedKeys = new();

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
    /// 스프라이트 로드 (캐시 + 병합)
    /// </summary>
    public UniTask<Sprite> LoadSpriteAsync(string address)
    {
        if (string.IsNullOrEmpty(address))
            return UniTask.FromResult<Sprite>(null);

        if (cache.TryGetValue(address, out var cached))
            return UniTask.FromResult(cached);

        if (failedKeys.Contains(address))
            return UniTask.FromResult<Sprite>(null);

        if (!loadingTasks.TryGetValue(address, out var lazy))
        {
            lazy = new AsyncLazy<Sprite>(() => LoadSpriteInternalAsync(address));
            loadingTasks[address] = lazy;
        }

        return lazy.Task;
    }

    /// <summary>
    /// 여러 스프라이트 프리로드
    /// </summary>
    public async UniTask PreloadSpritesAsync(IEnumerable<string> addresses)
    {
        var tasks = new List<UniTask>();

        foreach (var address in addresses)
        {
            if (string.IsNullOrEmpty(address)) continue;
            if (cache.ContainsKey(address)) continue;
            if (failedKeys.Contains(address)) continue;

            tasks.Add(LoadSpriteAsync(address).AsUniTask());
        }

        if (tasks.Count > 0)
        {
            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch
            {
                // 프리로드는 실패 무시
            }
        }
    }

    /// <summary>
    /// 캐시에서 즉시 가져오기 (로드 안 함)
    /// </summary>
    public Sprite GetCachedSprite(string address)
    {
        return cache.TryGetValue(address, out var sprite) ? sprite : null;
    }

    /// <summary>
    /// 캐시 개수
    /// </summary>
    public int CachedCount => cache.Count;

    /// <summary>
    /// 실패 개수
    /// </summary>
    public int FailedCount => failedKeys.Count;


    public bool IsCached(string address)
    {
        return cache.ContainsKey(address);
    }


    public void ClearCache()
    {
        foreach (var kv in cache)
        {

            if (kv.Value != null)
                Addressables.Release(kv.Value);
        }

        cache.Clear();
        loadingTasks.Clear();
        failedKeys.Clear();
        Debug.Log("[SpriteCache] 캐시 클리어");
    }




    private async UniTask<Sprite> LoadSpriteInternalAsync(string address)
    {
        AsyncOperationHandle<Sprite> handle = default;

        try
        {
            handle = Addressables.LoadAssetAsync<Sprite>(address);
            var sprite = await handle;

            if (sprite != null)
            {
                cache[address] = sprite;
                return sprite;
            }

            failedKeys.Add(address);
            return null;
        }
        catch
        {
            failedKeys.Add(address);
            return null;
        }
        finally
        {
            loadingTasks.Remove(address);

            if (handle.IsValid() &&
                handle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(handle);
            }
        }
    }

    public Sprite GetCachedSpriteOrNull(string address)
    {
        if (string.IsNullOrEmpty(address)) return null;
        return cache.TryGetValue(address, out var s) ? s : null;
    }

    public bool IsFailed(string address) => failedKeys.Contains(address);


}
