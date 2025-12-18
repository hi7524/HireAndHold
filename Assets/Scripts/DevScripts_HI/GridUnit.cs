using System.Collections.Generic;
using DG.Tweening;
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

    // 머지 이펙트
    private Sequence mergeEffectSequence;


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

        // OverlapPoint로 해당 위치의 모든 collider를 확인
        Collider2D[] colliders = Physics2D.OverlapPointAll(transform.position);
        foreach (var collider in colliders)
        {
            GridCell cell = collider.GetComponent<GridCell>();
            if (cell != null)
            {
                // GridCell 위에 있을 때만 색상 업데이트
                Vector2Int currentGridPos = cell.GridPosition;
                gridManager.CanPlaceUnit(currentGridPos, GridData.GetOccupiedCells());
                return;
            }
        }

        // GridCell 위에 없으면 색상 초기화
        gridManager.ClearAllGridsColor();
        gridManager.ChangeOccupiedCellColor();
    }

    public void OnDragEnd()
    {
        SetActiveChildrenObj(false);

        // 머지 효과 중지
        StopMergeEffect();

        // 드래그 종료 시 그리드 색상 초기화
        if (gridManager != null)
        {
            gridManager.ClearAllGridsColor();
            gridManager.ChangeOccupiedCellColor();
        }
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

                // 색상과 아이콘 복원
                gridManager.OnFailed();

                // 이 유닛의 아이콘도 다시 설정
                gridManager.SetGridIcons(curGridCell.GridPosition, occupiedCells, UnitId, StarLevel);
            }

            // 유닛 위치를 원래 GridCell의 월드 좌표로 복원
            transform.position = curGridCell.transform.position;
            Physics2D.SyncTransforms();

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

    // 머지 가능 시 호출
    public void OnMergeAvailable()
    {
        StartMergeEffect();
    }

    // 머지 불가능 시 호출
    public void OnMergeUnavailable()
    {
        StopMergeEffect();
    }

    // 머지 이펙트 시작 (프리뷰 셀에 펄스 + 글로우)
    private void StartMergeEffect()
    {
        StopMergeEffect();

        if (childrenObj == null || childrenObj.Count == 0)
            return;

        mergeEffectSequence = DOTween.Sequence();

        foreach (var child in childrenObj)
        {
            if (child == null)
                continue;

            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // 각 셀의 초기 상태 저장
                Vector3 originalScale = child.localScale;
                Color originalColor = sr.color;

                // 펄스 + 글로우 시퀀스
                Sequence cellSequence = DOTween.Sequence()
                    .Append(child.DOScale(originalScale * 1.15f, 0.3f))
                    .Join(sr.DOFade(1f, 0.3f))
                    .Append(child.DOScale(originalScale, 0.3f))
                    .Join(sr.DOFade(0.7f, 0.4f))
                    .SetLoops(-1);

                // 메인 시퀀스에 추가 (모든 셀이 동시에 애니메이션)
                mergeEffectSequence.Join(cellSequence);
            }
        }
    }

    // 머지 이펙트 중지
    private void StopMergeEffect()
    {
        mergeEffectSequence?.Kill();
        mergeEffectSequence = null;

        // 모든 셀을 원래 스케일과 알파로 복원
        if (childrenObj == null)
            return;

        foreach (var child in childrenObj)
        {
            if (child == null)
                continue;

            child.DOKill();
            child.localScale = GameConstants.previewCellSizeObject * Vector3.one;

            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null && GridData != null)
            {
                sr.color = GridData.gridColor;
            }
        }
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
