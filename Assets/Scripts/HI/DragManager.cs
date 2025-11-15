using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DragManager : MonoBehaviour
{
    [SerializeField] private LayerMask draggableLayer;

    private Camera mainCamera;
    private ITestDraggable dragTarget;

    private bool isTargetUI = false;


    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Pointer.current != null)
        {
            // 누르기 시작
            if (Pointer.current.press.wasPressedThisFrame)
            {
                dragTarget = DetectObject();
                dragTarget?.OnDragStart();
            }

            // 누르는 중
            if (Pointer.current.press.isPressed && dragTarget != null)
            {
                dragTarget.OnDrag();
                MoveDraggingObject(dragTarget.GameObject);
            }

            // 떼는 순간
            if (Pointer.current.press.wasReleasedThisFrame)
            {
                dragTarget?.OnDragEnd();
                dragTarget = null;
            }
        }
    }

    private ITestDraggable DetectObject()
    {
        isTargetUI = false;
        Vector2 pointerPosition = Pointer.current.position.ReadValue();

        ITestDraggable uiDraggable = DetectUIObject(pointerPosition);
        if (uiDraggable != null)
            return uiDraggable;

        ITestDraggable worldDraggable = DetectWorldObject(pointerPosition);
        return worldDraggable;
    }

    // 드래그 가능한 UI 감지
    private ITestDraggable DetectUIObject(Vector2 screenPosition)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            var draggable = result.gameObject.GetComponent<ITestDraggable>();
            if (draggable != null && draggable.IsDraggable)
            {
                isTargetUI = true;
                return draggable;
            }
        }

        return null;
    }

    // 드래그 가능한 GameObject 감지
    private ITestDraggable DetectWorldObject(Vector2 screenPosition)
    {
        Vector2 worldPoint = mainCamera.ScreenToWorldPoint(screenPosition);

        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPoint, Vector2.zero, Mathf.Infinity, draggableLayer);

        if (hits.Length == 0)
            return null;

        ITestDraggable topDraggable = null;
        int highestSortingOrder = int.MinValue;

        foreach (var hit in hits)
        {
            var draggable = hit.collider.GetComponent<ITestDraggable>();
            if (draggable == null || !draggable.IsDraggable)
                continue;

            var spriteRenderer = hit.collider.GetComponent<SpriteRenderer>();
            int sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;

            if (topDraggable == null || sortingOrder > highestSortingOrder)
            {
                topDraggable = draggable;
                highestSortingOrder = sortingOrder;
            }
        }

        return topDraggable;
    }

    // 오브젝트 이동
    private void MoveDraggingObject(GameObject targetObj)
    {
        Vector2 screenPos = Pointer.current.position.ReadValue();

        if (isTargetUI)
        {
            RectTransform rectTransform = targetObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.position = screenPos;
            }
        }
        else
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;
            targetObj.transform.position = worldPos;
            Physics2D.SyncTransforms();
        }
    }
}