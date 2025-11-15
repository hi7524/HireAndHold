using UnityEngine;
using UnityEngine.InputSystem;

public class DragManager : MonoBehaviour
{
    private Camera mainCamera;
    private ITestDraggable dragTarget;


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
        Vector2 pointerPosition = Pointer.current.position.ReadValue();
        Vector2 worldPoint = mainCamera.ScreenToWorldPoint(pointerPosition);

        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (hit.collider == null)
            return null;

        var draggable = hit.collider.GetComponent<ITestDraggable>();
        if (draggable != null)
            return draggable;

        return null;
    }

    private void MoveDraggingObject(GameObject targetObj)
    {
        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;

        targetObj.transform.position = worldPos;

        var collider = targetObj.GetComponent<CircleCollider2D>();
        Physics2D.SyncTransforms(); // 물리 시스템 강제 동기화
    }
}