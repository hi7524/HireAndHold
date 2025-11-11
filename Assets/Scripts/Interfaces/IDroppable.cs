using UnityEngine;

public interface IDroppable
{
    bool CanDrop(IDraggable draggable);
    void OnDrop(IDraggable draggable);
}