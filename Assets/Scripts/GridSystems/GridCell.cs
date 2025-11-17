using UnityEngine;


public class GridCell : MonoBehaviour, ITestDroppable
{
    public Vector2Int GridPosition { get; private set; }
    public GameObject PlacedObject { get; private set; }

    private SpriteRenderer spriteRenderer;
    private GridManager gridManager;

    private bool canAccept = true;


    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetGridManager(GridManager gridManager)
    {
        this.gridManager = gridManager;
    }

    public void SetGridPosition(Vector2Int pos)
    {
        GridPosition = pos;
    }

    // 셀의 드롭 허용 여부 설정
    public void SetAcceptable(bool canAccept)
    {
        this.canAccept = canAccept;
    }

    public bool CanAccept(ITestDraggable draggable)
    {
        return canAccept;
    }

    public void OnDragEnter(ITestDraggable draggable)
    {
        var gridUnit = draggable.GameObject.GetComponent<GridUnit>();
        if (gridUnit != null)
        {
            canAccept = gridManager.CanPlaceUnit(GridPosition, gridUnit.GridData.GetOccupiedCells(), gridUnit.GridData.gridColor);
        }
    }

    public void OnDragExit(ITestDraggable draggable)
    {
        gridManager.ClearAllGridsColor();
        gridManager.SetOccupiedCellColor();
    }

    public void OnDrop(ITestDraggable draggable)
    {
        var gridUnit = draggable.GameObject.GetComponent<GridUnit>();

        if (gridUnit == null)
            return;

        if (!canAccept)
            return;

        var previousCell = gridUnit.GetPreviousCell();
        if (previousCell != null)
        {
            gridManager.RemoveColoredCells(previousCell.GridPosition, gridUnit.GridData.GetOccupiedCells());
        }

        draggable.GameObject.transform.position = transform.position;
        PlacedObject = draggable.GameObject;

        gridUnit.SetCurrentGridCell(this);

        var occupiedCells = gridUnit.GridData.GetOccupiedCells();

        gridManager.SetGridState(GridPosition, GridState.Occupied);
        gridManager.OccupiedCellAndColor(GridPosition, gridUnit.GridData.gridColor);

        foreach (var relativePos in occupiedCells)
        {
            Vector2Int absolutePos = GridPosition + relativePos;
            gridManager.SetGridState(absolutePos, GridState.Occupied);
            gridManager.OccupiedCellAndColor(absolutePos, gridUnit.GridData.gridColor);
        }

        gridManager.ClearAllGridsColor();
    }

    public void ClearObject()
    {
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

    public void SetColor(Color color)
    {
        spriteRenderer.color = color;
    }
}
