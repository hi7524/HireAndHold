using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DragTestObject : MonoBehaviour, ITestDraggable
{
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