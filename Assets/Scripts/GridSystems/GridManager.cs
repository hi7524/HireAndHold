using UnityEngine;

public class GridManager : MonoBehaviour
{
    enum GridType
    {
        Empty = 0,
        Occupied = 1,
        Unavailable = -1,
    }

    [SerializeField] private GridLayoutData layoutData;

    private int[,] gridArray;


    private void Start()
    {
        // 최대 Grid Stage 사이즈에 맞추어 배열 생성
        gridArray = new int[layoutData.width, layoutData.height];

        SetUnavailableGrids();
    }

    private void SetUnavailableGrids()
    {
        for (int i = 0; i < layoutData.width; i++)
        {
            for (int j = 0; j < layoutData.height; j++)
            {
                if (!layoutData.IsValidCell(i, j))
                {
                    gridArray[i, j] = (int)GridType.Unavailable;
                }
            }
        }
    }
}