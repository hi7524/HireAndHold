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

    public int[,] gridArray { get; private set; }

    private GridCell[,] gridCells;


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
        foreach (var relativePos in grid)
        {
            Vector2Int absolutePos = curPos + relativePos;

            // 인덱스 범위 체크
            if (absolutePos.x < 0 || absolutePos.x >= layoutData.width ||
                absolutePos.y < 0 || absolutePos.y >= layoutData.height)
            {
                return false;
            }

            // 유효한 셀인지 체크 (뚫린 부분 체크)
            if (!layoutData.IsValidCell(absolutePos))
            {
                return false;
            }

            // 셀 상태 체크 (이미 차지되었는지)
            int cellState = gridArray[absolutePos.x, absolutePos.y];
            if (cellState != (int)GridState.Empty)
            {
                return false;
            }
        }

        return true;
    }
}