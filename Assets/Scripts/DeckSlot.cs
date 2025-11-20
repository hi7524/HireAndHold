using UnityEngine;
using UnityEngine.UI;

public class DeckSlot : MonoBehaviour
{
    public Image icon;
    public Sprite emptySprite;

    private UnitData committed;
    private UnitData pending;

    private DeckControl deckControl;

    public bool HasCommitted => committed != null;
    public bool HasPending => pending != null;

    public void SetDeckControl(DeckControl control)
    {
        deckControl = control;
    }

    public void BeginEdit()
    {
        pending = committed;
        ApplyPendingToUI();
    }

    public void ApplyPendingToUI()
    {
        if (pending != null)
        {
            icon.sprite = pending.icon;
        }
        else
        {
            icon.sprite = emptySprite;
        }
    }

    public void SetPending(UnitData unit)
    {
        if (pending == unit)
        {
            return;
        }

        if (pending != null && pending != committed)
        {
            deckControl.NotifyUnitCleared(pending);
        }

        pending = unit;
        ApplyPendingToUI();
    }

    public void ClearPending()
    {
        if (pending != null && pending != committed)
        {
            deckControl.NotifyUnitCleared(pending);
        }

        pending = null;
        ApplyPendingToUI();
    }

    public void CommitPending()
    {
        committed = pending;
        ApplyPendingToUI();
    }

    public UnitData GetCommitted()
    {
        return committed;
    }

    public void CancelPending()
    {
        if (pending != null && pending != committed)
        {
            deckControl.NotifyUnitCleared(pending);
        }

        pending = committed;
        ApplyPendingToUI();
    }

    public void OnClick()
    {
        deckControl?.OnSlotClicked(this);
    }

    public UnitData GetPending()
    {
        return pending;
    }

}
