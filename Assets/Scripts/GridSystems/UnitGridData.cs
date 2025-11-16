using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "UnitGridData", menuName = "Scriptable Objects/UnitGridData")]
public class UnitGridData : ScriptableObject
{
    public const int GridSize = 5;

    [System.Serializable]
    public class ShapeData
    {
        // 중심점(0,0) 기준 상대 좌표들
        public List<Vector2Int> occupiedCells = new List<Vector2Int>();
    }

    public ShapeData shape = new ShapeData();

    // 에디터용 - 그리드를 boolean 배열로 변환 (시각화용)
    public bool[,] GetGridRepresentation()
    {
        bool[,] grid = new bool[GridSize, GridSize];

        int centerX = GridSize / 2;
        int centerY = GridSize / 2;

        foreach (var cell in shape.occupiedCells)
        {
            int x = centerX + cell.x;
            int y = centerY + cell.y;

            if (x >= 0 && x < GridSize && y >= 0 && y < GridSize)
            {
                grid[x, y] = true;
            }
        }

        return grid;
    }

    // 특정 셀이 차지되어 있는지 확인
    public bool IsCellOccupied(Vector2Int relativePos)
    {
        return shape.occupiedCells.Contains(relativePos);
    }

    // 셀 토글 (에디터용)
    public void ToggleCell(int gridX, int gridY)
    {
        int centerX = GridSize / 2;
        int centerY = GridSize / 2;

        Vector2Int relativePos = new Vector2Int(gridX - centerX, gridY - centerY);

        if (shape.occupiedCells.Contains(relativePos))
        {
            shape.occupiedCells.Remove(relativePos);
        }
        else
        {
            shape.occupiedCells.Add(relativePos);
        }
    }

    public List<Vector2Int> GetOccupiedCells()
    {
        return new List<Vector2Int>(shape.occupiedCells);
    }
}