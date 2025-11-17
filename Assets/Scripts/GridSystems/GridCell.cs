using UnityEngine;


public class GridCell : MonoBehaviour, ITestDroppable
{
    public Vector2Int GridPosition { get; private set; }
    public GameObject PlacedObject {  get; private set; }

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
    }

    public void OnDrop(ITestDraggable draggable)
    {
        if (!canAccept)
            return;

        draggable.GameObject.transform.position = transform.position;
        PlacedObject = draggable.GameObject;

        // 드래그 가능한 오브젝트에 현재 셀 알려주기
        var gridUnit = draggable.GameObject.GetComponent<GridUnit>();
        if (gridUnit != null)
        {
            gridUnit.SetCurrentGridCell(this);

            var occupiedCells = gridUnit.GridData.GetOccupiedCells();

            gridManager.SetGridState(GridPosition, GridState.Occupied); // 중심
            foreach (var relativePos in occupiedCells) // 나머지
            {
                Vector2Int absolutePos = GridPosition + relativePos;
                gridManager.SetGridState(absolutePos, GridState.Occupied);
            }
        }
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
            }
        }

        PlacedObject = null;
    }

    public void SetColor(Color color)
    {
        spriteRenderer.color = color;
    }
}
