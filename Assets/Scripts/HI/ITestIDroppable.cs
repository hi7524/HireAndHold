using UnityEngine;

public interface ITestDroppable
{
    bool CanAccept(ITestDraggable draggable);

    void OnDrop(ITestDraggable draggable);
    void OnDragEnter(ITestDraggable draggable);
    void OnDragExit(ITestDraggable draggable);
}