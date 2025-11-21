using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInventorySlot : MonoBehaviour, IDraggable
{
    [SerializeField] private Image unitImg;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Transform previewObjTrans;
    [SerializeField] private float cellUISize = 65f;

    public bool IsDraggable => true; // 수정 필요: 편집 시스템과 연동짓기 **
    public GameObject GameObject => gameObject;

    public int UnitId { get; private set; }
    public UnitGridData GridData { get; private set; }

    private UnitInventory inventory;

    private readonly List<GameObject> previewImages = new List<GameObject>();
    private bool dropFailed = false;


    public void SetUnit(int unitId)
    {
        this.UnitId = unitId;
    }

    public void SetGridData(UnitGridData gridData)
    {
        this.GridData = gridData;

        // 기존 프리뷰 이미지가 있다면 UpdatePreviewImages 사용
        if (previewImages.Count > 0)
        {
            UpdatePreviewImages(gridData);
        }
        else
        {
            CreatePreviewUIImages();
            SetActivePreviewImages(false);
        }
    }

    // GridData가 변경되었을 때 기존 미리보기 이미지들을 재사용하여 갱신
    public void UpdatePreviewImages(UnitGridData newGridData)
    {
        if (newGridData == null)
            return;

        this.GridData = newGridData;

        var occupiedCells = GridData.GetOccupiedCells();
        // 기본 셀 (Vector2Int.zero) + 추가 셀들
        int requiredCount = occupiedCells.Count + 1;

        // 부족한 셀이 있으면 추가 생성
        while (previewImages.Count < requiredCount)
        {
            var cellPos = previewImages.Count == 0 ? Vector2Int.zero : occupiedCells[previewImages.Count - 1];
            CreatePreviewUICell(cellPos);
        }

        // 넘치는 셀이 있으면 삭제
        while (previewImages.Count > requiredCount)
        {
            int lastIndex = previewImages.Count - 1;
            GameObject objToRemove = previewImages[lastIndex];
            previewImages.RemoveAt(lastIndex);
            Destroy(objToRemove);
        }

        // 기존 셀들의 색상과 위치 갱신
        // 첫 번째 셀 (기본 위치)
        UpdatePreviewCell(0, Vector2Int.zero);

        // 나머지 셀들
        for (int i = 0; i < occupiedCells.Count; i++)
        {
            UpdatePreviewCell(i + 1, occupiedCells[i]);
        }

        SetActivePreviewImages(false);
    }

    // 개별 미리보기 셀의 위치와 색상 갱신
    private void UpdatePreviewCell(int index, Vector2Int cellPos)
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
        img.color = GridData.gridColor;
    }

    public void SetInventory(UnitInventory inventory)
    {
        this.inventory = inventory;
    }

    public void UpdateUi()
    {
        // 데이터 테이블과 연결 및 수정 필요 **
        // unitImg.sprite = unitSprite;
        var unitData = DataTableManager.UnitTable.Get(UnitId);
        nameText.text = unitData.UNIT_NAME;
    }


    public void OnDragStart()
    {
        SetActivePreviewImages(true);
    }

    public void OnDrag()
    {
        Debug.Log($"유닛 ID: {UnitId}");
    }

    public void OnDragEnd()
    {
        SetActivePreviewImages(false);

        if (!dropFailed)
        {
            inventory.RemoveUnit(UnitId);
        }
        dropFailed = false;
    }

    public void OnDropFailed()
    {
        dropFailed = true;
    }


    // 프리뷰를 위한 이미지 생성
    private void CreatePreviewUIImages()
    {
        if (GridData == null)
            return;

        var occupiedCells = GridData.GetOccupiedCells();

        CreatePreviewUICell(Vector2Int.zero);

        foreach (var cellPos in occupiedCells)
        {
            CreatePreviewUICell(cellPos);
        }
    }

    // 쎌 단위 이미지 생성
    private void CreatePreviewUICell(Vector2Int cellPos)
    {
        // UI Image GameObject 생성
        GameObject cellObj = new($"PreviewCell_{cellPos.x}_{cellPos.y}");
        cellObj.transform.SetParent(previewObjTrans, false);
        previewImages.Add(cellObj);

        // RectTransform 설정
        RectTransform rect = cellObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(cellUISize, cellUISize);
        rect.anchoredPosition = new Vector2(cellPos.x * cellUISize, cellPos.y * cellUISize);

        // 색상 설정
        Image img = cellObj.AddComponent<Image>();
        img.color = GridData.gridColor;
    }

    // 미리보기 이미지 활성화 및 비활성화
    private void SetActivePreviewImages(bool value)
    {
        if (previewImages == null || previewImages.Count == 0)
            return;

        if (value)
        {
            SetActivePreviewImages();
        }
        else
        {
            foreach (var previewImg in previewImages)
            {
                previewImg.SetActive(false);
            }
        }
    }

    // 활성화
    private void SetActivePreviewImages()
    {
        if (previewImages == null || previewImages.Count == 0)
            return;

        previewObjTrans.localScale = 0.5f * Vector3.one;
        foreach (var previewImg in previewImages)
        {
            previewImg.SetActive(true);
        }

        previewObjTrans.DOScale(1.0f, 0.15f);
    }

    public void OnDropSuccess()
    {
    }
}