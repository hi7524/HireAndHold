using UnityEngine;

public interface ITestDraggable
{
    bool IsDraggable { get; }

    void OnDragStart();
    void OnDrag();
    void OnDragEnd();
    GameObject GameObject { get; }
}