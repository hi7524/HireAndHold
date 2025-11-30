using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UnitCard : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;

    private DeckUnitModel data;

    private Action<DeckUnitModel> onClick;

    public DeckUnitModel Data => data;

    public void Init(DeckUnitModel unit)
    {
        data = unit;
        icon.sprite = unit.icon;
        nameText.text = unit.unitName;
    }

    public void Setup(Action<DeckUnitModel> clickAction)
    {
        onClick = clickAction;
    }

    public void OnClick()
    {
        onClick?.Invoke(data);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
