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
            }
        }
    }

    public void EnterEditMode()
    {
        isEditing = true;
        highlightOverlay.SetActive(true);
        UpdateCompleteButtonState();
    }

    public void ExitEditMode()
    {
        isEditing = false;
        highlightOverlay.SetActive(false);
    }

    void OnUnitCardClicked(UnitData data)
    {
        if (!isEditing)
        {
            return;
        }


        foreach (var slot in slots)
        {
            if (!slot.HasUnit)
            {
                slot.SetUnit(data);
                UpdateCompleteButtonState();
                return;
            }
        }

        Debug.Log(" x empty slot");
    }

    void UpdateCompleteButtonState()
    {
        bool allFilled = true;
        foreach (var slot in slots)
        {
            if (!slot.HasUnit)
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
            if (!slot.HasUnit)
            {
                Debug.Log("fill all slots");
                return;
            }
        }

        ExitEditMode();
        Debug.Log("deck composition finished");
    }
}
