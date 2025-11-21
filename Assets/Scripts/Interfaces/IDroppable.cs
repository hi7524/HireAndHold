using UnityEngine;

public interface IDroppable
{
    bool CanDrop(IDraggable draggable);

    void OnDrop(IDraggable draggable);
    void OnDragEnter(IDraggable draggable);
    void OnDragExit(IDraggable draggable);
}