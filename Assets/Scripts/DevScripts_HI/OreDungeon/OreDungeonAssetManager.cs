using Cysharp.Threading.Tasks;
using UnityEngine;

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