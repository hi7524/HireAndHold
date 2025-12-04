using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

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


    // Addressable Asset 로드 후 풀 생성
    private async void Awake()
    {
        await LoadAllAddressableAssets();

        foreach(var item in poolItems)
        {
            if (item.isLabel)
            {
                // null 체크 추가
                if (item.cachedPrefabs == null || item.cachedPrefabs.Count == 0)
                    continue;

                // Label로 로드된 여러 Asset에 대해 각각 풀 생성
                foreach(var prefab in item.cachedPrefabs)
                {
                    CreatePoolForPrefab(prefab.name, prefab, item.defaultCapacity, item.maxSize);
                }
            }
            else
            {
                // 단일 Asset에 대해 풀 생성
                CreatePool(item);
            }
        }
    }

    // 모든 Addressable Asset을 병렬로 로드
    private async Task LoadAllAddressableAssets()
    {
        List<Task> loadTasks = new List<Task>();

        foreach(var item in poolItems)
        {
            loadTasks.Add(LoadAddressableAsset(item));
        }

        await Task.WhenAll(loadTasks);
        Debug.Log("모든 어드레서블 에셋 로드 완료");
    }

    // 단일 또는 Label 기반 Addressable Asset 로드
    private async Task LoadAddressableAsset(PoolItem item)
    {
        if (string.IsNullOrEmpty(item.addressableKey))
            return;

        if (item.isLabel)
        {
            // Label로 여러 개 로드
            AsyncOperationHandle<IList<GameObject>> handle =
                Addressables.LoadAssetsAsync<GameObject>(item.addressableKey, null);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                item.cachedPrefabs = new List<GameObject>(handle.Result);
            }
        }
        else
        {
            // 캐시에서 먼저 시도
            var cachedPrefab = AddressablePreloader.Instance.GetCachedPrefab(item.addressableKey);
            if (cachedPrefab != null)
            {
                item.cachedPrefab = cachedPrefab;
                return;
            }

            // 캐시에 없으면 직접 로드 (fallback)
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(item.addressableKey);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                item.cachedPrefab = handle.Result;
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
        Destroy(obj.gameObject);
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
            Destroy(obj);
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