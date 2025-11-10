using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{

    // possible pooling objs:
    // 몬스터, 스킬, exp 아이템....

    [System.Serializable]
    public class PoolItem
    {
        public string key;
        public GameObject prefab;
        public int defaultCapacity = 10;
        public int maxSize = 15;
    }

    public List<PoolItem> poolItems = new List<PoolItem>();

    public Dictionary<string, IObjectPool<GameObject>> pools = new Dictionary<string, IObjectPool<GameObject>>();

    private void Awake()
    {
        foreach(var item in poolItems)
        {
            CreatePool(item);
        }
    }

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

    private GameObject CreatePooledItem(PoolItem item)
    {
        GameObject obj = Instantiate(item.prefab);

        return obj;
    }

    private void OnGetFromPool(GameObject obj)
    {
        obj.SetActive(true);
    }

    private void OnReleasedFromPool(GameObject obj)
    {
        obj.SetActive(false);
    }

    private void OnDestroyPoolObject(GameObject obj)
    {
        Destroy(obj.gameObject);
    }

    public GameObject Get(string key)
    {
        if(!pools.ContainsKey(key))
        {
            Debug.Log($"Pool {key} 없음");
            return null;
        }

        return pools[key].Get();
    }

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
