using UnityEngine;

public interface IDraggable
{
    bool IsDraggable { get; }
    GameObject GameObject { get; }

    void OnDragStart();
    void OnDrag();
    void OnDragEnd();
    void OnDropSuccess();
    void OnDropFailed();
}