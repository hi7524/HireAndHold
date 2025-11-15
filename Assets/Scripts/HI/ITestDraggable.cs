using UnityEngine;

public interface ITestDraggable
{
    bool IsDraggable { get; }
    bool RequireDropZone { get; }
    GameObject GameObject { get; }

    void OnDragStart();
    void OnDrag();
    void OnDragEnd();
}