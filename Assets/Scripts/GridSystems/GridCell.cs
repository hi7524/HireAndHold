using UnityEngine;

public class GridCell : MonoBehaviour, IDroppable
{
    public Vector2Int GridPosition { get; private set; }

    private GridManager gridManager;
    private GameObject PlacedObject;

    private SpriteRenderer spriteRenderer;

    private bool canDrop = true;


    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
            canDrop = gridManager.CanPlaceUnit(GridPosition, gridUnit.GridData.GetOccupiedCells());
        }

        var inventorySlot = draggable.GameObject.GetComponent<UnitInventorySlot>();
        if (inventorySlot != null)
        {
            canDrop = gridManager.CanPlaceUnit(GridPosition, inventorySlot.GridData.GetOccupiedCells());
        }

        var draggableUnitUi = draggable.GameObject.GetComponent<DraggableGridUnitUi>();
        if (draggableUnitUi != null)
        {
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
            // 이전 위치의 색칠된 셀들 제거
            var previousCell = gridUnit.GetPreviousCell();
            if (previousCell != null)
            {
                gridManager.RemoveColoredCells(previousCell.GridPosition, gridUnit.GridData.GetOccupiedCells());
            }

            // 배치 대상 위치 스냅
            draggable.GameObject.transform.position = transform.position;
            Physics2D.SyncTransforms(); // Collider2D 위치 동기화
            PlacedObject = draggable.GameObject;

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
            // GridManager를 통해 GridUnit 생성
            var newGridUnit = gridManager.SpawnGridUnit(transform.position, inventorySlot.GridData);

            if (newGridUnit == null)
            {
                draggable.OnDropFailed();
                return;
            }

            newGridUnit.SetUnitID(inventorySlot.UnitId);

            // 생성된 GridUnit을 배치
            PlacedObject = newGridUnit.GameObject;
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
            // GridManager를 통해 GridUnit 생성
            var newGridUnit = gridManager.SpawnGridUnit(transform.position, draggableUnitUi.GridData);

            if (newGridUnit == null)
            {
                draggable.OnDropFailed();
                return;
            }

            newGridUnit.SetUnitID(draggableUnitUi.UnitId);

            // 생성된 GridUnit을 배치
            PlacedObject = newGridUnit.GameObject;
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
        if (PlacedObject != null)
        {
            var gridUnit = PlacedObject.GetComponent<GridUnit>();
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

        PlacedObject = null;
    }
}