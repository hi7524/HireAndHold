using UnityEngine;

public class DropTestObject : MonoBehaviour, ITestDroppable
{
    public bool CanAccept(ITestDraggable draggable)
    {
        return true;
    }

    public void OnDragEnter(ITestDraggable draggable)
    {
        Debug.Log("놓는중");
    }

    public void OnDragExit(ITestDraggable draggable)
    {
        Debug.Log("나감");
    }

    public void OnDrop(ITestDraggable draggable)
    {
        draggable.GameObject.transform.position = transform.position;
    }
}
