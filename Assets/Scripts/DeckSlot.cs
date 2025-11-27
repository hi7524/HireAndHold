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

    private void ApplyCommittedToUI()
    {
        if (committed != null)
        {
            icon.sprite = committed.icon;
        }
        else
        {
            icon.sprite = emptySprite;
        }
    }

    private void ApplyPendingToUI()
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

    public void BeginEdit()
    {
        pending = committed;
        ApplyPendingToUI();
    }

    public void CancelPending()
    {
        pending = committed;
        ApplyCommittedToUI();
    }

    public void SetCommittedExternal(UnitData data)
    {
        committed = data;
        pending = null;
        ApplyCommittedToUI();  
    }

    public void SetPending(UnitData unit)
    {
        if (pending != null && pending != committed)
        {
            deckControl?.NotifyUnitCleared(pending);
        }

        pending = unit;
        ApplyPendingToUI();
    }

    public void ClearPending()
    {
        if (pending != null && pending != committed)
        {
            deckControl?.NotifyUnitCleared(pending);
        }

        pending = null;
        ApplyPendingToUI();
    }

    public void CommitPending()
    {
        if (committed != pending)
        {
            if (committed != null)
            {
                deckControl?.NotifyUnitCleared(committed);
            }

            committed = pending;
        }

        pending = null;
        ApplyCommittedToUI();  
    }

    public UnitData GetCommitted()
    {
        return committed;
    }

    public void OnClick()
    {
        deckControl?.OnSlotClicked(this);
    }
}
