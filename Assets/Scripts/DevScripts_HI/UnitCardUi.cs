using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UnitCardUi : BaseCardUi
{
    [SerializeField] private Transform previewTrans;
    [SerializeField] private DraggableGridUnitUi draggableUnitUI;
    [SerializeField] private float cellUISize = 20f;

    private int unitId;
    private UnitGridData gridUnitData;
    private Image img;

    // 드롭 성공 이벤트
    public event Action OnUnitDropSuccess;

    // 프리뷰 오브젝트
    private readonly List<GameObject> previewImages = new List<GameObject>();

    private void Awake()
    {
        img = gameObject.GetComponent<Image>();
    }

    private void Start()
    {
        VisualizeGridData();

        draggableUnitUI.OnDropSuccess += HandleUnitDropSuccess;
    }

    private void OnDestroy()
    {
        if (draggableUnitUI != null)
        {
            draggableUnitUI.OnDropSuccess -= HandleUnitDropSuccess;
        }
    }

    private void OnEnable()
    {
        draggableUnitUI.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        draggableUnitUI.gameObject.SetActive(false);
    }

    private void HandleUnitDropSuccess()
    {
        OnUnitDropSuccess?.Invoke();
    }

    public void SetUnitID(int unitId)
    {
        this.unitId = unitId;
        draggableUnitUI.SetUnit(unitId);
    }

    public void SetGridData(UnitGridData gridData)
    {
        this.gridUnitData = gridData;
        draggableUnitUI.SetGridData(gridData);
        VisualizeGridData();
    }

    public void SetDragState(bool value)
    {
        draggableUnitUI.SetDraggableState(value);
    }

    public void SetColor(Color color)
    {
        if (img != null)
        {
            img.color = color;
        }
    }

    public void VisualizeGridData()
    {
        if (gridUnitData == null)
            return;

        // 기존 프리뷰 이미지가 있다면 업데이트, 없다면 생성
        if (previewImages.Count > 0)
        {
            UpdatePreviewImages(gridUnitData);
        }
        else
        {
            CreatePreviewUIImages();
        }
    }

    // GridData가 변경되었을 때 기존 미리보기 이미지들을 재사용하여 갱신
    private void UpdatePreviewImages(UnitGridData newGridData)
    {
        if (newGridData == null)
            return;

        var occupiedCells = newGridData.GetOccupiedCells();
        int requiredCount = occupiedCells.Count + 1;

        // 부족한 셀 추가
        while (previewImages.Count < requiredCount)
        {
            var cellPos = previewImages.Count == 0 ? Vector2Int.zero : occupiedCells[previewImages.Count - 1];
            CreatePreviewUICell(cellPos, newGridData);
        }

        // 넘치는 셀 삭제
        while (previewImages.Count > requiredCount)
        {
            int lastIndex = previewImages.Count - 1;
            GameObject objToRemove = previewImages[lastIndex];
            previewImages.RemoveAt(lastIndex);
            Destroy(objToRemove);
        }

        // 기존 셀들의 색상 및 위치 갱신
        UpdatePreviewCell(0, Vector2Int.zero, newGridData);

        for (int i = 0; i < occupiedCells.Count; i++)
        {
            UpdatePreviewCell(i + 1, occupiedCells[i], newGridData);
        }
    }

    // 개별 미리보기 셀의 위치와 색상 갱신
    private void UpdatePreviewCell(int index, Vector2Int cellPos, UnitGridData gridData)
    {
        if (index < 0 || index >= previewImages.Count)
            return;

        GameObject cellObj = previewImages[index];
        cellObj.name = $"PreviewCell_{cellPos.x}_{cellPos.y}";

        // 위치 갱신
        RectTransform rect = cellObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(cellPos.x * cellUISize, cellPos.y * cellUISize);

        // 색상 갱신
        Image img = cellObj.GetComponent<Image>();
        img.color = gridData.gridColor;
    }

    // 프리뷰를 위한 이미지 생성
    private void CreatePreviewUIImages()
    {
        if (gridUnitData == null)
            return;

        var occupiedCells = gridUnitData.GetOccupiedCells();

        CreatePreviewUICell(Vector2Int.zero, gridUnitData);

        foreach (var cellPos in occupiedCells)
        {
            CreatePreviewUICell(cellPos, gridUnitData);
        }
    }

    // 셀 단위 이미지 생성
    private void CreatePreviewUICell(Vector2Int cellPos, UnitGridData gridData)
    {
        // UI Image GameObject 생성
        GameObject cellObj = new($"PreviewCell_{cellPos.x}_{cellPos.y}");
        cellObj.transform.SetParent(previewTrans, false);
        previewImages.Add(cellObj);

        // RectTransform 설정
        RectTransform rect = cellObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(cellUISize, cellUISize);
        rect.anchoredPosition = new Vector2(cellPos.x * cellUISize, cellPos.y * cellUISize);

        // 색상 설정
        Image img = cellObj.AddComponent<Image>();
        img.color = gridData.gridColor;
    }
}