using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInventorySlot : MonoBehaviour, ITestDraggable
{
    [SerializeField] private Image unitImg;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Transform previewObjTrans;
    [SerializeField] private float cellUISize = 65f;

    public bool IsDraggable => true; // 수정 필요: 편집 시스템과 연동짓기 **
    public GameObject GameObject => gameObject;
    public UnitGridData GridData { get; private set; }

    private UnitInventory inventory;

    private int unitId;
    private readonly List<GameObject> previewImages = new List<GameObject>();
    private bool dropFailed = false;


    public void SetUnit(int unitId)
    {
        this.unitId = unitId;
    }

    public void SetGridData(UnitGridData gridData)
    {
        this.GridData = gridData;
        CreatePreviewUIImages();
        SetActivePreviewImages(false);
    }

    public void SetInventory(UnitInventory inventory)
    {
        this.inventory = inventory;
    }

    public void UpdateUi()
    {
        // 데이터 테이블과 연결 및 수정 필요 **
        // unitImg.sprite = unitSprite;
        var unitData = DataTableManager.UnitTable.Get(unitId);
        nameText.text = unitData.UNIT_NAME;
    }


    public void OnDragStart()
    {
        SetActivePreviewImages(true);
    }

    public void OnDrag()
    {
        Debug.Log($"유닛 ID: {unitId}");
    }

    public void OnDragEnd()
    {
        SetActivePreviewImages(false);

        if (!dropFailed)
        {
            inventory.RemoveUnit(unitId);
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

    //미리보기 이미지 활성화 및 비활성화
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
}