using UnityEngine;
using UnityEngine.UI;

public class SpriteDropZone : MonoBehaviour, IDroppable
{
    [SerializeField] private bool acceptAllDraggables = true;

    public bool CanDrop(IDraggable draggable)
    {
        return acceptAllDraggables;
    }

    public void OnDrop(IDraggable draggable)
    {
        Debug.Log("동작 실행");
    }
}