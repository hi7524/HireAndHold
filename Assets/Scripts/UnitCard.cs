using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UnitCard : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;

    private UnitData data;
    private Action<UnitData> onClick;

    public UnitData Data => data;

    public void Init(UnitData unit)
    {
        data = unit;
        icon.sprite = unit.icon;
        nameText.text = unit.unitName;
    }

    public void Setup(Action<UnitData> clickAction)
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
