using System;
using System.Collections.Generic;
using UnityEngine;

public enum GridState
{
    Empty = 0,
    Occupied = 1,
    Unavailable = -1,
}

public class GridManager : MonoBehaviour
{
    [SerializeField] private GridLayoutData layoutData;
    [SerializeField] private GridVisualizer gridVisualizer;
    [Space]
    [SerializeField] private Color validColor;
    [SerializeField] private Color invalidColor;
    [Space]
    [SerializeField] private GameObject gridUnitPrefab;
    [Space]
    [SerializeField] private LevelUpRewardController levelUpRewardController;
    [SerializeField] private StageUiManager uiManager;

    public int[,] gridArray { get; private set; }
    public bool IsFilledAllGrids { get; private set; }

    private GridCell[,] gridCells;
    private HashSet<GridCell> highlightedCells = new HashSet<GridCell>();
    private Dictionary<Vector2Int, Color> coloredCell = new Dictionary<Vector2Int, Color>();
    private Dictionary<Vector2Int, Color> tempColoredCell;


    private void Start()
    {
        InitializeGrid();
    }

    // 그리드 배열 및 셀 초기화
    private void InitializeGrid()
    {
        gridArray = new int[layoutData.width, layoutData.height];
        gridCells = new GridCell[layoutData.width, layoutData.height];
        RegisterCells();
    }

    // 그리드 셀 상태 설정 (Empty, Occupied, Unavailable)
    public void SetGridState(Vector2Int pos, GridState state)
    {
        gridArray[pos.x, pos.y] = (int)state;
        gridCells[pos.x, pos.y].SetAcceptable(state == GridState.Empty);

        CheckFilledAllCells();
    }

    // GridVisualizer의 자식 오브젝트들을 순회하며 GridCell 컴포넌트 수집 및 등록
    private void RegisterCells()
    {
        if (gridVisualizer == null)
        {
            Debug.LogWarning("GridVisualizer가 할당되지 않았습니다.");
            return;
        }

        foreach (Transform child in gridVisualizer.transform)
        {
            var cell = child.GetComponent<GridCell>();
            if (cell != null)
            {
                RegisterCell(cell);
            }
        }
    }

    // 개별 셀을 그리드 배열에 등록
    private void RegisterCell(GridCell cell)
    {
        Vector2Int pos = cell.GridPosition;

        if (IsWithinBounds(pos))
        {
            gridCells[pos.x, pos.y] = cell;
        }

        cell.SetGridManager(this);
    }

    // 유닛 배치 가능 여부 확인 및 시각적 피드백 제공
    public bool CanPlaceUnit(Vector2Int curPos, List<Vector2Int> grid)
    {
        ClearAllGridsColor();
        ChangeOccupiedCellColor();

        HashSet<Vector2Int> allPositions = GetAbsolutePositions(curPos, grid);
        bool canPlace = ValidateAllPositions(allPositions);

        HighlightCells(allPositions, canPlace);

        return canPlace;
    }

    // 현재 위치와 상대 위치를 합쳐 절대 위치 목록 생성
    private HashSet<Vector2Int> GetAbsolutePositions(Vector2Int curPos, List<Vector2Int> grid)
    {
        HashSet<Vector2Int> allPositions = new HashSet<Vector2Int> { curPos };

        foreach (var relativePos in grid)
        {
            allPositions.Add(curPos + relativePos);
        }

        return allPositions;
    }

    // 모든 위치가 배치 가능한지 검증
    private bool ValidateAllPositions(HashSet<Vector2Int> positions)
    {
        bool canPlace = true;

        foreach (var pos in positions)
        {
            if (!IsValidPosition(pos))
            {
                canPlace = false;
            }
        }

        return canPlace;
    }

    // 위치가 유효한지 확인 (범위 내, 유효한 셀, 비어있음)
    private bool IsValidPosition(Vector2Int pos)
    {
        if (!IsWithinBounds(pos))
            return false;

        if (!layoutData.IsValidCell(pos))
            return false;

        int cellState = gridArray[pos.x, pos.y];
        return cellState == (int)GridState.Empty;
    }

    // 위치가 그리드 범위 내에 있는지 확인
    private bool IsWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < layoutData.width &&
               pos.y >= 0 && pos.y < layoutData.height;
    }

    // 배치 가능 여부에 따라 셀 하이라이트 색상 적용
    private void HighlightCells(HashSet<Vector2Int> positions, bool canPlace)
    {
        Color highlightColor = canPlace ? validColor : invalidColor;

        foreach (var pos in positions)
        {
            if (IsWithinBounds(pos))
            {
                GridCell cell = gridCells[pos.x, pos.y];
                if (cell != null)
                {
                    cell.SetColor(highlightColor);
                    highlightedCells.Add(cell);
                }
            }
        }
    }

    // 칸이 모두 채워져있는지 체크
    private void CheckFilledAllCells()
    {
        IsFilledAllGrids = false;

        for (int x = 0; x < layoutData.width; x++)
        {
            for (int y = 0; y < layoutData.height; y++)
            {
                // 유효하지 않은 셀은 건너뛰기
                if (!layoutData.IsValidCell(x, y))
                    continue;

                // 유효한 셀이 비어있으면 false
                if (gridArray[x, y] == (int)GridState.Empty)
                    return;
            }
        }

        IsFilledAllGrids = true;
        Debug.Log("그리드 채우기 버프");
    }

    // 유닛 합성시 호출
    public void OnMergedUnits()
    {
        uiManager.UpdateInfoText("유닛 합성 완료!");
    }

    // 모든 하이라이트된 셀의 색상 초기화
    public void ClearAllGridsColor()
    {
        foreach (var cell in highlightedCells)
        {
            if (cell != null && !coloredCell.ContainsKey(cell.GridPosition))
            {
                cell.SetColor(Color.white);
            }
        }
        highlightedCells.Clear();
    }

    // 유닛이 차지하는 모든 셀에 지정된 색상 적용
    public void SetUnitCellsColor(Vector2Int curPos, List<Vector2Int> grid, Color color)
    {
        ClearAllGridsColor();

        HashSet<Vector2Int> allPositions = GetAbsolutePositions(curPos, grid);

        foreach (var pos in allPositions)
        {
            if (IsWithinBounds(pos))
            {
                GridCell cell = gridCells[pos.x, pos.y];
                if (cell != null)
                {
                    cell.SetColor(color);
                }
            }
        }
    }

    // 점유된 셀과 색상 정보 등록
    public void SetOccupiedCellAndColor(Vector2Int pos, Color color)
    {
        coloredCell[pos] = color;
    }

    // 유닛이 차지하던 셀의 색상 정보 제거 및 색상 초기화
    public void RemoveColoredCells(Vector2Int curPos, List<Vector2Int> grid)
    {
        RemoveCellColor(curPos);

        foreach (var relativePos in grid)
        {
            Vector2Int absolutePos = curPos + relativePos;
            RemoveCellColor(absolutePos);
        }
    }

    // 특정 셀의 색상 정보 제거 및 흰색으로 리셋
    private void RemoveCellColor(Vector2Int pos)
    {
        if (coloredCell.Remove(pos))
        {
            gridCells[pos.x, pos.y].SetColor(Color.white);
        }
    }

    // 점유된 모든 셀에 등록된 색상 다시 적용
    public void ChangeOccupiedCellColor()
    {
        foreach (var cell in coloredCell)
        {
            var pos = cell.Key;
            gridCells[pos.x, pos.y].SetColor(cell.Value);
        }
    }

    // 현재 색상 상태를 임시 저장 (실패 시 복원용)
    public void CopyColoredCellToTemp()
    {
        tempColoredCell = new Dictionary<Vector2Int, Color>(coloredCell);
    }

    // 배치 실패 시 임시 저장된 색상 상태로 복원
    public void OnFailed()
    {
        if (tempColoredCell == null)
            return;

        RestoreColoredCells();
        RestoreCellColors();
    }

    // coloredCell 딕셔너리를 임시 저장 값으로 복원
    private void RestoreColoredCells()
    {
        foreach (var cell in tempColoredCell)
        {
            coloredCell[cell.Key] = cell.Value;
        }
    }

    // 그리드 셀의 시각적 색상을 임시 저장 값으로 복원
    private void RestoreCellColors()
    {
        foreach (var cell in tempColoredCell)
        {
            var pos = cell.Key;
            gridCells[pos.x, pos.y].SetColor(cell.Value);
        }
    }

    // 그리드 유닛 생성 및 초기화
    public GridUnit SpawnGridUnit(Vector3 position, UnitGridData gridData)
    {
        if (gridUnitPrefab == null)
            return null;

        GameObject unitObj = Instantiate(gridUnitPrefab, position, Quaternion.identity);
        GridUnit gridUnit = unitObj.GetComponent<GridUnit>();

        if (gridUnit == null)
        {
            Destroy(unitObj);
            return null;
        }

        gridUnit.SetGridData(gridData);
        return gridUnit;
    }

    // 레벨업 보상 유닛이 생성되었음을 알림
    public void NotifyLevelUpRewardUnitSpawned(GridUnit unit)
    {
        if (levelUpRewardController != null)
            levelUpRewardController.OnLevelUpRewardUnitSpawned(unit);
    }
}