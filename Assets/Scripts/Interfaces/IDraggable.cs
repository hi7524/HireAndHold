using UnityEngine;
using UnityEngine.EventSystems;

public interface IDraggable
{
    GameObject GameObject { get; }
    void OnBeginDrag(PointerEventData eventData);
    void OnDrag(PointerEventData eventData);
    void OnEndDrag(PointerEventData eventData);
}