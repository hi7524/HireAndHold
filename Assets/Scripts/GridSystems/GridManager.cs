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

    public int[,] gridArray { get; private set; }

    private GridCell[,] gridCells;
    private HashSet<GridCell> highlightedCells = new HashSet<GridCell>();
    private Dictionary<Vector2Int, Color> coloredCell = new Dictionary<Vector2Int, Color>();
    private Dictionary<Vector2Int, Color> tempColoredCell;


    private void Start()
    {
        // 최대 Grid Stage 사이즈에 맞추어 배열 생성
        gridArray = new int[layoutData.width, layoutData.height];
        gridCells = new GridCell[layoutData.width, layoutData.height];

        RegisterCells();
    }

    public void SetGridState(Vector2Int pos, GridState state)
    {
        gridArray[pos.x, pos.y] = (int)state;
        gridCells[pos.x, pos.y].SetAcceptable(state == GridState.Empty);
    }

    // GridVisualizer의 자식 오브젝트들을 순회하며 GridCell 컴포넌트 수집
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
                Vector2Int pos = cell.GridPosition;

                if (pos.x >= 0 && pos.x < layoutData.width &&
                    pos.y >= 0 && pos.y < layoutData.height)
                {
                    gridCells[pos.x, pos.y] = cell;
                }

                cell.SetGridManager(this);
            }
        }
    }

    public bool CanPlaceUnit(Vector2Int curPos, List<Vector2Int> grid)
    {
        ClearAllGridsColor();
        ChangeOccupiedCellColor();

        bool canPlace = true;

        HashSet<Vector2Int> allPositions = new HashSet<Vector2Int>
        {
            curPos
        };

        foreach (var relativePos in grid)
        {
            allPositions.Add(curPos + relativePos);
        }

        foreach (var absolutePos in allPositions)
        {
            // 인덱스 범위 체크
            if (absolutePos.x < 0 || absolutePos.x >= layoutData.width ||
                absolutePos.y < 0 || absolutePos.y >= layoutData.height)
            {
                canPlace = false;
                continue;
            }

            // 유효한 셀인지 체크 (뚫린 부분 체크)
            if (!layoutData.IsValidCell(absolutePos))
            {
                canPlace = false;
                continue;
            }

            // 셀 상태 체크 (이미 차지되었는지)
            int cellState = gridArray[absolutePos.x, absolutePos.y];
            if (cellState != (int)GridState.Empty)
            {
                canPlace = false;
            }
        }

        // 색상 변경
        foreach (var absolutePos in allPositions)
        {
            if (absolutePos.x >= 0 && absolutePos.x < layoutData.width &&
                absolutePos.y >= 0 && absolutePos.y < layoutData.height)
            {
                GridCell cell = gridCells[absolutePos.x, absolutePos.y];
                if (cell != null)
                {
                    cell.SetColor(canPlace ? validColor : invalidColor);
                    highlightedCells.Add(cell);
                }
            }
        }

        return canPlace;
    }

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

    public void SetUnitCellsColor(Vector2Int curPos, List<Vector2Int> grid, Color color)
    {
        ClearAllGridsColor();

        HashSet<Vector2Int> allPositions = new HashSet<Vector2Int>
        {
            curPos
        };

        foreach (var relativePos in grid)
        {
            allPositions.Add(curPos + relativePos);
        }

        // 유닛이 차지하는 모든 셀에 색상 적용
        foreach (var absolutePos in allPositions)
        {
            if (absolutePos.x >= 0 && absolutePos.x < layoutData.width &&
                absolutePos.y >= 0 && absolutePos.y < layoutData.height)
            {
                GridCell cell = gridCells[absolutePos.x, absolutePos.y];
                if (cell != null)
                {
                    cell.SetColor(color);
                }
            }
        }
    }

    public void SetOccupiedCellAndColor(Vector2Int pos, Color color)
    {
        coloredCell[pos] = color;
    }

    public void RemoveColoredCells(Vector2Int curPos, List<Vector2Int> grid)
    {
        // coloredCell에서 제거하면서 해당 셀을 흰색으로 변경
        if (coloredCell.Remove(curPos))
        {
            gridCells[curPos.x, curPos.y].SetColor(Color.white);
        }

        foreach (var relativePos in grid)
        {
            Vector2Int absolutePos = curPos + relativePos;
            if (coloredCell.Remove(absolutePos))
            {
                gridCells[absolutePos.x, absolutePos.y].SetColor(Color.white);
            }
        }
    }

    public void ChangeOccupiedCellColor()
    {
        foreach (var cell in coloredCell)
        {
            var pos = cell.Key;
            gridCells[pos.x, pos.y].SetColor(cell.Value);
        }
    }

    public void CopyColoredCellToTemp()
    {
        tempColoredCell = new Dictionary<Vector2Int, Color>(coloredCell);
    }

    public void OnFailed()
    {
        if (tempColoredCell == null)
            return;

        // tempColoredCell의 내용을 coloredCell로 복원
        foreach (var cell in tempColoredCell)
        {
            coloredCell[cell.Key] = cell.Value;
        }

        // 색상 복원
        foreach (var cell in tempColoredCell)
        {
            var pos = cell.Key;
            gridCells[pos.x, pos.y].SetColor(cell.Value);
        }
    }

    public GridUnit SpawnGridUnit(Vector3 position, UnitGridData gridData)
    {
        if (gridUnitPrefab == null)
            return null;

        var unitObj = Instantiate(gridUnitPrefab, position, Quaternion.identity);
        var gridUnit = unitObj.GetComponent<GridUnit>();

        if (gridUnit == null)
        {
            Destroy(unitObj);
            return null;
        }

        gridUnit.SetGridData(gridData);
        return gridUnit;
    }
}