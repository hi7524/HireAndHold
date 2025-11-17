using UnityEngine;

public class GridCell : MonoBehaviour, ITestDroppable
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

    public bool CanDrop(ITestDraggable draggable)
    {
        return canDrop;
    }

    public void OnDragEnter(ITestDraggable draggable)
    {
        // GridUnit 배치 가능 여부 판정 및 판정에 따라 색상 변경
        var gridUnit = draggable.GameObject.GetComponent<GridUnit>();
        if (gridUnit != null)
        {
            canDrop = gridManager.CanPlaceUnit(GridPosition, gridUnit.GridData.GetOccupiedCells(), gridUnit.GridData.gridColor);
        }
    }

    public void OnDragExit(ITestDraggable draggable)
    {
        // 그리드 색상 변경
        gridManager.ClearAllGridsColor();
        gridManager.ChangeOccupiedCellColor();
    }

    public void OnDrop(ITestDraggable draggable)
    {
        // 드롭 가능 상태가 아닐 경우 배치 불가
        if (!canDrop)
            return;

        // 유닛이 아닐 경우 배치 불가
        var gridUnit = draggable.GameObject.GetComponent<GridUnit>();
        if (gridUnit == null)
            return;


        // 이전 위치의 색칠된 셀들 제거
        var previousCell = gridUnit.GetPreviousCell();
        if (previousCell != null)
        {
            gridManager.RemoveColoredCells(previousCell.GridPosition, gridUnit.GridData.GetOccupiedCells());
        }

        // 배치 대상 위치 스냅 
        draggable.GameObject.transform.position = transform.position;
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