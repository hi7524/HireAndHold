using UnityEngine;

public class UnitCardUi : BaseCardUi
{
    public int UnitId; // 테스트용**
    public UnitGridData gridUnitData; // 테스트용**

    [SerializeField] private Transform previewTrans;
    [SerializeField] private DraggableGridUnitUi draggableUnitUI;


    private void Start()
    {
        SetUnitID();
        SetGridData();
    }

    public void SetUnitID()
    {
        draggableUnitUI.SetUnit(UnitId);
    }

    public void SetGridData()
    {
        draggableUnitUI.SetGridData(gridUnitData);
    }
}