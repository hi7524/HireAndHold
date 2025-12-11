using UnityEngine;

/// <summary>
/// GridUnit의 보조 터치 영역. 유닛이 차지하는 각 셀 위치에 배치되어
/// 어느 부분을 터치해도 부모 GridUnit이 드래그되도록 합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GridUnitDragProxy : MonoBehaviour, IDraggable
{
    private GridUnit parentGridUnit;
    private Vector2Int cellOffset; // 이 프록시가 부모 GridUnit의 중심에서 얼마나 떨어져 있는지

    public bool IsDraggable => parentGridUnit != null && parentGridUnit.IsDraggable;
    public GameObject GameObject => parentGridUnit != null ? parentGridUnit.GameObject : gameObject;

    public Vector2Int CellOffset => cellOffset;

    public void Initialize(GridUnit parent, Vector2Int offset)
    {
        parentGridUnit = parent;
        cellOffset = offset;
    }

    public void OnDragStart()
    {
        if (parentGridUnit != null)
        {
            parentGridUnit.OnDragStartFromProxy(cellOffset);
        }
    }

    public void OnDrag()
    {
        if (parentGridUnit != null)
        {
            parentGridUnit.OnDrag();
        }
    }

    public void OnDragEnd()
    {
        if (parentGridUnit != null)
        {
            parentGridUnit.OnDragEnd();
        }
    }

    public void OnDropSuccess()
    {
        if (parentGridUnit != null)
        {
            parentGridUnit.OnDropSuccess();
        }
    }

    public void OnDropFailed()
    {
        if (parentGridUnit != null)
        {
            parentGridUnit.OnDropFailed();
        }
    }
}
