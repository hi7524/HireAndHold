using UnityEngine;
using System.Collections.Generic;

// 그리드 셀 생성, 배치 하이라이트, 버프 영역 시각화
public class GridVisualizer : MonoBehaviour
{
    [SerializeField] private GridCell gridCellPrf;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float space = 0.1f;
    [Space]
    [Header("Color Settings")]
    [SerializeField] private Color validColor = Color.green;
    [SerializeField] private Color invalidColor = Color.red;
    [Space]
    [SerializeField] private Color crossBuffColor = new Color(0.9f, 0.95f, 1f, 1f);
    [SerializeField] private Color regionBuffColor = new Color(0.95f, 1f, 0.9f, 1f);

    private GridLayoutData layoutData;
    private GridCell[,] gridCells;
    private HashSet<GridCell> highlightedCells = new HashSet<GridCell>(); // 현재 하이라이트 중인 셀 추적 (배치 미리보기용)
    private Dictionary<Vector2Int, Color> coloredCell = new Dictionary<Vector2Int, Color>(); // 유닛이 점유 중인 셀과 색상 매핑 (배치된 유닛 표시용)
    private Dictionary<Vector2Int, Color> tempColoredCell; // 배치 실패 시 복원용 색상 백업
    private Dictionary<Vector2Int, Sprite> tempIconCells; // 배치 실패 시 복원용 아이콘 백업 (위치 -> 스프라이트)
    private Dictionary<Vector2Int, Color> buffColoredCells = new Dictionary<Vector2Int, Color>(); // 버프 영역 셀과 색상 매핑 (크로스/리전 버프 표시용)


    // GridLayoutData를 바탕으로 그리드 셀 생성 및 위치 설정
    public void VisualizeGridData(GridLayoutData data)
    {
        if (data == null || gridCellPrf == null)
            return;

        layoutData = data;

        gridCells = new GridCell[data.width, data.height];

        // 전체 그리드 크기 계산 (중앙 정렬을 위한 오프셋 계산용)
        float totalWidth = data.width * (cellSize + space) - space;
        float totalHeight = data.height * (cellSize + space) - space;

        // 중심을 (0, 0)으로 맞추기 위한 오프셋
        Vector3 centerOffset = new Vector3(-totalWidth / 2f, -totalHeight / 2f, 0f);

        // 각 셀의 pivot을 중심으로 하기 위한 offset
        Vector3 cellPivotOffset = new Vector3(cellSize / 2f, cellSize / 2f, 0f);

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
                ) + cellPivotOffset + centerOffset;

                cell.transform.localScale = new Vector3(cellSize, cellSize, 1f);

                gridCells[i, j] = cell;
            }
        }

        // GridCell 생성 후 Physics2D 동기화 (씬 전환 시 Collider2D 감지 문제 해결)
        Physics2D.SyncTransforms();
    }
    
    // 해당 좌표가 범위 안에 있는지 검사 및 해당 좌표의 GridCell 가져오기
    public GridCell GetGridCellAt(Vector2Int pos)
    {
        if (!IsWithinBounds(pos))
            return null;

        return gridCells[pos.x, pos.y];
    }

    // 위치가 그리드 범위 내에 있는지 확인
    private bool IsWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < layoutData.width &&
               pos.y >= 0 && pos.y < layoutData.height;
    }

    // 좌표에 해당하는 Cell들의 배치 가능 여부에 따라 셀들의 색상 변경
    public void HighlightCells(HashSet<Vector2Int> positions, bool canPlace)
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

    // 모든 하이라이트된 셀의 색상 초기화
    public void ClearAllGridsColor()
    {
        foreach (var cell in highlightedCells)
        {
            if (cell != null && !coloredCell.ContainsKey(cell.GridPosition))
            {
                // 버프가 활성화된 셀이면 버프 색상으로, 아니면 흰색으로
                if (buffColoredCells.ContainsKey(cell.GridPosition))
                {
                    cell.SetColor(buffColoredCells[cell.GridPosition]);
                }
                else
                {
                    cell.SetColor(Color.white);
                }
            }
        }
        highlightedCells.Clear();
    }

    // 이전 하이라이트를 지우고 유닛이 좌표에 해당하는 모든 셀에 색상 적용
    public void SetUnitCellsColor(HashSet<Vector2Int> allPositions, Color color)
    {
        ClearAllGridsColor();

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

    // 점유된 셀의 좌표와 색상 정보 등록
    public void SetOccupiedCellAndColor(Vector2Int pos, Color color)
    {
        coloredCell[pos] = color;
    }

    // 점유된 셀에 아이콘 설정
    public void SetCellIcon(Vector2Int pos, Sprite icon)
    {
        if (!IsWithinBounds(pos))
            return;

        GridCell cell = gridCells[pos.x, pos.y];
        if (cell != null)
        {
            cell.SetIcon(icon);
        }
    }

    // 특정 좌표 셀의 색상 정보 제거 및 기본 색상으로 리셋
    public void RemoveCellColor(Vector2Int pos)
    {
        if (coloredCell.Remove(pos))
        {
            // 버프가 활성화된 셀이면 버프 색상으로, 아니면 흰색으로
            if (buffColoredCells.ContainsKey(pos))
            {
                gridCells[pos.x, pos.y].SetColor(buffColoredCells[pos]);
            }
            else
            {
                gridCells[pos.x, pos.y].SetColor(Color.white);
            }

            // 아이콘도 제거
            gridCells[pos.x, pos.y].ClearIcon();
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

    // 현재 색상과 아이콘 상태를 임시 저장 (실패 시 복원용)
    public void CopyColoredCellToTemp()
    {
        tempColoredCell = new Dictionary<Vector2Int, Color>(coloredCell);

        // 현재 아이콘이 있는 셀들과 스프라이트를 백업
        tempIconCells = new Dictionary<Vector2Int, Sprite>();
        for (int x = 0; x < layoutData.width; x++)
        {
            for (int y = 0; y < layoutData.height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (gridCells[x, y] != null && gridCells[x, y].HasIcon())
                {
                    tempIconCells[pos] = gridCells[x, y].GetIcon();
                }
            }
        }
    }

    // 배치 실패 시 임시 저장된 색상 상태로 복원
    public void OnFailed()
    {
        if (tempColoredCell == null)
            return;

        RestoreColoredCells();
        RestoreCellColorsAndIcons();
    }

    // coloredCell 딕셔너리를 임시 저장 값으로 복원
    private void RestoreColoredCells()
    {
        foreach (var cell in tempColoredCell)
        {
            coloredCell[cell.Key] = cell.Value;
        }
    }

    // 그리드 셀의 시각적 색상과 아이콘을 임시 저장 값으로 복원
    private void RestoreCellColorsAndIcons()
    {
        // 실패 전 상태로 색상 복원
        foreach (var cell in tempColoredCell)
        {
            var pos = cell.Key;
            gridCells[pos.x, pos.y].SetColor(cell.Value);
        }

        // 모든 셀의 아이콘을 먼저 제거
        for (int x = 0; x < layoutData.width; x++)
        {
            for (int y = 0; y < layoutData.height; y++)
            {
                if (gridCells[x, y] != null)
                {
                    gridCells[x, y].ClearIcon();
                }
            }
        }

        // 실패 전에 있던 아이콘 복원
        if (tempIconCells != null)
        {
            foreach (var iconCell in tempIconCells)
            {
                Vector2Int pos = iconCell.Key;
                Sprite icon = iconCell.Value;
                if (IsWithinBounds(pos) && gridCells[pos.x, pos.y] != null)
                {
                    gridCells[pos.x, pos.y].SetIcon(icon);
                }
            }
        }
    }

    // 버프 영역의 색상 업데이트
    public void UpdateBuffColors()
    {
        // 이전 버프 색상 초기화
        buffColoredCells.Clear();

        if (layoutData == null)
        {
            Debug.LogError("layoutData가 null입니다");
            return;
        }

        // 크로스 버프 색상 적용
        if (layoutData.enableCrossBuffs)
        {
            foreach (var crossBuff in layoutData.crossBuffs)
            {
                ApplyColorToHorizontalLine(crossBuff.centerPos, crossBuffColor);
                ApplyColorToVerticalLine(crossBuff.centerPos, crossBuffColor);

                // 중심점
                Color.RGBToHSV(crossBuffColor, out float h, out float s, out float v);
                Color centerColor = Color.HSVToRGB(h, Mathf.Min(s * 2f, 1f), Mathf.Min(v * 2f, 1f));
                centerColor.a = 1f;
                buffColoredCells[crossBuff.centerPos] = centerColor;
            }
        }

        // 리전 버프 색상 적용
        if (layoutData.enableRegionBuffs)
        {
            foreach (var regionBuff in layoutData.regionBuffs)
            {
                ApplyColorToRegion(regionBuff.regionCells, regionBuffColor);
            }
        }

        // 실제 셀에 색상 반영
        ApplyBuffColorsToGrid();
    }

    // 가로줄에 색상 적용
    private void ApplyColorToHorizontalLine(Vector2Int centerPos, Color color)
    {
        for (int x = 0; x < layoutData.width; x++)
        {
            Vector2Int pos = new Vector2Int(x, centerPos.y);
            if (layoutData.IsValidCell(pos))
            {
                buffColoredCells[pos] = color;
            }
        }
    }

    // 세로줄에 색상 적용
    private void ApplyColorToVerticalLine(Vector2Int centerPos, Color color)
    {
        for (int y = 0; y < layoutData.height; y++)
        {
            Vector2Int pos = new Vector2Int(centerPos.x, y);
            if (layoutData.IsValidCell(pos))
            {
                buffColoredCells[pos] = color;
            }
        }
    }

    // 리전에 색상 적용
    private void ApplyColorToRegion(List<Vector2Int> region, Color color)
    {
        foreach (var pos in region)
        {
            if (layoutData.IsValidCell(pos))
            {
                buffColoredCells[pos] = color;
            }
        }
    }

    // 버프 색상을 실제 그리드에 반영
    private void ApplyBuffColorsToGrid()
    {
        int appliedCount = 0;
        foreach (var buffCell in buffColoredCells)
        {
            Vector2Int pos = buffCell.Key;
            Color color = buffCell.Value;

            // 유닛이 이미 배치된 셀은 유닛 색상 유지
            if (!coloredCell.ContainsKey(pos))
            {
                if (IsWithinBounds(pos))
                {
                    gridCells[pos.x, pos.y].SetColor(color);
                    appliedCount++;
                }
            }
        }
    }

    // 버프 활성화 애니메이션 재생
    public void PlayBuffActivationEffect(List<Vector2Int> affectedCells)
    {
        if (affectedCells.Count == 0)
            return;

        float delayIncrement = 0.05f;

        for (int i = 0; i < affectedCells.Count; i++)
        {
            Vector2Int pos = affectedCells[i];
            if (IsWithinBounds(pos) && gridCells[pos.x, pos.y] != null)
            {
                float delay = i * delayIncrement;
                gridCells[pos.x, pos.y].PlayBuffActivationAnimation(delay);
            }
        }
    }
}