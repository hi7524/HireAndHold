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
        var draggedTransform = (draggable as MonoBehaviour)?.transform;
        if (draggedTransform != null)
        {
            draggedTransform.SetParent(transform);
            draggedTransform.localPosition = Vector3.zero;
        }
    }
}