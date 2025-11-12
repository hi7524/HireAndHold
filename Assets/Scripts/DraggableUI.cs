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
    private int originalSiblingIndex;
    private IDroppable currentDroppable;

    public GameObject GameObject => gameObject;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPos = rectTransform.anchoredPosition;
        originalSiblingIndex = transform.GetSiblingIndex();

        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        UpdateCurrentDroppable(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        bool dropped = false;

        if (currentDroppable != null)
        {
            if (currentDroppable.CanDrop(this))
            {
                currentDroppable.OnDrop(this);
                dropped = true;
            }

            currentDroppable.OnDragExit(this);
            currentDroppable = null;
        }

        if (requireDropZone && !dropped)
        {
            ReturnToOriginalPosition();
        }
    }

    private void UpdateCurrentDroppable(PointerEventData eventData)
    {
        IDroppable newDroppable = FindDroppableAtPosition(eventData);

        if (newDroppable != currentDroppable)
        {
            currentDroppable?.OnDragExit(this);
            currentDroppable = newDroppable;
            currentDroppable?.OnDragEnter(this);
        }
    }

    private IDroppable FindDroppableAtPosition(PointerEventData eventData)
    {
        // UI Raycast
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject == gameObject)
                continue;

            var droppable = result.gameObject.GetComponent<IDroppable>();
            if (droppable != null)
            {
                return droppable;
            }
        }

        // Physics2D Raycast
        if (Camera.main != null)
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            Collider2D collider = Physics2D.OverlapPoint(worldPos);

            if (collider != null)
            {
                return collider.GetComponent<IDroppable>();
            }
        }

        return null;
    }

    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalPos;
    }
}
