using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    [SerializeField] private GridLayoutData layoutData;
    [SerializeField] private GridCell gridCellPrf;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float space = 0.1f;
    [SerializeField] private ObjectPoolManager poolManager;

    public GridLayoutData GetLayoutData() => layoutData;

    private void Awake()
    {
        VisualizeGridData(layoutData);
    }

    public void VisualizeGridData(GridLayoutData data)
    {
        if (data == null || gridCellPrf == null)
            return;

        for (int i = 0; i < data.width; i++)
        {
            for (int j = 0; j < data.height; j++)
            {
                if (!data.IsValidCell(i, j))
                {
                    continue;
                }

                var cell = Instantiate(gridCellPrf, transform);
                cell.SetGridPosition(new Vector2Int(i, j));

                cell.transform.localPosition = new Vector3(
                    i * (cellSize + space),
                    j * (cellSize + space),
                    0f
                );

                cell.transform.localScale = new Vector3(cellSize, cellSize, 1f);

                // // ** 테스트용
                // cell.GetComponent<SpriteDropZone>().poolManager = poolManager;
            }
        }
    }

    public void ClearGrid()
    {
        while (transform.childCount > 0)
        {
            if (Application.isPlaying)
            {
                Destroy(transform.GetChild(0).gameObject);
            }
            else
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }
    }
}