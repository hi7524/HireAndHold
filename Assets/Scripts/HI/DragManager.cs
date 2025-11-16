using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DragManager : MonoBehaviour
{
    [SerializeField] private LayerMask draggableLayer;

    private Camera mainCamera;
    private ITestDraggable dragTarget;
    private Vector3 originalPosition;

    private bool isTargetUI = false;
    private ITestDroppable currentDropTarget;


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
                if (dragTarget != null)
                {
                    originalPosition = dragTarget.GameObject.transform.position;
                    dragTarget.OnDragStart();
                }
            }

            // 누르는 중
            if (Pointer.current.press.isPressed && dragTarget != null)
            {
                dragTarget.OnDrag();
                MoveDraggingObject(dragTarget.GameObject);

                // 드롭 타겟 감지 및 Enter/Exit 이벤트 처리
                ITestDroppable newDropTarget = DetectDropTarget();

                if (newDropTarget != currentDropTarget)
                {
                    // 이전 타겟에서 나감
                    if (currentDropTarget != null)
                    {
                        currentDropTarget.OnDragExit(dragTarget);
                    }

                    // 새로운 타겟에 진입
                    if (newDropTarget != null)
                    {
                        newDropTarget.OnDragEnter(dragTarget);
                    }

                    currentDropTarget = newDropTarget;
                }
            }

            // 떼는 순간
            if (Pointer.current.press.wasReleasedThisFrame)
            {
                if (dragTarget != null)
                {
                    ITestDroppable dropTarget = DetectDropTarget();

                    // 드롭 성공
                    if (dropTarget != null && dropTarget.CanAccept(dragTarget))
                    {
                        dropTarget.OnDrop(dragTarget);
                    }
                    // 드롭 실패
                    else
                    {
                        dragTarget.GameObject.transform.position = originalPosition;
                        if (!isTargetUI)
                        {
                            Physics2D.SyncTransforms();
                        }
                    }

                    // Exit 이벤트 처리 (드롭 타겟이 있었다면)
                    if (currentDropTarget != null)
                    {
                        currentDropTarget.OnDragExit(dragTarget);
                        currentDropTarget = null;
                    }

                    dragTarget.OnDragEnd();
                    dragTarget = null;
                }
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

    // 드롭 가능 대상 감지
    private ITestDroppable DetectDropTarget()
    {
        Vector2 pointerPosition = Pointer.current.position.ReadValue();

        ITestDroppable uiDroppable = DetectUIDropTarget(pointerPosition);
        if (uiDroppable != null)
            return uiDroppable;

        ITestDroppable worldDroppable = DetectWorldDropTarget(pointerPosition);
        return worldDroppable;
    }

    // 드롭 가능 UI 감지
    private ITestDroppable DetectUIDropTarget(Vector2 screenPosition)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            var droppable = result.gameObject.GetComponent<ITestDroppable>();
            if (droppable != null)
            {
                return droppable;
            }
        }

        return null;
    }

    // 드롭 가능 GameObject 감지
    private ITestDroppable DetectWorldDropTarget(Vector2 screenPosition)
    {
        Vector2 worldPoint = mainCamera.ScreenToWorldPoint(screenPosition);

        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPoint, Vector2.zero, Mathf.Infinity, draggableLayer);

        if (hits.Length == 0)
            return null;

        ITestDroppable topDroppable = null;
        int highestSortingOrder = int.MinValue;

        foreach (var hit in hits)
        {
            var droppable = hit.collider.GetComponent<ITestDroppable>();
            if (droppable == null)
                continue;

            var spriteRenderer = hit.collider.GetComponent<SpriteRenderer>();
            int sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;

            if (topDroppable == null || sortingOrder > highestSortingOrder)
            {
                topDroppable = droppable;
                highestSortingOrder = sortingOrder;
            }
        }

        return topDroppable;
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