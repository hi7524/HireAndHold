using UnityEngine;
using DG.Tweening;

public class GridCell : MonoBehaviour, IDroppable
{
    public Vector2Int GridPosition { get; private set; }

    private GridManager gridManager;
    private GameObject placedObject;
    private Vector3 originalSize;

    private SpriteRenderer spriteRenderer;

    private bool canDrop = true;


    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        originalSize = gameObject.transform.localScale;
    }

    public void SetGridManager(GridManager gridManager)
    {
        this.gridManager = gridManager;
    }

    public GridManager GetGridManager()
    {
        return gridManager;
    }

    public void SetGridPosition(Vector2Int pos)
    {
        GridPosition = pos;
    }

    public void SetAcceptable(bool canAccept)
    {
        this.canDrop = canAccept;
    }

    public void SetColor(Color color)
    {
        spriteRenderer.color = color;
    }

    // 버프 활성화 애니메이션 (스케일)
    public void PlayBuffActivationAnimation(float delay = 0f)
    {
        transform.DOKill();

        transform.localScale = originalSize;

        Sequence sequence = DOTween.Sequence();
        sequence.SetDelay(delay);
        sequence.Append(transform.DOScale(originalSize * 1.2f, 0.1f).SetEase(Ease.OutQuad));
        sequence.Append(transform.DOScale(originalSize, 0.12f).SetEase(Ease.InQuad));
        sequence.OnComplete(() =>
        {
            transform.localScale = originalSize;
        });
    }

    public bool CanDrop(IDraggable draggable)
    {
        return canDrop;
    }

    public void OnDragEnter(IDraggable draggable)
    {
        // GridUnit 배치 가능 여부 판정 및 판정에 따라 색상 변경
        var gridUnit = draggable.GameObject.GetComponent<GridUnit>();
        if (gridUnit != null)
        {
            // 합성 가능 여부 체크
            if (placedObject != null)
            {
                var existingUnit = placedObject.GetComponent<GridUnit>();
                if (existingUnit != null && CanMerge(existingUnit, gridUnit))
                {
                    canDrop = true;
                    return;
                }
            }

            canDrop = gridManager.CanPlaceUnit(GridPosition, gridUnit.GridData.GetOccupiedCells());
        }

        var inventorySlot = draggable.GameObject.GetComponent<UnitInventorySlot>();
        if (inventorySlot != null)
        {
            // 합성 가능 여부 체크
            if (placedObject != null)
            {
                var existingUnit = placedObject.GetComponent<GridUnit>();
                if (existingUnit != null && CanMergeWithInventorySlot(existingUnit, inventorySlot))
                {
                    canDrop = true;
                    return;
                }
            }

            canDrop = gridManager.CanPlaceUnit(GridPosition, inventorySlot.GridData.GetOccupiedCells());
        }

        var draggableUnitUi = draggable.GameObject.GetComponent<DraggableGridUnitUi>();
        if (draggableUnitUi != null)
        {
            // 합성 가능 여부 체크
            if (placedObject != null)
            {
                var existingUnit = placedObject.GetComponent<GridUnit>();
                if (existingUnit != null && CanMergeWithUi(existingUnit, draggableUnitUi))
                {
                    canDrop = true;
                    return;
                }
            }

            canDrop = gridManager.CanPlaceUnit(GridPosition, draggableUnitUi.GridData.GetOccupiedCells());
        }
    }

    public void OnDragExit(IDraggable draggable)
    {
        // 그리드 색상 변경
        gridManager.ClearAllGridsColor();
        gridManager.ChangeOccupiedCellColor();
    }

    public void OnDrop(IDraggable draggable)
    {
        // 드롭 가능 상태가 아닐 경우 배치 불가
        if (!canDrop)
            return;

        // 유닛 또는 인벤토리 슬롯 또는 DraggableGridUnitUi 아닐 경우 배치 불가
        var gridUnit = draggable.GameObject.GetComponent<GridUnit>();
        var inventorySlot = draggable.GameObject.GetComponent<UnitInventorySlot>();
        var draggableUnitUi = draggable.GameObject.GetComponent<DraggableGridUnitUi>();
        if (gridUnit == null && inventorySlot == null && draggableUnitUi == null)
            return;


        // GridUnit 처리
        if (gridUnit != null)
        {
            // 합성 체크: 이미 유닛이 배치되어 있는 경우
            if (placedObject != null)
            {
                var existingUnit = placedObject.GetComponent<GridUnit>();
                if (existingUnit != null)
                {
                    if (TryMergeUnits(existingUnit, gridUnit))
                    {
                        // 합성 성공 - 색상 업데이트
                        Debug.Log("머지");
                        gridManager.OnMergedUnits();
                        gridManager.ClearAllGridsColor();
                        gridManager.ChangeOccupiedCellColor();
                        return;
                    }
                    else
                    {
                        // 합성 불가능하면 드롭 실패
                        draggable.OnDropFailed();
                        return;
                    }
                }
            }

            // 이전 위치의 색칠된 셀들 제거
            var previousCell = gridUnit.GetPreviousCell();
            if (previousCell != null)
            {
                gridManager.RemoveColoredCells(previousCell.GridPosition, gridUnit.GridData.GetOccupiedCells());
            }

            // 배치 대상 위치 스냅
            draggable.GameObject.transform.position = transform.position;
            Physics2D.SyncTransforms(); // Collider2D 위치 동기화
            placedObject = draggable.GameObject;

            gridUnit.SetCurrentGridCell(this);

            // GridManager에 그리드 정보 전달
            var occupiedCells = gridUnit.GridData.GetOccupiedCells();

            gridManager.SetGridState(GridPosition, GridState.Occupied);
            gridManager.SetOccupiedCellAndColor(GridPosition, gridUnit.GridData.gridColor);

            foreach (var relativePos in occupiedCells)
            {
                Vector2Int absolutePos = GridPosition + relativePos;
                gridManager.SetGridState(absolutePos, GridState.Occupied);
                gridManager.SetOccupiedCellAndColor(absolutePos, gridUnit.GridData.gridColor);
            }
        }

        // UnitInventorySlot 처리 - GridUnit 생성
        if (inventorySlot != null)
        {
            // 합성 체크: 이미 유닛이 배치되어 있는 경우
            if (placedObject != null)
            {
                var existingUnit = placedObject.GetComponent<GridUnit>();
                if (existingUnit != null)
                {
                    if (TryMergeWithInventorySlot(existingUnit, inventorySlot))
                    {
                        // 합성 성공 - 색상 업데이트
                        Debug.Log("머지");
                        gridManager.OnMergedUnits();
                        gridManager.ClearAllGridsColor();
                        gridManager.ChangeOccupiedCellColor();
                        return;
                    }
                    else
                    {
                        // 합성 불가능하면 드롭 실패
                        draggable.OnDropFailed();
                        return;
                    }
                }
            }

            // GridManager를 통해 GridUnit 생성
            var newGridUnit = gridManager.SpawnGridUnit(transform.position, inventorySlot.GridData);

            if (newGridUnit == null)
            {
                draggable.OnDropFailed();
                return;
            }

            newGridUnit.SetUnitID(inventorySlot.UnitId, inventorySlot.StarLevel);
            newGridUnit.SetInventoryPlaceable(true);

            // 생성된 GridUnit을 배치
            placedObject = newGridUnit.GameObject;
            newGridUnit.SetCurrentGridCell(this);

            // GridManager에 그리드 정보 전달
            var occupiedCells = newGridUnit.GridData.GetOccupiedCells();

            gridManager.SetGridState(GridPosition, GridState.Occupied);
            gridManager.SetOccupiedCellAndColor(GridPosition, newGridUnit.GridData.gridColor);

            foreach (var relativePos in occupiedCells)
            {
                Vector2Int absolutePos = GridPosition + relativePos;
                gridManager.SetGridState(absolutePos, GridState.Occupied);
                gridManager.SetOccupiedCellAndColor(absolutePos, newGridUnit.GridData.gridColor);
            }
        }

        // DraggableGridUnitUi 처리 - GridUnit 생성
        if (draggableUnitUi != null)
        {
            // 합성 체크: 이미 유닛이 배치되어 있는 경우
            if (placedObject != null)
            {
                var existingUnit = placedObject.GetComponent<GridUnit>();
                if (existingUnit != null)
                {
                    if (TryMergeWithUi(existingUnit, draggableUnitUi))
                    {
                        // 합성 성공 - UI 비활성화 및 색상 업데이트
                        Debug.Log("머지");
                        gridManager.OnMergedUnits();
                        gridManager.ClearAllGridsColor();
                        gridManager.ChangeOccupiedCellColor();
                        return;
                    }
                    else
                    {
                        // 합성 불가능하면 드롭 실패
                        draggable.OnDropFailed();
                        return;
                    }
                }
            }

            // GridManager를 통해 GridUnit 생성
            var newGridUnit = gridManager.SpawnGridUnit(transform.position, draggableUnitUi.GridData);

            if (newGridUnit == null)
            {
                draggable.OnDropFailed();
                return;
            }

            newGridUnit.SetUnitID(draggableUnitUi.UnitId, draggableUnitUi.StarLevel);

            // 레벨업 보상 유닛인 경우 GridManager를 통해 알림
            if (draggableUnitUi.DraggableUnitType == DraggableUnitType.LevelUp)
            {
                gridManager.NotifyLevelUpRewardUnitSpawned(newGridUnit);
                newGridUnit.SetInventoryPlaceable(false);
            }

            // 생성된 GridUnit을 배치
            placedObject = newGridUnit.GameObject;
            newGridUnit.SetCurrentGridCell(this);

            // GridManager에 그리드 정보 전달
            var occupiedCells = newGridUnit.GridData.GetOccupiedCells();

            gridManager.SetGridState(GridPosition, GridState.Occupied);
            gridManager.SetOccupiedCellAndColor(GridPosition, newGridUnit.GridData.gridColor);

            foreach (var relativePos in occupiedCells)
            {
                Vector2Int absolutePos = GridPosition + relativePos;
                gridManager.SetGridState(absolutePos, GridState.Occupied);
                gridManager.SetOccupiedCellAndColor(absolutePos, newGridUnit.GridData.gridColor);
            }
        }

        gridManager.ClearAllGridsColor();
        gridManager.ChangeOccupiedCellColor();
    }

    public void ClearObject()
    {
        gridManager.CopyColoredCellToTemp();

        // 유닛이 차지했던 모든 셀을 Empty로 설정
        if (placedObject != null)
        {
            var gridUnit = placedObject.GetComponent<GridUnit>();
            if (gridUnit != null)
            {
                var occupiedCells = gridUnit.GridData.GetOccupiedCells();

                gridManager.SetGridState(GridPosition, GridState.Empty);

                foreach (var relativePos in occupiedCells)
                {
                    Vector2Int absolutePos = GridPosition + relativePos;
                    gridManager.SetGridState(absolutePos, GridState.Empty);
                }

                gridManager.RemoveColoredCells(GridPosition, occupiedCells);
            }
        }

        placedObject = null;
    }

    // 드롭 실패 시 PlacedObject 복원
    public void RestorePlacedObject(GameObject obj)
    {
        placedObject = obj;
    }

    // 합성 가능 여부 체크
    private bool CanMerge(GridUnit existingUnit, GridUnit draggingUnit)
    {
        // 같은 유닛 ID
        if (existingUnit.UnitId != draggingUnit.UnitId)
            return false;

        // 같은 성급
        if (existingUnit.StarLevel != draggingUnit.StarLevel)
            return false;

        // 3성 이상 합성 불가
        if (existingUnit.StarLevel >= 3)
            return false;

        return true;
    }

    // 유닛 합성 처리
    private bool TryMergeUnits(GridUnit existingUnit, GridUnit draggingUnit)
    {
        if (!CanMerge(existingUnit, draggingUnit))
            return false;

        // 합성 처리 성급 업그레이드
        int newStarLevel = existingUnit.StarLevel + 1;
        existingUnit.SetUnitID(existingUnit.UnitId, newStarLevel);

        // 드래그 중이던 유닛 삭제
        Destroy(draggingUnit.gameObject);
        return true;
    }

    // DraggableGridUnitUi와 합성 가능 여부 체크
    private bool CanMergeWithUi(GridUnit existingUnit, DraggableGridUnitUi draggableUnitUi)
    {
        // 같은 유닛 ID
        if (existingUnit.UnitId != draggableUnitUi.UnitId)
            return false;

        // 같은 성급
        if (existingUnit.StarLevel != draggableUnitUi.StarLevel)
            return false;

        // 3성 이상 합성 불가
        if (existingUnit.StarLevel >= 3)
            return false;

        return true;
    }

    // DraggableGridUnitUi와 유닛 합성 처리
    private bool TryMergeWithUi(GridUnit existingUnit, DraggableGridUnitUi draggableUnitUi)
    {
        if (!CanMergeWithUi(existingUnit, draggableUnitUi))
            return false;

        // 합성 처리: 성급 업그레이드
        int newStarLevel = existingUnit.StarLevel + 1;
        existingUnit.SetUnitID(existingUnit.UnitId, newStarLevel);

        return true;
    }

    // UnitInventorySlot과 합성 가능 여부 체크
    private bool CanMergeWithInventorySlot(GridUnit existingUnit, UnitInventorySlot inventorySlot)
    {
        // 같은 유닛 ID
        if (existingUnit.UnitId != inventorySlot.UnitId)
            return false;

        // 같은 성급
        if (existingUnit.StarLevel != inventorySlot.StarLevel)
            return false;

        // 3성 이상 합성 불가
        if (existingUnit.StarLevel >= 3)
            return false;

        return true;
    }

    // UnitInventorySlot과 유닛 합성 처리
    private bool TryMergeWithInventorySlot(GridUnit existingUnit, UnitInventorySlot inventorySlot)
    {
        if (!CanMergeWithInventorySlot(existingUnit, inventorySlot))
            return false;

        // 합성 처리: 성급 업그레이드
        int newStarLevel = existingUnit.StarLevel + 1;
        existingUnit.SetUnitID(existingUnit.UnitId, newStarLevel);

        return true;
    }
}