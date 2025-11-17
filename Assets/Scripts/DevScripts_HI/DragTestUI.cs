using UnityEngine;

public class DragTestUI : MonoBehaviour, ITestDraggable
{
    public bool IsDraggable => true;
    public bool RequireDropZone => true;
    public GameObject GameObject => gameObject;



    public void OnDragStart()
    {
        Debug.Log($"드래그 시작: {gameObject.name}");
    }

    public void OnDrag()
    {
        Debug.Log($"드래그: {gameObject.name}");
    }

    public void OnDragEnd()
    {
        Debug.Log($"드래그 종료: {gameObject.name}");
    }

    public void OnDropFailed()
    {
    }
}
