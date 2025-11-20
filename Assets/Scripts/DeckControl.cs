using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckControl : MonoBehaviour
{
    public GameObject highlightOverlay;
    public List<DeckSlot> slots;
    public Transform unitListParent;
    public Button completeButton;

    private List<UnitCard> unitCards = new();
    private Dictionary<UnitData, UnitCard> unitMap = new();
    private bool isEditing = false;

    void Awake()
    {
        foreach (var slot in slots)
        {
            slot.SetDeckControl(this);
        }

        completeButton.onClick.AddListener(OnCompleteClicked);
        completeButton.interactable = false;
    }

    void Start()
    {
        highlightOverlay.SetActive(false);

        foreach (Transform t in unitListParent)
        {
            var card = t.GetComponent<UnitCard>();
            var unitData = t.GetComponent<UnitData>();

            if (card != null && unitData != null)
            {
                card.Init(unitData);
                card.Setup(OnUnitCardClicked);
                unitCards.Add(card);
                unitMap[unitData] = card;
            }
        }
    }

    public void OnSlotClicked(DeckSlot slot)
    {
        if (!isEditing)
        {
            EnterEditMode();
            return;
        }

        if (slot.HasPending)
        {
            slot.ClearPending();
            UpdateCompleteButtonState();
            return;
        }
    }

    public void EnterEditMode()
    {
        isEditing = true;
        highlightOverlay.SetActive(true);

        foreach (var slot in slots)
        {
            slot.BeginEdit();
        }

        UpdateCompleteButtonState();
    }

    public void ExitEditMode()
    {
        isEditing = false;
        highlightOverlay.SetActive(false);

        foreach (var slot in slots)
        {
            slot.CancelPending();
        }

        UpdateCompleteButtonState();
    }

    public void ToggleEditMode()
    {
        if (isEditing)
        {
            ExitEditMode();
        }
        else
        {
            EnterEditMode();
        }
    }

    void OnUnitCardClicked(UnitData data)
    {
        if (!isEditing)
        {
            return;
        }

        foreach (var slot in slots)
        {
            if (!slot.HasPending)
            {
                slot.SetPending(data);
                NotifyUnitAssigned(data);
                UpdateCompleteButtonState();
                return;
            }
        }
    }

    public void NotifyUnitAssigned(UnitData data)
    {
        if (unitMap.TryGetValue(data, out var card))
        {
            card.SetVisible(false);
        }
    }

    public void NotifyUnitCleared(UnitData data)
    {
        if (unitMap.TryGetValue(data, out var card))
        {
            card.SetVisible(true);
        }
    }

    void UpdateCompleteButtonState()
    {
        bool allFilled = true;

        foreach (var slot in slots)
        {
            if (!slot.HasPending)
            {
                allFilled = false;
                break;
            }
        }

        completeButton.interactable = allFilled;
    }

    void OnCompleteClicked()
    {
        if (!isEditing)
        {
            return;
        }

        foreach (var slot in slots)
        {
            if (!slot.HasPending)
            {
                return;
            }
        }

        foreach (var slot in slots)
        {
            slot.CommitPending();
        }

        isEditing = false;
        highlightOverlay.SetActive(false);
        UpdateCompleteButtonState();
    }
}
