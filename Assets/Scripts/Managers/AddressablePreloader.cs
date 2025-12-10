using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 타이틀 -> 로비 전환 시 게임에서 사용할 모든 Addressable 에셋을 미리 로드하고 캐싱하는 클래스
/// </summary>
public class AddressablePreloader : MonoBehaviour
{
    private static AddressablePreloader instance;
    public static AddressablePreloader Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("AddressablePreloader");
                instance = go.AddComponent<AddressablePreloader>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // 캐싱된 에셋들
    private Dictionary<string, GameObject> cachedPrefabs = new Dictionary<string, GameObject>();
    private Dictionary<string, UnitGridData> cachedGridData = new Dictionary<string, UnitGridData>();
    private Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> cachedMaps = new Dictionary<string, Sprite>();
    private List<AsyncOperationHandle> handles = new List<AsyncOperationHandle>();

    public bool IsLoaded { get; private set; } = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 게임에서 사용할 모든 Addressable 에셋을 미리 로드
    /// </summary>
    public async UniTask PreloadAllAsync(CancellationToken ct, IProgress<float> progress = null)
    {
        if (IsLoaded)
        {
            progress?.Report(1f);
            return;
        }

        var prefabKeys = new List<string>();
        var gridDataKeys = new List<string>();
        var spriteKeys = new List<string>();
        var mapKeys = new List<string>();

        // 1. 몬스터 비주얼 키 수집
        var monsterTable = DataTableManager.MonsterTable.GetAll();
        foreach (var monster in monsterTable)
        {
            if (!string.IsNullOrEmpty(monster.MON_MODEL) &&
                !prefabKeys.Contains(monster.MON_MODEL) &&
                IsValidAddressableKey(monster.MON_MODEL))
            {
                prefabKeys.Add(monster.MON_MODEL);
            }
        }

        // 2. 유닛 비주얼, GridData, Sprite 키 수집
        var unitTable = DataTableManager.UnitTable.GetAll();
        foreach (var unit in unitTable)
        {
            if (!string.IsNullOrEmpty(unit.PREFAB_NAME) &&
                !prefabKeys.Contains(unit.PREFAB_NAME) &&
                IsValidAddressableKey(unit.PREFAB_NAME))
            {
                prefabKeys.Add(unit.PREFAB_NAME);
            }

            if (!string.IsNullOrEmpty(unit.GRID_DATA) &&
                !gridDataKeys.Contains(unit.GRID_DATA) &&
                IsValidAddressableKey(unit.GRID_DATA))
            {
                gridDataKeys.Add(unit.GRID_DATA);
            }

            if (!string.IsNullOrEmpty(unit.UNIT_ICON) &&
                !spriteKeys.Contains(unit.UNIT_ICON) &&
                IsValidAddressableKey(unit.UNIT_ICON))
            {
                spriteKeys.Add(unit.UNIT_ICON);
            }
        }

        // 3. 패시브 스킬 아이콘 키 수집 (ID: 22070~22087)
        var skillTable = DataTableManager.SkillTable;
        for (int skillId = 22070; skillId <= 22087; skillId++)
        {
            var skill = skillTable.Get(skillId);
            if (skill != null && !string.IsNullOrEmpty(skill.SKILL_ICON) &&
                !spriteKeys.Contains(skill.SKILL_ICON) &&
                IsValidAddressableKey(skill.SKILL_ICON))
            {
                spriteKeys.Add(skill.SKILL_ICON);
            }
        }

        // 4. 스테이지 맵 키 수집
        var stageTable = DataTableManager.StageTable.GetAll();
        foreach (var stage in stageTable)
        {
            if (!string.IsNullOrEmpty(stage.STAGE_MAP) &&
                !mapKeys.Contains(stage.STAGE_MAP) &&
                IsValidAddressableKey(stage.STAGE_MAP))
            {
                mapKeys.Add(stage.STAGE_MAP);
            }
        }

        int total = prefabKeys.Count + gridDataKeys.Count + spriteKeys.Count + mapKeys.Count;
        int completed = 0;

        if (total == 0)
        {
            IsLoaded = true;
            progress?.Report(1f);
            return;
        }

        var loadTasks = new List<UniTask>();

        // 프리팹 로드 태스크 추가
        foreach (var key in prefabKeys)
        {
            loadTasks.Add(LoadPrefabWithProgress(key, ct, () =>
            {
                completed++;
                progress?.Report((float)completed / total);
            }));
        }

        // GridData 로드 태스크 추가
        foreach (var key in gridDataKeys)
        {
            loadTasks.Add(LoadGridDataWithProgress(key, ct, () =>
            {
                completed++;
                progress?.Report((float)completed / total);
            }));
        }

        // Sprite 로드 태스크 추가
        foreach (var key in spriteKeys)
        {
            loadTasks.Add(LoadSpriteWithProgress(key, ct, () =>
            {
                completed++;
                progress?.Report((float)completed / total);
            }));
        }

        // 맵 로드 태스크 추가
        foreach (var key in mapKeys)
        {
            loadTasks.Add(LoadMapWithProgress(key, ct, () =>
            {
                completed++;
                progress?.Report((float)completed / total);
            }));
        }

        await UniTask.WhenAll(loadTasks);

        IsLoaded = true;
        Debug.Log($"[AddressablePreloader] 프리로드 완료: {cachedPrefabs.Count} 프리팹, {cachedGridData.Count} GridData, {cachedSprites.Count} Sprite, {cachedMaps.Count} 맵");
    }

    private async UniTask LoadPrefabWithProgress(string key, CancellationToken ct, Action onComplete)
    {
        try
        {
            await LoadAndCachePrefab(key, ct);
        }
        finally
        {
            onComplete?.Invoke();
        }
    }

    private async UniTask LoadGridDataWithProgress(string key, CancellationToken ct, Action onComplete)
    {
        try
        {
            await LoadAndCacheGridData(key, ct);
        }
        finally
        {
            onComplete?.Invoke();
        }
    }

    private async UniTask LoadSpriteWithProgress(string key, CancellationToken ct, Action onComplete)
    {
        try
        {
            await LoadAndCacheSprite(key, ct);
        }
        finally
        {
            onComplete?.Invoke();
        }
    }

    private async UniTask LoadMapWithProgress(string key, CancellationToken ct, Action onComplete)
    {
        try
        {
            await LoadAndCacheMap(key, ct);
        }
        finally
        {
            onComplete?.Invoke();
        }
    }

    private async UniTask LoadAndCachePrefab(string key, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(key) || cachedPrefabs.ContainsKey(key))
            return;

        try
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(key);
            handles.Add(handle);
            var prefab = await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status == AsyncOperationStatus.Succeeded && prefab != null)
            {
                cachedPrefabs[key] = prefab;
            }
        }
        catch (OperationCanceledException)
        {
            // 취소됨 - 정상적인 상황
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AddressablePreloader] 프리팹 로드 실패: {key}, {e.Message}");
        }
    }

    private async UniTask LoadAndCacheGridData(string key, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(key) || cachedGridData.ContainsKey(key))
            return;

        try
        {
            var handle = Addressables.LoadAssetAsync<UnitGridData>(key);
            handles.Add(handle);
            var gridData = await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status == AsyncOperationStatus.Succeeded && gridData != null)
            {
                cachedGridData[key] = gridData;
            }
        }
        catch (OperationCanceledException)
        {
            // 취소됨 - 정상적인 상황
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AddressablePreloader] GridData 로드 실패: {key}, {e.Message}");
        }
    }

    private async UniTask LoadAndCacheSprite(string key, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(key) || cachedSprites.ContainsKey(key))
            return;

        try
        {
            var handle = Addressables.LoadAssetAsync<Sprite>(key);
            handles.Add(handle);
            var sprite = await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status == AsyncOperationStatus.Succeeded && sprite != null)
            {
                cachedSprites[key] = sprite;
            }
        }
        catch (OperationCanceledException)
        {
            // 취소됨 - 정상적인 상황
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AddressablePreloader] Sprite 로드 실패: {key}, {e.Message}");
        }
    }

    private async UniTask LoadAndCacheMap(string key, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(key) || cachedMaps.ContainsKey(key))
            return;

        try
        {
            var handle = Addressables.LoadAssetAsync<Sprite>(key);
            handles.Add(handle);
            var mapSprite = await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status == AsyncOperationStatus.Succeeded && mapSprite != null)
            {
                cachedMaps[key] = mapSprite;
            }
        }
        catch (OperationCanceledException)
        {
            // 취소됨 - 정상적인 상황
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AddressablePreloader] 맵 로드 실패: {key}, {e.Message}");
        }
    }

    /// <summary>
    /// 캐싱된 프리팹 가져오기
    /// </summary>
    public GameObject GetCachedPrefab(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        return cachedPrefabs.TryGetValue(key, out var prefab) ? prefab : null;
    }

    /// <summary>
    /// 캐싱된 GridData 가져오기
    /// </summary>
    public UnitGridData GetCachedGridData(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        return cachedGridData.TryGetValue(key, out var data) ? data : null;
    }

    /// <summary>
    /// 프리팹이 캐싱되어 있는지 확인
    /// </summary>
    public bool HasCachedPrefab(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return cachedPrefabs.ContainsKey(key);
    }

    /// <summary>
    /// GridData가 캐싱되어 있는지 확인
    /// </summary>
    public bool HasCachedGridData(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return cachedGridData.ContainsKey(key);
    }

    /// <summary>
    /// 캐싱된 Sprite 가져오기
    /// </summary>
    public Sprite GetCachedSprite(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        return cachedSprites.TryGetValue(key, out var sprite) ? sprite : null;
    }

    /// <summary>
    /// Sprite가 캐싱되어 있는지 확인
    /// </summary>
    public bool HasCachedSprite(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return cachedSprites.ContainsKey(key);
    }

    /// <summary>
    /// 캐싱된 맵 스프라이트 가져오기
    /// </summary>
    public Sprite GetCachedMap(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        return cachedMaps.TryGetValue(key, out var map) ? map : null;
    }

    /// <summary>
    /// 맵이 캐싱되어 있는지 확인
    /// </summary>
    public bool HasCachedMap(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return cachedMaps.ContainsKey(key);
    }

    /// <summary>
    /// 유효한 Addressable 키인지 확인 (폴더 경로나 플레이스홀더 텍스트 필터링)
    /// </summary>
    private bool IsValidAddressableKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        // 한글 플레이스홀더 텍스트 필터링 (예: "폴더 경로")
        if (key.Contains("폴더") || key.Contains("경로"))
            return false;

        // 공백만 있는 키 필터링
        if (string.IsNullOrWhiteSpace(key))
            return false;

        // 파일 시스템 경로 형식 필터링 (Assets/로 시작하는 경우)
        if (key.StartsWith("Assets/") || key.StartsWith("Assets\\"))
            return false;

        return true;
    }

    private void OnDestroy()
    {
        foreach (var handle in handles)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        handles.Clear();
        cachedPrefabs.Clear();
        cachedGridData.Clear();
        cachedSprites.Clear();
        cachedMaps.Clear();

        if (instance == this)
        {
            instance = null;
        }
    }
}
