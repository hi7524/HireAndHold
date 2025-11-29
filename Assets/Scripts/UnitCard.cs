using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UnitCard : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;

    private DeckData data;
    private Action<DeckData> onClick;

    public DeckData Data => data;

    public void Init(DeckData unit)
    {
        data = unit;
        icon.sprite = unit.icon;
        nameText.text = unit.unitName;
    }

    public void Setup(Action<DeckData> clickAction)
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
