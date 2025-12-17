using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
    [SerializeField] private FloatingTextSpawner floatingTextSpawner;

    // possible pooling objs:
    // 몬스터, 스킬, exp 아이템....

    [System.Serializable]
    public class PoolItem
    {
        public string key;
        public string addressableKey;
        public bool isLabel;
        public int defaultCapacity = 10;
        public int maxSize = 15;

        [System.NonSerialized]
        public GameObject cachedPrefab;         // 단일 Asset용

        [System.NonSerialized]
        public List<GameObject> cachedPrefabs;  // Label로 로드된 여러 Asset용
    }
    // 여러개 사용하기: isLabel 체크하고 addressable asset에 라벨 설정하기
    // 한 개 사용하기: addressableKey에 asset name만 적기


    public List<PoolItem> poolItems = new List<PoolItem>();

    public Dictionary<string, IObjectPool<GameObject>> pools = new Dictionary<string, IObjectPool<GameObject>>();


    // 타이틀→로비에서 AddressablePreloader가 이미 로드 완료함
    // 동기적으로 캐시에서 가져와서 풀 생성
    private void Awake()
    {
        LoadAllAssetsFromCache();

        foreach (var item in poolItems)
        {
            if (item.isLabel)
            {
                if (item.cachedPrefabs == null || item.cachedPrefabs.Count == 0)
                    continue;

                foreach (var prefab in item.cachedPrefabs)
                {
                    CreatePoolForPrefab(prefab.name, prefab, item.defaultCapacity, item.maxSize);
                }
            }
            else
            {
                // cachedPrefab이 없으면 풀 생성 건너뛰기
                if (item.cachedPrefab == null)
                {
                    Debug.LogWarning($"[ObjectPoolManager] '{item.key}' 풀 생성 실패 - 프리팹 없음");
                    continue;
                }
                CreatePool(item);
            }
        }

        Debug.Log("[ObjectPoolManager] 모든 풀 생성 완료");
    }

    // AddressablePreloader 캐시에서 에셋 가져오기 (없으면 동기 로드)
    private void LoadAllAssetsFromCache()
    {
        foreach (var item in poolItems)
        {
            if (string.IsNullOrEmpty(item.addressableKey))
                continue;

            if (item.isLabel)
            {
                // Label은 동기 로드로 처리
                var handle = Addressables.LoadAssetsAsync<GameObject>(item.addressableKey, null);
                handle.WaitForCompletion();

                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    item.cachedPrefabs = new List<GameObject>(handle.Result);
                }
            }
            else
            {
                // 캐시에서 먼저 시도
                var cachedPrefab = AddressablePreloader.Instance != null
                    ? AddressablePreloader.Instance.GetCachedPrefab(item.addressableKey)
                    : null;
                if (cachedPrefab != null)
                {
                    item.cachedPrefab = cachedPrefab;
                }
                else
                {
                    // 캐시에 없으면 동기 로드 (fallback)
                    var handle = Addressables.LoadAssetAsync<GameObject>(item.addressableKey);
                    item.cachedPrefab = handle.WaitForCompletion();

                    if (item.cachedPrefab != null)
                    {
                        Debug.Log($"[ObjectPoolManager] '{item.addressableKey}' 동기 로드 완료");
                    }
                    else
                    {
                        Debug.LogError($"[ObjectPoolManager] '{item.addressableKey}' 로드 실패");
                    }
                }
            }
        }
    }

    // PoolItem으로부터 오브젝트 풀 생성 및 초기화
    private void CreatePool(PoolItem item)
    {
        var pool = new ObjectPool<GameObject>(() => CreatePooledItem(item),OnGetFromPool,OnReleasedFromPool,OnDestroyPoolObject,
            true,
            item.defaultCapacity,
            item.maxSize
            );

        pools.Add(item.key, pool);

        for(int i =0; i < item.defaultCapacity; i++)
        {
            var obj = pool.Get();
            pool.Release(obj);
        }
    }

    // Prefab으로부터 직접 오브젝트 풀 생성 (Label용)
    private void CreatePoolForPrefab(string key, GameObject prefab, int defaultCapacity, int maxSize)
    {
        var pool = new ObjectPool<GameObject>(
            () => Instantiate(prefab),
            OnGetFromPool,
            OnReleasedFromPool,
            OnDestroyPoolObject,
            true,
            defaultCapacity,
            maxSize
        );

        pools.Add(key, pool);

        for (int i = 0; i < defaultCapacity; i++)
        {
            var obj = pool.Get();
            pool.Release(obj);
        }
    }

    // 캐싱된 Prefab으로 새 오브젝트 생성
    private GameObject CreatePooledItem(PoolItem item)
    {
        if (item.cachedPrefab == null)
            return null;

        GameObject obj = Instantiate(item.cachedPrefab);
        return obj;
    }

    // 풀에서 오브젝트를 가져올 때 호출 (활성화 및 초기화)
    private void OnGetFromPool(GameObject obj)
    {
        obj.SetActive(true);

        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.SetFloatingTextSpawner(floatingTextSpawner);
        }
    }

    // 풀로 오브젝트를 반환할 때 호출 (비활성화)
    private void OnReleasedFromPool(GameObject obj)
    {
        obj.SetActive(false);
    }

    // 풀이 maxSize 초과 시 오브젝트 파괴
    private void OnDestroyPoolObject(GameObject obj)
    {
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

    // 키로 풀에서 오브젝트 가져오기
    public GameObject Get(string key)
    {
        if(!pools.ContainsKey(key))
        {
            Debug.Log($"Pool {key} 없음");
            return null;
        }

        return pools[key].Get();
    }

    // 키로 풀에 오브젝트 반환하기
    public void Release(string key, GameObject obj)
    {
        if (!pools.ContainsKey(key))
        {
            Debug.Log($"Pool {key} X ");
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
            return;
        }

        if (!obj.activeSelf)
        {
            Debug.Log($"이미 반환 됨 {obj.name}");
            return;
        }

        pools[key].Release(obj);
    }
}