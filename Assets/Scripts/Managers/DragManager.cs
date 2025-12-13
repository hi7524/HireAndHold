using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

public class DragManager : MonoBehaviour
{
    [SerializeField] private LayerMask draggableLayer;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private bool isDragEnabled = false;

    private Camera mainCamera;
    private DragState dragState;
    private IDroppable currentDropTarget;

    // 드래그 상태 변경 이벤트
    public event Action OnDragStarted;
    public event Action OnDragEnded;

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
        public Vector3 DragOffset; // 드래그 시작 시 마우스와 오브젝트 간의 오프셋

        public readonly bool IsDragging => Target != null;

        public void Reset()
        {
            Target = null;
            OriginalPosition = Vector3.zero;
            IsUI = false;
            OriginalParent = null;
            OriginalSiblingIndex = 0;
            DragOffset = Vector3.zero;
        }
    }

    //  Pointer.current 제거 버전

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

    // 드래그 Update (InputSystem-safe)
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

    // 현재 드래그 중인지 확인
    public bool IsDragging => dragState.IsDragging;

    // 드래그 활성화/비활성화 설정
    public void SetDragEnabled(bool value)
    {
        isDragEnabled = value;
    }

    // 드래그 상태를 강제로 종료 (외부에서 호출)
    public void CancelDrag()
    {
        if (!dragState.IsDragging)
            return;

        // 드래그 중인 객체를 원래 위치로 복원
        dragState.Target.GameObject.transform.position = dragState.OriginalPosition;

        if (!dragState.IsUI)
        {
            Physics2D.SyncTransforms();
        }

        if (dragState.IsUI && dragState.OriginalParent != null)
        {
            dragState.Target.GameObject.transform.SetParent(dragState.OriginalParent, true);
            dragState.Target.GameObject.transform.SetSiblingIndex(dragState.OriginalSiblingIndex);
        }

        currentDropTarget?.OnDragExit(dragState.Target);
        currentDropTarget = null;

        dragState.Target.OnDropFailed();
        dragState.Target.OnDragEnd();

        dragState.Reset();

        // 드래그 종료 이벤트 발생
        OnDragEnded?.Invoke();
    }


    // 드래그 시작 처리: 드래그 대상 감지 및 초기 상태 설정
    private void HandleDragStart()
    {
        // 이미 드래그 중이면 새로운 드래그를 시작하지 않음
        if (dragState.IsDragging)
            return;

        Vector2 pointerPosition = GetPointerPosition();
        IDraggable newTarget = DetectDraggable(pointerPosition, out bool isUI);

        if (newTarget == null || !newTarget.IsDraggable)
            return;

        // 새로운 드래그 상태 설정
        dragState.Target = newTarget;
        dragState.IsUI = isUI;
        dragState.OriginalPosition = dragState.Target.GameObject.transform.position;

        // 드래그 오프셋 계산 (마우스 위치 - 오브젝트 위치)
        if (isUI)
        {
            dragState.DragOffset = dragState.Target.GameObject.transform.position - (Vector3)pointerPosition;
        }
        else
        {
            Vector3 worldPos = MainCamera.ScreenToWorldPoint(pointerPosition);
            worldPos.z = 0;
            dragState.DragOffset = dragState.Target.GameObject.transform.position - worldPos;
        }

        if (dragState.IsUI && rootCanvas != null)
        {
            MoveUIToCanvasTop(dragState.Target.GameObject.transform);
        }

        dragState.Target.OnDragStart();

        // 드래그 시작 이벤트 발생
        OnDragStarted?.Invoke();
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
        // 드래그 중이 아니면 처리하지 않음
        if (!dragState.IsDragging)
            return;

        // 드래그 종료 처리를 위한 임시 변수 저장
        IDraggable draggingTarget = dragState.Target;
        bool wasUI = dragState.IsUI;
        Transform originalParent = dragState.OriginalParent;
        int originalSiblingIndex = dragState.OriginalSiblingIndex;
        Vector3 originalPosition = dragState.OriginalPosition;

        // 먼저 상태를 리셋하여 빠른 재입력 시 충돌 방지
        dragState.Reset();

        Vector2 pointerPosition = GetPointerPosition();
        IDroppable dropTarget = DetectDropTarget(pointerPosition);

        bool dropSuccess = dropTarget != null && dropTarget.CanDrop(draggingTarget);

        if (dropSuccess)
        {
            dropTarget.OnDrop(draggingTarget);
            draggingTarget.OnDropSuccess();
        }
        else
        {
            // OnDropFailed()가 호출되기 전에 위치 복원 (IDraggable이 자체 복원 로직이 없는 경우를 위해)
            // GridUnit 같은 경우는 OnDropFailed()에서 자체적으로 올바른 위치로 복원함
            draggingTarget.GameObject.transform.position = originalPosition;

            if (!wasUI)
            {
                Physics2D.SyncTransforms();
            }

            draggingTarget.OnDropFailed();
        }

        if (wasUI && originalParent != null)
        {
            draggingTarget.GameObject.transform.SetParent(originalParent, true);
            draggingTarget.GameObject.transform.SetSiblingIndex(originalSiblingIndex);
        }

        currentDropTarget?.OnDragExit(draggingTarget);
        currentDropTarget = null;

        draggingTarget.OnDragEnd();

        // 드래그 종료 이벤트 발생
        OnDragEnded?.Invoke();
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
            // 오프셋을 적용하여 클릭한 위치가 유지되도록 함
            rectTransform.position = (Vector3)screenPos + dragState.DragOffset;
        }
    }

    // World 오브젝트를 월드 좌표로 이동
    private void MoveWorldObject(GameObject targetObj, Vector2 screenPos)
    {
        Vector3 worldPos = MainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;
        // 오프셋을 적용하여 클릭한 위치가 유지되도록 함
        targetObj.transform.position = worldPos + dragState.DragOffset;
    }

    // Collider에서 SpriteRenderer의 SortingOrder 가져오기
    private int GetSortingOrder(Collider2D collider)
    {
        var spriteRenderer = collider.GetComponent<SpriteRenderer>();
        return spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
    }
}
