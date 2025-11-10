using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    [SerializeField] private GridLayoutData layoutData;
    [SerializeField] private GridCell gridCellPrf;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float space = 0.1f;


    private void Start()
    {
        VisualizeGridData(layoutData);
    }

    private void VisualizeGridData(GridLayoutData data)
    {
        for (int i = 0; i < data.width; i++)
        {
            for (int j = 0; j < data.height; j++)
            {
                if (!data.IsValidCell(i, j))
                {
                    continue;
                }

                var cell = Instantiate(gridCellPrf, transform);

                cell.transform.localPosition = new Vector3(
                    i * (cellSize + space),
                    j * (cellSize + space),
                    0f
                );

                cell.transform.localScale = new Vector3(cellSize, cellSize, 1f);
            }
        }
    }
}