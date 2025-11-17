using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GridUnit : MonoBehaviour, ITestDraggable
{
    [SerializeField] private UnitGridData gridData;

    // 그리드 데이터
    public UnitGridData GridData => gridData;

    // 드래그
    public bool IsDraggable => true;
    public bool RequireDropZone => true;
    public GameObject GameObject => gameObject;

    private GridCell curGridCell;
    private GridCell previousGridCell;


    public void OnDragStart()
    {
        previousGridCell = curGridCell;
        curGridCell?.ClearObject();
        curGridCell = null;
    }

    public void OnDrag()
    {
    }

    public void OnDragEnd()
    {
    }

    public void OnDropFailed()
    {
        curGridCell = previousGridCell;

        // 드롭 실패 시 원래 그리드 색상 복원
        if (curGridCell != null)
        {
            var gridManager = curGridCell.GetGridManager();
            if (gridManager != null)
            {
                gridManager.OnFailed();
            }
        }
    }

    public void SetCurrentGridCell(GridCell cell)
    {
        curGridCell = cell;
    }

    public GridCell GetPreviousCell()
    {
        return previousGridCell;
    }
}