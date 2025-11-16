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


    public void OnDragStart()
    {
        curGridCell?.ClearObject();
        curGridCell = null;
    }

    public void OnDrag()
    {
    }

    public void OnDragEnd()
    {
    }

    public void SetCurrentGridCell(GridCell cell)
    {
        curGridCell = cell;
    }
}