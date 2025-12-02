using UnityEngine;
using UnityEngine.UI;

public class DeckSlot : MonoBehaviour
{
    public Image icon;
    public Sprite emptySprite;

    private DeckUnitModel committed;
    private DeckUnitModel pending;
    private DeckControl deck;

    public bool HasCommitted => committed != null;
    public bool HasPending => pending != null;

    public void SetDeckControl(DeckControl control)
    {
        deck = control;
    }

    public void BeginEdit()
    {
        pending = committed;

        if (pending != null)
        {
            pending.FixMissingAddress();
        }

        ApplyPendingToUI();
    }

    public void CancelPending()
    {
        pending = committed;

        if (pending != null)
        {
            pending.FixMissingAddress();
        }

        ApplyCommittedToUI();
    }

    public void SetPending(DeckUnitModel model)
    {
        pending = model;

        if (pending != null)
        {
            pending.FixMissingAddress();
        }

        ApplyPendingToUI();
    }

    public void ClearPending()
    {
        if (pending != null)
        {
            deck.NotifyUnitCleared(pending);
        }

        pending = null;
        ApplyPendingToUI();
    }

    public void CommitPending()
    {
        if (committed != null)
        {
            deck.NotifyUnitCleared(committed);
        }

        if (pending != null)
        {
            pending.FixMissingAddress();  
        }

        committed = pending;
        pending = null;

        if (committed != null)
        {
            committed.FixMissingAddress();
        }

        ApplyCommittedToUI();
    }

    public DeckUnitModel GetCommitted() => committed;
    public DeckUnitModel GetPending() => pending;

    public void SetCommittedExternal(DeckUnitModel model)
    {
        committed = model;

        if (committed != null)
        {
            committed.FixMissingAddress();
        }

        pending = null;
        ApplyCommittedToUI();
    }

    void ApplyCommittedToUI()
    {
        icon.sprite = committed != null ? committed.icon : emptySprite;
    }

    void ApplyPendingToUI()
    {
        icon.sprite = pending != null ? pending.icon : emptySprite;
    }

    public void OnClick()
    {
        deck.OnSlotClicked(this);
    }
}
