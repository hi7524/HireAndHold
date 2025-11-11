using UnityEngine;
using UnityEngine.EventSystems;

public interface IDraggable
{
    void OnBeginDrag(PointerEventData eventData);
    void OnDrag(PointerEventData eventData);
    void OnEndDrag(PointerEventData eventData);
}