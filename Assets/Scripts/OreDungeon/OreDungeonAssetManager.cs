using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 광석 던전 에셋 관리
/// AddressablePreloader 유무에 따라 캐시 참조 또는 직접 로드를 분기 처리
/// 정상 플로우와 에디터 테스트 양쪽을 지원
/// </summary>
public class OreDungeonAssetManager : MonoBehaviour
{
    [SerializeField] private OreDungeonManager gameManager;

    public DataTable_OreDungeon OreDungeonTable { get; private set; }
    public DataTable_Ore OreTable { get; private set; }
    public Dictionary<int, Sprite> UnitSprites { get; private set; } = new Dictionary<int, Sprite>();

    private bool isPreloaded;


    private void Awake()
    {
        // 참조 누락 확인
        if (!ValidateReferences())
            return;

        // Preload 여부 판단
        isPreloaded = AddressablePreloader.Instance != null &&
                      AddressablePreloader.Instance.IsLoaded;
    }

    private async void Start()
    {
        // DatabaseManager 초기화 대기 (프리셋 데이터 로드를 위해)
        if (isPreloaded && DatabaseManager.Instance != null)
        {
            await DatabaseManager.Instance.WaitForInitializationAsync();
        }

        await LoadResources();

        gameManager.Initialize(isPreloaded);
    }

    // Manager의 CurDungeonID에 따라 리소스를 로드하거나 캐시에서 가져옴
    private async UniTask LoadResources()
    {
        if (isPreloaded)
        {
            OreDungeonTable = DataTableManager.OreDungeonTable;
            OreTable = DataTableManager.OreTable;
        }
        else
        {
            // DataTableManager 초기화 대기 (UnitTable 등 필요)
            await DataTableManager.InitAsync();

            await LoadOreDungeonTableDirectly();
            await LoadOreTableDirectly();
        }

        // 모든 유닛 스프라이트 미리 로드
        await LoadAllUnitSprites();
    }

    // 모든 유닛 스프라이트 미리 로드
    private async UniTask LoadAllUnitSprites()
    {
        if (DataTableManager.UnitTable == null)
            return;

        UnitSprites.Clear();

        List<UniTask> loadTasks = new List<UniTask>();

        foreach (var unitId in DataTableManager.UnitTable.RawTable.Keys)
        {
            UnitData unitData = DataTableManager.UnitTable.Get(unitId);
            if (unitData != null && !string.IsNullOrEmpty(unitData.UNIT_ICON))
            {
                loadTasks.Add(LoadUnitSprite(unitId, unitData.UNIT_ICON));
            }
        }

        await UniTask.WhenAll(loadTasks);
    }

    // 개별 유닛 스프라이트 로드
    private async UniTask LoadUnitSprite(int unitId, string iconAddress)
    {
        try
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(iconAddress).ToUniTask();
            if (sprite != null)
            {
                UnitSprites[unitId] = sprite;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"유닛 스프라이트 로드 실패 (ID: {unitId}, Address: {iconAddress}): {e.Message}");
        }
    }

    // OreDungeon 테이블만 직접 로드
    private async UniTask LoadOreDungeonTableDirectly()
    {
        try
        {
            OreDungeonTable = new DataTable_OreDungeon();
            await OreDungeonTable.LoadAsync(DataTableIds.OreDungeon);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"OreDungeon 테이블 로드 실패: {e.Message}");
        }
    }

    // Ore 테이블 직접 로드
    private async UniTask LoadOreTableDirectly()
    {
        try
        {
            OreTable = new DataTable_Ore();
            await OreTable.LoadAsync(DataTableIds.Ore);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ore 테이블 로드 실패: {e.Message}");
        }
    }

    // 참조 누락 확인
    private bool ValidateReferences()
    {
        if (gameManager == null)
        {
            Debug.LogError($"{nameof(OreDungeonManager)} 참조가 누락되었습니다.");
            return false;
        }

        return true;
    }
}