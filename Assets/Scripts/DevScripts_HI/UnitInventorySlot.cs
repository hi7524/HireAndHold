using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInventorySlot : MonoBehaviour
{
    [SerializeField] private Image unitSprite;
    [SerializeField] private TextMeshProUGUI nameText;

    private DraggableGridUnitUi draggableUnitUi;

    public int UnitId => draggableUnitUi != null ? draggableUnitUi.UnitId : 0;
    public int StarLevel => draggableUnitUi != null ? draggableUnitUi.StarLevel : 1;
    public UnitGridData GridData => draggableUnitUi != null ? draggableUnitUi.GridData : null;


    private void Awake()
    {
        InitializeDraggableUnitUi();
    }

    private void InitializeDraggableUnitUi()
    {
        if (draggableUnitUi == null)
        {
            // DraggableGridUnitUi 컴포넌트 가져오기
            draggableUnitUi = GetComponent<DraggableGridUnitUi>();
        }
    }

    public void SetUnit(int unitId, int starLevel = 1)
    {
        if (draggableUnitUi != null)
        {
            draggableUnitUi.SetUnit(unitId, starLevel);
            draggableUnitUi.SetDraggableUnitType(DraggableUnitType.Inventory);
        }
    }

    public void SetGridData(UnitGridData gridData)
    {
        if (draggableUnitUi != null)
        {
            draggableUnitUi.SetGridData(gridData);
        }
    }

    public void UpdatePreviewImages(UnitGridData newGridData)
    {
        if (draggableUnitUi != null)
        {
            draggableUnitUi.UpdatePreviewImages(newGridData);
        }
    }

    public void SetInventory(UnitInventory inventory)
    {
        if (draggableUnitUi != null)
        {
            draggableUnitUi.SetInventory(inventory);
        }
    }

    public void UpdateUi()
    {
        if (nameText != null && draggableUnitUi != null)
        {
            var unitData = DataTableManager.UnitTable.Get(draggableUnitUi.UnitId);
            nameText.text = unitData.NAME;
        }
    }

    public void UpdateUnitSprite(Sprite sprite)
    {
        if (unitSprite != null)
            unitSprite.sprite = sprite;
    }
}