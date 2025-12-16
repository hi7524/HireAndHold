using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GridPreviewHelper
{
    private readonly List<GameObject> previewCells = new List<GameObject>();
    private readonly Transform parentTransform;
    private readonly float cellSize;
    private Sequence mergeEffectSequence;

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
        StopMergeEffect();

        foreach (var cell in previewCells)
        {
            if (cell != null)
                Object.Destroy(cell);
        }
        previewCells.Clear();
    }

    // 머지 가능 시각 효과 시작 (펄스 + 글로우)
    public void StartMergeEffect()
    {
        StopMergeEffect();

        if (previewCells.Count == 0)
            return;

        mergeEffectSequence = DOTween.Sequence();

        foreach (var cellObj in previewCells)
        {
            if (cellObj == null)
                continue;

            RectTransform rect = cellObj.GetComponent<RectTransform>();
            Image img = cellObj.GetComponent<Image>();

            if (rect != null && img != null)
            {
                // 각 셀의 초기 상태 저장
                Vector3 originalScale = rect.localScale;

                // 펄스 + 글로우 시퀀스
                Sequence cellSequence = DOTween.Sequence()
                    .Append(rect.DOScale(originalScale * 1.15f, 0.3f))
                    .Join(img.DOFade(1f, 0.3f))
                    .Append(rect.DOScale(originalScale, 0.3f))
                    .Join(img.DOFade(0.7f, 0.4f))
                    .SetLoops(-1);

                // 메인 시퀀스에 추가 (모든 셀이 동시에 애니메이션)
                mergeEffectSequence.Join(cellSequence);
            }
        }
    }

    // 머지 효과 중지
    public void StopMergeEffect()
    {
        mergeEffectSequence?.Kill();
        mergeEffectSequence = null;

        // 모든 셀을 원래 스케일과 알파로 복원
        foreach (var cellObj in previewCells)
        {
            if (cellObj == null)
                continue;

            RectTransform rect = cellObj.GetComponent<RectTransform>();
            Image img = cellObj.GetComponent<Image>();

            if (rect != null)
            {
                rect.DOKill();
                rect.localScale = Vector3.one;
            }

            if (img != null)
            {
                img.DOKill();
                Color color = img.color;
                color.a = 1f;
                img.color = color;
            }
        }
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
