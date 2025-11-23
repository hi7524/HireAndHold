using System;
using UnityEngine;
using UnityEngine.UI;

public class UnitCardUi : BaseCardUi
{
    [SerializeField] private Transform previewTrans; // 왼쪽 상단에 작게 표시할 미리보기 그리드
    [SerializeField] private DraggableGridUnitUi draggableUnitUI; // 실제로 드래그 할 유닛 UI
    [SerializeField] private float cellUISize = 20f;

    private int unitId;
    private UnitGridData gridUnitData;
    private Image img;

    // 드롭 성공 이벤트
    public event Action OnUnitDropSuccess;

    // 프리뷰 헬퍼
    private GridPreviewHelper previewHelper;

    private void Awake()
    {
        img = gameObject.GetComponent<Image>();
    }

    private void Start()
    {
        VisualizeGridData();

        draggableUnitUI.OnUnitDroppedSuccessfully += HandleUnitDropSuccess;
    }

    private void OnDestroy()
    {
        if (draggableUnitUI != null)
        {
            draggableUnitUI.OnUnitDroppedSuccessfully -= HandleUnitDropSuccess;
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
        draggableUnitUI.SetDraggableUnitType(DraggableUnitType.LevelUp);
    }

    public void SetGridData(UnitGridData gridData)
    {
        this.gridUnitData = gridData;
        draggableUnitUI.SetGridData(gridData);

        if (previewHelper == null)
        {
            previewHelper = new GridPreviewHelper(previewTrans, cellUISize);
        }

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

        if (previewHelper == null)
        {
            previewHelper = new GridPreviewHelper(previewTrans, cellUISize);
        }

        if (previewHelper.HasCells)
        {
            previewHelper.UpdatePreview(gridUnitData);
        }
        else
        {
            previewHelper.CreatePreview(gridUnitData);
        }

        previewHelper.Show();
    }
}