using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableUI : MonoBehaviour, IDraggable, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private bool requireDropZone = true;

    private Canvas canvas;
    private RectTransform rectTransform;

    private Transform originalParent;
    private Vector2 originalPos;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPos = rectTransform.anchoredPosition;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (requireDropZone && !TryDropOnTarget(eventData))
        {
            ReturnToOriginalPosition();
        }
    }

    private bool TryDropOnTarget(PointerEventData eventData)
    {
        // UI Raycast
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            var droppable = result.gameObject.GetComponent<IDroppable>();
            if (droppable != null && droppable.CanDrop(this))
            {
                droppable.OnDrop(this);
                return true;
            }
        }

        // Physics2D Raycast
        float spriteZPosition = 0f;
        float distanceFromCamera = Mathf.Abs(spriteZPosition - Camera.main.transform.position.z);

        Vector3 screenPos = eventData.position;
        screenPos.z = distanceFromCamera;
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

        Collider2D collider = Physics2D.OverlapPoint(worldPos);

        if (collider != null)
        {
            var droppable = collider.GetComponent<IDroppable>();
            if (droppable != null && droppable.CanDrop(this))
            {
                droppable.OnDrop(this);
                gameObject.SetActive(false);
                return true;
            }
        }

        return false;
    }

    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPos;
    }
}
