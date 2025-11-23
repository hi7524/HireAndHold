using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridPreviewHelper
{
    private readonly List<GameObject> previewCells = new List<GameObject>();
    private readonly Transform parentTransform;
    private readonly float cellSize;

    public GridPreviewHelper(Transform parent, float cellSize)
    {
        this.parentTransform = parent;
        this.cellSize = cellSize;
    }

    // 프리뷰 셀 생성 또는 업데이트
    public void UpdatePreview(UnitGridData gridData)
    {
        if (gridData == null)
            return;

        var occupiedCells = gridData.GetOccupiedCells();
        int requiredCount = occupiedCells.Count + 1;

        AdjustCellCount(requiredCount, occupiedCells, gridData);
        UpdateAllCells(occupiedCells, gridData);
    }

    // 프리뷰 초기 생성
    public void CreatePreview(UnitGridData gridData)
    {
        if (gridData == null)
            return;

        var occupiedCells = gridData.GetOccupiedCells();

        CreateCell(Vector2Int.zero, gridData);

        foreach (var cellPos in occupiedCells)
        {
            CreateCell(cellPos, gridData);
        }
    }

    // 프리뷰 셀 표시
    public void Show()
    {
        foreach (var cell in previewCells)
        {
            if (cell != null)
                cell.SetActive(true);
        }
    }

    // 프리뷰 셀 숨김
    public void Hide()
    {
        foreach (var cell in previewCells)
        {
            if (cell != null)
                cell.SetActive(false);
        }
    }

    // 프리뷰 셀 정리
    public void Clear()
    {
        foreach (var cell in previewCells)
        {
            if (cell != null)
                Object.Destroy(cell);
        }
        previewCells.Clear();
    }

    // 프리뷰 셀 개수가 있는지 확인
    public bool HasCells => previewCells.Count > 0;

    // 필요한 셀 개수 조정
    private void AdjustCellCount(int requiredCount, List<Vector2Int> occupiedCells, UnitGridData gridData)
    {
        while (previewCells.Count < requiredCount)
        {
            var cellPos = previewCells.Count == 0 ? Vector2Int.zero : occupiedCells[previewCells.Count - 1];
            CreateCell(cellPos, gridData);
        }

        while (previewCells.Count > requiredCount)
        {
            int lastIndex = previewCells.Count - 1;
            GameObject objToRemove = previewCells[lastIndex];
            previewCells.RemoveAt(lastIndex);
            Object.Destroy(objToRemove);
        }
    }

    // 모든 셀 업데이트
    private void UpdateAllCells(List<Vector2Int> occupiedCells, UnitGridData gridData)
    {
        UpdateCell(0, Vector2Int.zero, gridData);

        for (int i = 0; i < occupiedCells.Count; i++)
        {
            UpdateCell(i + 1, occupiedCells[i], gridData);
        }
    }

    // 개별 셀 업데이트
    private void UpdateCell(int index, Vector2Int cellPos, UnitGridData gridData)
    {
        if (index < 0 || index >= previewCells.Count)
            return;

        GameObject cellObj = previewCells[index];
        cellObj.name = $"PreviewCell_{cellPos.x}_{cellPos.y}";

        RectTransform rect = cellObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(cellPos.x * cellSize, cellPos.y * cellSize);

        Image img = cellObj.GetComponent<Image>();
        img.color = gridData.gridColor;
    }

    // 셀 생성
    private void CreateCell(Vector2Int cellPos, UnitGridData gridData)
    {
        GameObject cellObj = new();
        cellObj.transform.SetParent(parentTransform, false);
        previewCells.Add(cellObj);

        RectTransform rect = cellObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(cellSize, cellSize);
        rect.anchoredPosition = new Vector2(cellPos.x * cellSize, cellPos.y * cellSize);

        Image img = cellObj.AddComponent<Image>();
        img.color = gridData.gridColor;
    }
}
