using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[RequireComponent(typeof(Collider2D))]
public class GridUnit : MonoBehaviour, IDraggable
{
    [SerializeField] private GameObject cellPrf;
    [SerializeField] private Transform previewTrans;

    private Unit unit;
    private BattleUnitManager battleUnitManager;
    private GridManager gridManager;

    public int UnitId => unit.UnitID;
    public int StarLevel { get; private set; } = 1; // 성급 (1~3성)
    public UnitGridData GridData { get; private set; }
    public bool canPlaceInInventory = true; // 인벤토리에 보관 가능하게 할 지 여부

    // 드래그
    public bool IsDraggable => true;
    public bool RequireDropZone => true;
    public GameObject GameObject => gameObject;

    private GridCell curGridCell;
    private GridCell previousGridCell;

    private List<Transform> childrenObj = new List<Transform>();
    private AsyncOperationHandle<UnitGridData> gridDataHandle;

    // 드래그 프록시 관리
    private List<GridUnitDragProxy> dragProxies = new List<GridUnitDragProxy>();
    private Vector2Int touchOffset = Vector2Int.zero; // 터치한 셀의 오프셋


    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    private void Start()
    {
        CreatePreviewSprites();
        SetActiveChildrenObj(false);
    }

    public void SetUnitID(int unitId, int starLevel = 1)
    {
        StarLevel = Mathf.Clamp(starLevel, 1, 3);
        // 합성 시 UpdateUnitID 호출 - Stat 모디파이어 보존하면서 유닛 데이터/스킬/비주얼 업데이트
        unit.UpdateUnitID(unitId);
    } 

    public void SetInventoryPlaceable(bool value)
    {
        canPlaceInInventory = value;
    }

    public void SetBattleUnitManager(BattleUnitManager manager)
    {
        battleUnitManager = manager;
    }

    public void OnDragStart()
    {
        OnDragStartFromProxy(Vector2Int.zero);
    }

    // 프록시로부터 드래그 시작 (터치한 셀의 오프셋 정보 포함)
    public void OnDragStartFromProxy(Vector2Int offset)
    {
        touchOffset = offset;

        // GridCell 설정 관련
        previousGridCell = curGridCell;
        curGridCell?.ClearObject();
        curGridCell = null;

        // Grid 정보 시각화(미리보기) 관련
        SetActiveChildrenObj(true);
    }

    public void OnDrag()
    {
        // GridManager가 없으면 리턴
        if (gridManager == null || GridData == null)
            return;

        // 유닛의 현재 월드 위치를 그리드 위치로 변환
        Vector2Int currentGridPos = gridManager.WorldToGridPosition(transform.position);

        // 현재 그리드 위치 기준으로 배치 가능 여부 체크 및 색상 업데이트
        gridManager.CanPlaceUnit(currentGridPos, GridData.GetOccupiedCells());
    }

    public void OnDragEnd()
    {
        SetActiveChildrenObj(false);
    }

    public void OnDropFailed()
    {
        curGridCell = previousGridCell;

        // 드롭 실패 시 원래 그리드 상태 및 색상 복원
        if (curGridCell != null)
        {
            var gridManager = curGridCell.GetGridManager();
            if (gridManager != null)
            {
                // gridArray 상태 복원 (Empty -> Occupied)
                var occupiedCells = GridData.GetOccupiedCells();
                gridManager.SetGridState(curGridCell.GridPosition, GridState.Occupied);

                foreach (var relativePos in occupiedCells)
                {
                    Vector2Int absolutePos = curGridCell.GridPosition + relativePos;
                    gridManager.SetGridState(absolutePos, GridState.Occupied);
                }

                // 색상 복원
                gridManager.OnFailed();
            }

            // GridCell의 PlacedObject 참조도 복원
            curGridCell.RestorePlacedObject(gameObject);
        }
    }

    public void SetCurrentGridCell(GridCell cell)
    {
        curGridCell = cell;

        // GridManager 참조 설정
        if (cell != null && gridManager == null)
        {
            gridManager = cell.GetGridManager();

            // GridManager를 얻은 후 DragProxy 재생성 (크기와 위치가 올바르게 설정됨)
            if (GridData != null && dragProxies.Count == 0)
            {
                CreateDragProxies();
            }
        }
    }

    public GridCell GetPreviousCell()
    {
        return previousGridCell;
    }

    public void SetGridData(UnitGridData newGridData)
    {
        GridData = newGridData;

        // 기존 프리뷰 제거
        ClearPreviewSprites();

        // 새 프리뷰 생성
        CreatePreviewSprites();
        SetActiveChildrenObj(false);

        // 드래그 프록시 생성
        CreateDragProxies();
    }

    public Vector2Int GetTouchOffset()
    {
        return touchOffset;
    }

    // 미리보기 스프라이트 제거
    private void ClearPreviewSprites()
    {
        foreach (var child in childrenObj)
        {
            if (child != null)
                Destroy(child.gameObject);
        }
        childrenObj.Clear();
    }

    // 미리보기 스프라이트 생성
    private void CreatePreviewSprites()
    {
        if (GridData == null || cellPrf == null)
            return;

        var occupiedCells = GridData.GetOccupiedCells();

        // 중앙 셀
        CreatePreviewCell(Vector2Int.zero);

        // 나머지 셀
        foreach (var cellPos in occupiedCells)
        {
            CreatePreviewCell(cellPos);
        }
    }

    private void CreatePreviewCell(Vector2Int cellPos)
    {
        var cell = Instantiate(cellPrf, previewTrans);
        childrenObj.Add(cell.transform);
        cell.transform.localScale = GameConstants.previewCellSizeObject * Vector3.one;
        cell.transform.localPosition = new Vector3(cellPos.x * GameConstants.previewCellSizeObject, cellPos.y * GameConstants.previewCellSizeObject, 0);

        SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = GridData.gridColor;
    }

    // 자식 오브젝트 전체 비활성화 및 활성화
    private void SetActiveChildrenObj(bool value)
    {
        if (childrenObj == null || childrenObj.Count == 0)
            return;

        for (int i = 0; i < childrenObj.Count; i++)
        {
            childrenObj[i].gameObject.SetActive(value);
        }
    }

    // 드래그 프록시 생성 - 유닛이 차지하는 모든 셀 위치에 터치 가능한 콜라이더 배치
    private void CreateDragProxies()
    {
        // 기존 프록시 제거
        ClearDragProxies();

        if (GridData == null)
            return;

        var occupiedCells = GridData.GetOccupiedCells();

        // 중심 셀 프록시 (offset = 0, 0)
        CreateDragProxyAt(Vector2Int.zero);

        // 나머지 셀 프록시들
        foreach (var cellPos in occupiedCells)
        {
            CreateDragProxyAt(cellPos);
        }
    }

    private void CreateDragProxyAt(Vector2Int cellOffset)
    {
        if (gridManager == null)
            return;

        GameObject proxyObj = new GameObject($"DragProxy_{cellOffset.x}_{cellOffset.y}");
        proxyObj.transform.SetParent(transform);

        // 콜라이더 크기와 간격 설정
        const float proxySize = 0.5f;
        const float proxySpacing = 0.05f;
        float proxyInterval = proxySize + proxySpacing; // 0.6

        proxyObj.transform.localPosition = new Vector3(
            cellOffset.x * proxyInterval,
            cellOffset.y * proxyInterval,
            0
        );
        proxyObj.layer = gameObject.layer;

        // 콜라이더 추가 (0.5 x 0.5 고정 크기)
        BoxCollider2D collider = proxyObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(proxySize, proxySize);

        // 프록시 컴포넌트 추가
        GridUnitDragProxy proxy = proxyObj.AddComponent<GridUnitDragProxy>();
        proxy.Initialize(this, cellOffset);

        dragProxies.Add(proxy);
    }

    private void ClearDragProxies()
    {
        foreach (var proxy in dragProxies)
        {
            if (proxy != null)
                Destroy(proxy.gameObject);
        }
        dragProxies.Clear();
    }

    public void OnDropSuccess()
    {
        //
    }

    private void OnDestroy()
    {
        if (gridDataHandle.IsValid())
        {
            Addressables.Release(gridDataHandle);
        }

        if (battleUnitManager != null && unit != null)
        {
            battleUnitManager.UnregisterUnit(unit);
        }

        // 드래그 프록시 정리
        ClearDragProxies();
    }
}
