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

    // GridVisualizer의 자식 오브젝트들을 순회하며 GridCell 컴포넌트를 수집
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
}