using UnityEngine;
using UnityEngine.UI;

public class UIDropZone : MonoBehaviour, IDroppable
{
    [SerializeField] private bool acceptAllDraggables = true;


    public bool CanDrop(IDraggable draggable)
    {
        return acceptAllDraggables;
    }

    public void OnDrop(IDraggable draggable)
    {
        var draggedTransform = draggable.GameObject.transform;
        draggedTransform.SetParent(transform);
        draggedTransform.localPosition = Vector3.zero;
    }

    public void OnDragEnter(IDraggable draggable)
    {
        
    }

    public void OnDragExit(IDraggable draggable)
    {
        
    }
}