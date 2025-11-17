using UnityEngine;

public class DropTestObject : MonoBehaviour, ITestDroppable
{
    public bool CanAccept(ITestDraggable draggable)
    {
        return true;
    }

    public void OnDragEnter(ITestDraggable draggable)
    {
    }

    public void OnDragExit(ITestDraggable draggable)
    {
    }

    public void OnDrop(ITestDraggable draggable)
    {
        draggable.GameObject.transform.position = transform.position;
    }
}
