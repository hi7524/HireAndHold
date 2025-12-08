using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestSlot : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private DraggableGridUnitUi draggableUnitUi;
    [SerializeField] private TextMeshProUGUI imgText;
    [SerializeField] private TextMeshProUGUI idText;
    [SerializeField] private TextMeshProUGUI nameText;
    private int ID;

    public void SetID(int id)
    {
        ID = id;
        idText.text = $"ID: {ID}";
    }

    public void SetSprite(Sprite sprite)
    {
        imgText.gameObject.SetActive(false);
        image.sprite = sprite;
    }

    public void SetNameText(string name)
    {
        nameText.text = name;
    }

    public void SetDraggableUnitData(int id, int starLevel, UnitGridData gridData)
    {
        if (draggableUnitUi != null)
        {
            draggableUnitUi.SetUnit(id, starLevel);
            draggableUnitUi.SetDraggableUnitType(DraggableUnitType.Inventory);
            draggableUnitUi.SetGridData(gridData);
        }
    }
}