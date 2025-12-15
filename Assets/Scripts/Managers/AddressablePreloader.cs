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
    private Dictionary<string, GridLayoutData> cachedGridLayouts = new Dictionary<string, GridLayoutData>();
    private Dictionary<string, AudioClip> cachedAudioClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, Sprite> cachedGridIcons = new Dictionary<string, Sprite>(); // 유닛 그리드 아이콘
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
        var gridLayoutKeys = new List<string>();
        var audioClipKeys = new List<string>();

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

            if (!string.IsNullOrEmpty(unit.UNIT_ICON_FULL) &&
                !spriteKeys.Contains(unit.UNIT_ICON_FULL) &&
                IsValidAddressableKey(unit.UNIT_ICON_FULL))
            {
                spriteKeys.Add(unit.UNIT_ICON_FULL);
            }

            if (!string.IsNullOrEmpty(unit.HIT_AUDIO_CLIP) &&
                !audioClipKeys.Contains(unit.HIT_AUDIO_CLIP) &&
                IsValidAddressableKey(unit.HIT_AUDIO_CLIP))
            {
                audioClipKeys.Add(unit.HIT_AUDIO_CLIP);
            }
        }

        // 3. 플레이어 스킬 아이콘 키 수집 (ID: 22059~22069) + 패시브 스킬 (22070~22087)
        var skillTable = DataTableManager.SkillTable;
        for (int skillId = 22059; skillId <= 22087; skillId++)
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

            // 스테이지 그리드 레이아웃 키 수집
            if (!string.IsNullOrEmpty(stage.STAGE_GRID) &&
                !gridLayoutKeys.Contains(stage.STAGE_GRID) &&
                IsValidAddressableKey(stage.STAGE_GRID))
            {
                gridLayoutKeys.Add(stage.STAGE_GRID);
            }
        }

        // 5. 플레이어 스킬 이펙트 키 수집 (SKILL_OBJECT=2인 플레이어 스킬)
        var allSkills = DataTableManager.SkillTable.GetAll();
        foreach (var skill in allSkills)
        {
            // SKILL_OBJECT=2는 플레이어 스킬
            if (skill.SKILL_OBJECT == 2 &&
                !string.IsNullOrEmpty(skill.SKILL_EFFECT) &&
                !prefabKeys.Contains(skill.SKILL_EFFECT) &&
                IsValidAddressableKey(skill.SKILL_EFFECT))
            {
                prefabKeys.Add(skill.SKILL_EFFECT);
            }
        }

        // 6. 플레이어 스킬 프리팹 Label로 일괄 로드
        await LoadPlayerSkillPrefabsByLabel(ct);

        // 7. 유닛 그리드 아이콘 Label로 일괄 로드
        await LoadUnitGridIconsByLabel(ct);

        int total = prefabKeys.Count + gridDataKeys.Count + spriteKeys.Count + mapKeys.Count + gridLayoutKeys.Count + audioClipKeys.Count;
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

        // GridLayout 로드 태스크 추가
        foreach (var key in gridLayoutKeys)
        {
            loadTasks.Add(LoadGridLayoutWithProgress(key, ct, () =>
            {
                completed++;
                progress?.Report((float)completed / total);
            }));
        }

        // AudioClip 로드 태스크 추가
        foreach (var key in audioClipKeys)
        {
            loadTasks.Add(LoadAudioClipWithProgress(key, ct, () =>
            {
                completed++;
                progress?.Report((float)completed / total);
            }));
        }

        await UniTask.WhenAll(loadTasks);

        IsLoaded = true;
        Debug.Log($"[AddressablePreloader] 프리로드 완료: {cachedPrefabs.Count} 프리팹, {cachedGridData.Count} GridData, {cachedSprites.Count} Sprite, {cachedMaps.Count} 맵, {cachedGridLayouts.Count} GridLayout, {cachedAudioClips.Count} AudioClip, {cachedGridIcons.Count} GridIcon");
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

    private async UniTask LoadGridLayoutWithProgress(string key, CancellationToken ct, Action onComplete)
    {
        try
        {
            await LoadAndCacheGridLayout(key, ct);
        }
        finally
        {
            onComplete?.Invoke();
        }
    }

    private async UniTask LoadAudioClipWithProgress(string key, CancellationToken ct, Action onComplete)
    {
        try
        {
            await LoadAndCacheAudioClip(key, ct);
        }
        finally
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// PlayerSkillPrefab Label이 붙은 모든 프리팹을 일괄 로드
    /// </summary>
    private async UniTask LoadPlayerSkillPrefabsByLabel(CancellationToken ct)
    {
        const string label = "PlayerSkillPrefab";

        try
        {
            // Label로 로드 시 AssetLabelReference 사용
            var labelRef = new AssetLabelReference { labelString = label };
            var handle = Addressables.LoadAssetsAsync<GameObject>(labelRef, prefab =>
            {
                if (prefab != null && !cachedPrefabs.ContainsKey(prefab.name))
                {
                    cachedPrefabs[prefab.name] = prefab;
                }
            });
            handles.Add(handle);
            await handle.ToUniTask(cancellationToken: ct);

            Debug.Log($"[AddressablePreloader] PlayerSkillPrefab Label 로드 완료: {handle.Result?.Count ?? 0}개");
        }
        catch (OperationCanceledException)
        {
            // 취소됨 - 정상적인 상황
        }
        catch (InvalidKeyException)
        {
            // Label이 없는 경우 - 무시 (스킬 프리팹이 등록되지 않은 상태)
            Debug.LogWarning($"[AddressablePreloader] '{label}' Label을 가진 에셋이 없습니다. Tools > Addressable > Register Player Skill Prefabs 메뉴를 실행해주세요.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AddressablePreloader] PlayerSkillPrefab Label 로드 실패: {e.Message}");
        }
    }

    /// <summary>
    /// UnitGridIcons Label이 붙은 모든 스프라이트를 일괄 로드
    /// </summary>
    private async UniTask LoadUnitGridIconsByLabel(CancellationToken ct)
    {
        const string label = "UnitGridIcons";

        try
        {
            // Label로 로드 시 AssetLabelReference 사용
            var labelRef = new AssetLabelReference { labelString = label };
            var handle = Addressables.LoadAssetsAsync<Sprite>(labelRef, sprite =>
            {
                if (sprite != null && !cachedGridIcons.ContainsKey(sprite.name))
                {
                    cachedGridIcons[sprite.name] = sprite;
                }
            });
            handles.Add(handle);
            await handle.ToUniTask(cancellationToken: ct);

            Debug.Log($"[AddressablePreloader] UnitGridIcons Label 로드 완료: {handle.Result?.Count ?? 0}개");
        }
        catch (OperationCanceledException)
        {
            // 취소됨 - 정상적인 상황
        }
        catch (Exception e)
        {
            // Label이 없거나 로드 실패한 경우 - 경고만 출력하고 계속 진행
            if (e is InvalidKeyException || e.GetType().Name == "InvalidKeyException")
            {
                Debug.LogWarning($"[AddressablePreloader] '{label}' Label을 가진 에셋이 없습니다. UnitGridIcons 그룹을 생성하고 아이콘들을 추가해주세요.");
            }
            else
            {
                Debug.LogWarning($"[AddressablePreloader] UnitGridIcons Label 로드 실패: {e.GetType().Name} - {e.Message}");
            }
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

    private async UniTask LoadAndCacheGridLayout(string key, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(key) || cachedGridLayouts.ContainsKey(key))
            return;

        try
        {
            var handle = Addressables.LoadAssetAsync<GridLayoutData>(key);
            handles.Add(handle);
            var gridLayout = await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status == AsyncOperationStatus.Succeeded && gridLayout != null)
            {
                cachedGridLayouts[key] = gridLayout;
            }
        }
        catch (OperationCanceledException)
        {
            // 취소됨 - 정상적인 상황
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AddressablePreloader] GridLayout 로드 실패: {key}, {e.Message}");
        }
    }

    private async UniTask LoadAndCacheAudioClip(string key, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(key) || cachedAudioClips.ContainsKey(key))
            return;

        try
        {
            var handle = Addressables.LoadAssetAsync<AudioClip>(key);
            handles.Add(handle);
            var audioClip = await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status == AsyncOperationStatus.Succeeded && audioClip != null)
            {
                cachedAudioClips[key] = audioClip;
            }
        }
        catch (OperationCanceledException)
        {
            // 취소됨 - 정상적인 상황
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AddressablePreloader] AudioClip 로드 실패: {key}, {e.Message}");
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
    /// 캐싱된 GridLayout 가져오기
    /// </summary>
    public GridLayoutData GetCachedGridLayout(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        return cachedGridLayouts.TryGetValue(key, out var gridLayout) ? gridLayout : null;
    }

    /// <summary>
    /// GridLayout이 캐싱되어 있는지 확인
    /// </summary>
    public bool HasCachedGridLayout(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return cachedGridLayouts.ContainsKey(key);
    }

    /// <summary>
    /// 캐싱된 AudioClip 가져오기
    /// </summary>
    public AudioClip GetCachedAudioClip(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        return cachedAudioClips.TryGetValue(key, out var audioClip) ? audioClip : null;
    }

    /// <summary>
    /// AudioClip이 캐싱되어 있는지 확인
    /// </summary>
    public bool HasCachedAudioClip(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return cachedAudioClips.ContainsKey(key);
    }

    /// <summary>
    /// 캐싱된 그리드 아이콘 가져오기
    /// </summary>
    public Sprite GetCachedGridIcon(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        return cachedGridIcons.TryGetValue(key, out var icon) ? icon : null;
    }

    /// <summary>
    /// 그리드 아이콘이 캐싱되어 있는지 확인
    /// </summary>
    public bool HasCachedGridIcon(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return cachedGridIcons.ContainsKey(key);
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

        // 숫자만 있는 키 필터링 (예: "0" - CSV에서 빈 값 표현)
        if (int.TryParse(key, out _))
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
        cachedGridLayouts.Clear();
        cachedAudioClips.Clear();
        cachedGridIcons.Clear();

        if (instance == this)
        {
            instance = null;
        }
    }
}
