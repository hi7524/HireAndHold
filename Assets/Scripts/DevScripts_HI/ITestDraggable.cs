using UnityEngine;

public interface ITestDraggable
{
    bool IsDraggable { get; }
    GameObject GameObject { get; }

    void OnDragStart();
    void OnDrag();
    void OnDragEnd();
    void OnDropFailed();
}