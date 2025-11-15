using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DragTestObject : MonoBehaviour, ITestDraggable
{
    public GameObject GameObject => gameObject;

    public bool IsDraggable => true;


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
}