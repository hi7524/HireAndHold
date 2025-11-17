using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GridUnit : MonoBehaviour, ITestDraggable
{
    [SerializeField] private UnitGridData gridData;
    [SerializeField] private float cellSize = 0.6f;
    [SerializeField] private GameObject cellPrf;

    // 그리드 데이터
    public UnitGridData GridData => gridData;

    // 드래그
    public bool IsDraggable => true;
    public bool RequireDropZone => true;
    public GameObject GameObject => gameObject;

    private GridCell curGridCell;
    private GridCell previousGridCell;

    private List<Transform> childrenObj = new List<Transform>();


    private void Start()
    {
        CreatePreviewSprites();
    }

    public void OnDragStart()
    {
        // GridCell 설정 관련
        previousGridCell = curGridCell;
        curGridCell?.ClearObject();
        curGridCell = null;

        // Grid 정보 시각화(미리보기) 관련
        SetActiveChildrenObj(true);
    }

    public void OnDrag()
    {
        
    }

    public void OnDragEnd()
    {
        SetActiveChildrenObj(false);
    }

    public void OnDropFailed()
    {
        curGridCell = previousGridCell;

        // 드롭 실패 시 원래 그리드 색상 복원
        if (curGridCell != null)
        {
            var gridManager = curGridCell.GetGridManager();
            if (gridManager != null)
            {
                gridManager.OnFailed();
            }
        }
    }

    public void SetCurrentGridCell(GridCell cell)
    {
        curGridCell = cell;
    }

    public GridCell GetPreviousCell()
    {
        return previousGridCell;
    }

    // 미리보기 스프라이트 생성
    private void CreatePreviewSprites()
    {
        if (gridData == null || cellPrf == null)
            return;

        for (int i = 0; i < gridData.GetOccupiedCells().Count; i++)
        {
            // 생성
            var cell = Instantiate(cellPrf, transform);
            childrenObj.Add(cell.transform);
            cell.transform.localScale = cellSize * Vector3.one;

            // 위치 설정
            Vector2Int cellPos = gridData.GetOccupiedCells()[i];
            cell.transform.localPosition = new Vector3(cellPos.x * cellSize, cellPos.y * cellSize, 0);
        }

        var center = Instantiate(cellPrf, transform);
        childrenObj.Add(center.transform);
        center.transform.localScale = cellSize * Vector3.one;

        center.transform.localPosition = transform.position;
    }

    // 자식 오브젝트 전체 비활성화 및 활성화
    private void SetActiveChildrenObj(bool value)
    {
        if (childrenObj == null || childrenObj.Count == 0)
            return;

        for (int i = 0; i < childrenObj.Count; i++)
        {
            childrenObj[i].gameObject.SetActive(value);
        }
    }
}