using UnityEngine;
using UnityEngine.UI;

public class DeckSlot : MonoBehaviour
{
    public Image icon;
    public Sprite emptySprite;

    private UnitData current;
    private DeckControl deckControl;

    public bool HasUnit => current != null;

    public void SetDeckControl(DeckControl control)
    {
        deckControl = control;
    }

    public void SetUnit(UnitData unit)
    {
        current = unit;
        icon.sprite = unit.icon;
    }

    public void Clear()
    {
        current = null;
        icon.sprite = emptySprite;
    }

    public void OnClick()
    {
        if (HasUnit)
        {
            Clear();
        }
        else
        {
            deckControl?.EnterEditMode();
        }
    }
}
