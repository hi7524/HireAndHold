using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GridUnit : MonoBehaviour, ITestDraggable
{
    [SerializeField] private float cellSize = 0.6f;
    [SerializeField] private GameObject cellPrf;
    [SerializeField] private Transform previewTrans;

    public int UnitId { get; private set; }
    public UnitGridData GridData { get; private set; }

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
        SetActiveChildrenObj(false);
    }

    public void SetUnitID(int unitId)
    {
        UnitId = unitId;
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

    public void SetGridData(UnitGridData newGridData)
    {
        GridData = newGridData;

        // 기존 프리뷰 제거
        ClearPreviewSprites();

        // 새 프리뷰 생성
        CreatePreviewSprites();
        SetActiveChildrenObj(false);
    }

    // 미리보기 스프라이트 제거
    private void ClearPreviewSprites()
    {
        foreach (var child in childrenObj)
        {
            if (child != null)
                Destroy(child.gameObject);
        }
        childrenObj.Clear();
    }

    // 미리보기 스프라이트 생성
    private void CreatePreviewSprites()
    {
        if (GridData == null || cellPrf == null)
            return;

        var occupiedCells = GridData.GetOccupiedCells();

        // 중앙 셀
        CreatePreviewCell(Vector2Int.zero);

        // 나머지 셀
        foreach (var cellPos in occupiedCells)
        {
            CreatePreviewCell(cellPos);
        }
    }

    private void CreatePreviewCell(Vector2Int cellPos)
    {
        var cell = Instantiate(cellPrf, previewTrans);
        childrenObj.Add(cell.transform);
        cell.transform.localScale = cellSize * Vector3.one;
        cell.transform.localPosition = new Vector3(cellPos.x * cellSize, cellPos.y * cellSize, 0);

        SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = GridData.gridColor;
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