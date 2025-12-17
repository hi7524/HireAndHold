using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 광석 던전 에셋 관리
/// AddressablePreloader 유무에 따라 캐시 참조 또는 직접 로드를 분기 처리
/// 정상 플로우와 에디터 테스트 양쪽을 지원
/// </summary>
public class OreDungeonAssetManager : MonoBehaviour
{
    [SerializeField] private OreDungeonManager manager;

    private bool isPreloaded;
    private DataTable_OreDungeon oreDungeonTable;


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

        manager.Initialize(isPreloaded);
    }

    // Manager의 CurDungeonID에 따라 리소스를 로드하거나 캐시에서 가져옴
    private async UniTask LoadResources()
    {
        if (isPreloaded)
        {
            Debug.Log("캐시된 리소스 사용");
        }
        else
        {
            Debug.Log("직접 리소스 로드 시작");
            await LoadOreDungeonTableDirectly();
        }
    }

    // OreDungeon 테이블만 직접 로드
    private async UniTask LoadOreDungeonTableDirectly()
    {
        try
        {
            oreDungeonTable = new DataTable_OreDungeon();
            await oreDungeonTable.LoadAsync(DataTableIds.OreDungeon);
            Debug.Log("OreDungeon 테이블 로드 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"OreDungeon 테이블 로드 실패: {e.Message}");
        }
    }

    // Manager가 호출하는 메서드 - 직접 로드한 테이블 반환
    public DataTable_OreDungeon GetOreDungeonTable()
    {
        return isPreloaded ? DataTableManager.OreDungeonTable : oreDungeonTable;
    }

    // 참조 누락 확인
    private bool ValidateReferences()
    {
        if (manager == null)
        {
            Debug.LogError($"{nameof(OreDungeonManager)} 참조가 누락되었습니다.");
            return false;
        }

        return true;
    }
}