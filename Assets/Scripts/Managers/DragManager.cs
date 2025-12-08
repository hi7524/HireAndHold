using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DragManager : MonoBehaviour
{
    [SerializeField] private LayerMask draggableLayer;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private bool isDragEnabled = false;

    private Camera mainCamera;
    private DragState dragState;
    private IDroppable currentDropTarget;

    private Camera MainCamera
    {
        get
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            return mainCamera;
        }
    }

    private struct DragState
    {
        public IDraggable Target;
        public Vector3 OriginalPosition;
        public bool IsUI;
        public Transform OriginalParent;
        public int OriginalSiblingIndex;

        public readonly bool IsDragging => Target != null;

        public void Reset()
        {
            Target = null;
            OriginalPosition = Vector3.zero;
            IsUI = false;
            OriginalParent = null;
            OriginalSiblingIndex = 0;
        }
    }

    // -----------------------------
    // 🔥 Pointer.current 제거 버전
    // -----------------------------

    private bool IsPressed()
    {
        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.press.isPressed;

        return Mouse.current?.leftButton.isPressed == true;
    }

    private bool PressedThisFrame()
    {
        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        return Mouse.current?.leftButton.wasPressedThisFrame == true;
    }

    private bool ReleasedThisFrame()
    {
        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;

        return Mouse.current?.leftButton.wasReleasedThisFrame == true;
    }

    private Vector2 GetPointerPosition()
    {
        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        return Mouse.current.position.ReadValue();
    }

    // -----------------------------
    // 🔥 드래그 Update (InputSystem-safe)
    // -----------------------------
    private void Update()
    {
        if (!isDragEnabled)
            return;

        if (PressedThisFrame())
        {
            HandleDragStart();
        }
        else if (IsPressed() && dragState.IsDragging)
        {
            HandleDragging();
        }
        else if (ReleasedThisFrame() && dragState.IsDragging)
        {
            HandleDragEnd();
        }
    }

    // 드래그 활성화/비활성화 설정
    public void SetDragEnabled(bool value)
    {
        isDragEnabled = value;
    }

    // 현재 포인터 위치 가져오기 (Pointer.current 또는 Touchscreen 폴백)
    private Vector2 GetCurrentPointerPosition()
    {
        var pointer = Pointer.current;
        if (pointer != null)
            return pointer.position.ReadValue();

        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
            return touchscreen.primaryTouch.position.ReadValue();

        return Vector2.zero;
    }

    // 드래그 시작 처리: 드래그 대상 감지 및 초기 상태 설정
    private void HandleDragStart()
    {
        Vector2 pointerPosition = GetPointerPosition();
        dragState.Target = DetectDraggable(pointerPosition, out dragState.IsUI);

        if (!dragState.IsDragging)
            return;

        dragState.OriginalPosition = dragState.Target.GameObject.transform.position;

        if (dragState.IsUI && rootCanvas != null)
        {
            MoveUIToCanvasTop(dragState.Target.GameObject.transform);
        }

        dragState.Target.OnDragStart();
    }

    // 드래그 중 처리: 오브젝트 이동 및 드롭 타겟 업데이트
    private void HandleDragging()
    {
        dragState.Target.OnDrag();
        MoveDraggingObject(dragState.Target.GameObject);
        UpdateDropTarget();
    }

    // 드래그 종료 처리: 드롭 성공/실패 처리 및 상태 리셋
    private void HandleDragEnd()
    {
        Vector2 pointerPosition = GetPointerPosition();
        IDroppable dropTarget = DetectDropTarget(pointerPosition);

        bool dropSuccess = dropTarget != null && dropTarget.CanDrop(dragState.Target);

        if (dropSuccess)
        {
            dropTarget.OnDrop(dragState.Target);
            dragState.Target.OnDropSuccess();
        }
        else
        {
            ResetDraggablePosition();
            dragState.Target.OnDropFailed();
        }

        if (dragState.IsUI && dragState.OriginalParent != null)
        {
            RestoreUIParent(dragState.Target.GameObject.transform);
        }

        if (currentDropTarget != null)
        {
            currentDropTarget.OnDragExit(dragState.Target);
            currentDropTarget = null;
        }

        dragState.Target.OnDragEnd();
        dragState.Reset();
    }

    // 드롭 타겟 변경 감지 및 Enter/Exit 이벤트 처리
    private void UpdateDropTarget()
    {
        Vector2 pointerPosition = GetPointerPosition();
        IDroppable newDropTarget = DetectDropTarget(pointerPosition);

        if (newDropTarget == currentDropTarget)
            return;

        currentDropTarget?.OnDragExit(dragState.Target);
        newDropTarget?.OnDragEnter(dragState.Target);
        currentDropTarget = newDropTarget;
    }

    // UI 오브젝트를 Canvas 최상위로 이동 (드래그 시 다른 UI 위에 표시되도록)
    private void MoveUIToCanvasTop(Transform targetTransform)
    {
        dragState.OriginalParent = targetTransform.parent;
        dragState.OriginalSiblingIndex = targetTransform.GetSiblingIndex();
        targetTransform.SetParent(rootCanvas.transform, true);
        targetTransform.SetAsLastSibling();
    }

    // UI 오브젝트를 원래 부모로 복원
    private void RestoreUIParent(Transform targetTransform)
    {
        targetTransform.SetParent(dragState.OriginalParent, true);
        targetTransform.SetSiblingIndex(dragState.OriginalSiblingIndex);
    }

    // 드래그 오브젝트를 원래 위치로 복원 (드롭 실패 시)
    private void ResetDraggablePosition()
    {
        dragState.Target.GameObject.transform.position = dragState.OriginalPosition;

        if (!dragState.IsUI)
        {
            Physics2D.SyncTransforms();
        }
    }

    // -----------------------------
    // 🔥 드래그 가능한 오브젝트 감지
    // -----------------------------
    private IDraggable DetectDraggable(Vector2 pointerPosition, out bool isUI)
    {
        // 1) UI 우선 검사
        IDraggable uiDraggable = DetectUIObject(pointerPosition);
        if (uiDraggable != null)
        {
            isUI = true;
            return uiDraggable;
        }

        // 2) 월드 검사
        isUI = false;
        return DetectWorldObject(pointerPosition);
    }

    private IDraggable DetectUIObject(Vector2 screenPosition)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            var draggable = result.gameObject.GetComponent<IDraggable>();
            if (draggable != null && draggable.IsDraggable)
            {
                return draggable;
            }
        }

        return null;
    }

    private IDraggable DetectWorldObject(Vector2 screenPosition)
    {
        Vector2 worldPoint = MainCamera.ScreenToWorldPoint(screenPosition);

        // 🔥 zero vector 제거 (Android에서 안정성 문제)
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPoint, Vector2.down, 0.01f, draggableLayer);

        if (hits.Length == 0)
            return null;

        return FindTopDraggable(hits);
    }

    // Raycast 결과에서 가장 위에 있는 드래그 가능한 오브젝트 찾기 (SortingOrder 기준)
    private IDraggable FindTopDraggable(RaycastHit2D[] hits)
    {
        IDraggable topDraggable = null;
        int highestSortingOrder = int.MinValue;

        foreach (var hit in hits)
        {
            var draggable = hit.collider.GetComponent<IDraggable>();
            if (draggable == null || !draggable.IsDraggable)
                continue;

            int sortingOrder = GetSortingOrder(hit.collider);

            if (topDraggable == null || sortingOrder > highestSortingOrder)
            {
                topDraggable = draggable;
                highestSortingOrder = sortingOrder;
            }
        }

        return topDraggable;
    }

    // -----------------------------
    // 🔥 드롭 타겟 감지
    // -----------------------------
    private IDroppable DetectDropTarget(Vector2 pointerPosition)
    {
        IDroppable uiDroppable = DetectUIDropTarget(pointerPosition);
        if (uiDroppable != null)
            return uiDroppable;

        return DetectWorldDropTarget(pointerPosition);
    }

    private IDroppable DetectUIDropTarget(Vector2 screenPosition)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            var droppable = result.gameObject.GetComponentInParent<IDroppable>();
            if (droppable != null)
                return droppable;
        }

        return null;
    }

    private IDroppable DetectWorldDropTarget(Vector2 screenPosition)
    {
        Vector2 worldPoint = MainCamera.ScreenToWorldPoint(screenPosition);

        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPoint, Vector2.down, 0.01f, draggableLayer);

        if (hits.Length == 0)
            return null;

        return FindTopDroppable(hits);
    }

    // Raycast 결과에서 가장 위에 있는 드롭 가능한 타겟 찾기
    private IDroppable FindTopDroppable(RaycastHit2D[] hits)
    {
        IDroppable topDroppable = null;
        int highestSortingOrder = int.MinValue;

        foreach (var hit in hits)
        {
            var droppable = hit.collider.GetComponent<IDroppable>();
            if (droppable == null)
                continue;

            int sortingOrder = GetSortingOrder(hit.collider);

            if (topDroppable == null || sortingOrder > highestSortingOrder)
            {
                topDroppable = droppable;
                highestSortingOrder = sortingOrder;
            }
        }

        return topDroppable;
    }

    // 드래그 중인 오브젝트를 마우스 위치로 이동
    private void MoveDraggingObject(GameObject targetObj)
    {
        Vector2 screenPos = GetPointerPosition();

        if (dragState.IsUI)
        {
            MoveUIObject(targetObj, screenPos);
        }
        else
        {
            MoveWorldObject(targetObj, screenPos);
        }
    }

    // UI 오브젝트를 스크린 좌표로 이동
    private void MoveUIObject(GameObject targetObj, Vector2 screenPos)
    {
        RectTransform rectTransform = targetObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.position = screenPos;
        }
    }

    // World 오브젝트를 월드 좌표로 이동
    private void MoveWorldObject(GameObject targetObj, Vector2 screenPos)
    {
        Vector3 worldPos = MainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;
        targetObj.transform.position = worldPos;
    }

    // Collider에서 SpriteRenderer의 SortingOrder 가져오기
    private int GetSortingOrder(Collider2D collider)
    {
        var spriteRenderer = collider.GetComponent<SpriteRenderer>();
        return spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
    }
}
