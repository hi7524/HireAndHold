using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DeckPreset
{
    public DeckUnitModel[] units;
}

public class DeckControl : MonoBehaviour
{
    public GameObject highlightOverlay;
    public List<DeckSlot> slots;
    public Transform unitListParent;
    public Button completeButton;
    public GameObject detailedPanel;
    public List<Button> presetButtons;
    public UnitCard cardPrefab;

    private DataTable_Unit unitTable;

    private UnitManager unitManager;

    private List<UnitCard> unitCards = new();
    private Dictionary<int, DeckUnitModel> unitModelMap = new();
    private DeckPreset[] presets;

    private bool isEditing = false;
    private int activePresetIndex = 0;

    private const string PREF_KEY = "deck_presets_v1";

    void Awake()
    {
        foreach (var slot in slots)
        {
            slot.SetDeckControl(this);
        }

        completeButton.onClick.AddListener(OnCompleteClicked);

        // preset 버튼
        for (int i = 0; i < presetButtons.Count; i++)
        {
            int idx = i;
            presetButtons[i].onClick.AddListener(() => OnClickPresetButton(idx));
            presetButtons[i].onClick.AddListener(ExitEditModeIfEditing);
        }

        completeButton.onClick.AddListener(ExitEditModeIfEditing);

        // presets 초기화
        presets = new DeckPreset[5];
        for (int i = 0; i < presets.Length; i++)
        {
            presets[i] = new DeckPreset();
            presets[i].units = new DeckUnitModel[5];
        }
    }

    async void Start()
    {
        unitTable = new DataTable_Unit();
        await unitTable.LoadAsync("UnitTable");

        unitManager = new UnitManager(unitTable, null, null, null);

        // 유닛 추가
        foreach (var kv in unitTable.RawTable)
        {
            unitManager.AddUnit(kv.Key);
        }

        highlightOverlay.SetActive(false);
        detailedPanel.SetActive(false);

        CreateUnitCards();
        LoadPresets();
        LoadPreset(activePresetIndex);
    }

    void Update()
    {
        if (!isEditing)
        {
            return;
        }

        if (Pointer.current != null &&
            Pointer.current.press.wasPressedThisFrame)
        {
            if (!IsClickOnEditableUI())
            {
                ExitEditMode();
            }
        }
    }

    bool IsClickOnEditableUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (Pointer.current == null)
        {
            return false;
        }

        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = Pointer.current.position.ReadValue(); // ★ 수정된 부분

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(pointer, results);

        foreach (var r in results)
        {
            if (r.gameObject.GetComponent<DeckSlot>() != null)
            {
                return true;
            }

            if (r.gameObject.GetComponent<UnitCard>() != null)
            {
                return true;
            }

            if (presetButtons.Exists(b => r.gameObject.transform.IsChildOf(b.transform)))
            {
                return true;
            }

            if (r.gameObject.transform.IsChildOf(completeButton.transform))
            {
                return true;
            }
        }

        return false;
    }

    void CreateUnitCards()
    {
        foreach (var kv in unitManager.GetAllUnits())
        {
            int id = kv.Key;
            PlayerUnit pUnit = kv.Value;

            UnitData data = unitTable.Get(id);

            DeckUnitModel model = new DeckUnitModel
            {
                unitId = id,
                unitName = data.NAME,
                icon = Resources.Load<Sprite>(data.UNIT_ICON),
                rawData = data,
                playerUnit = pUnit
            };

            unitModelMap[id] = model;

            UnitCard card = Instantiate(cardPrefab, unitListParent);
            card.Init(model);
            card.Setup(OnUnitCardClicked);
            card.SetVisible(true);

            unitCards.Add(card);
        }
    }
    public void OnClickPresetButton(int index)
    {
        if (isEditing)
        {
            ExitEditMode();
        }
        activePresetIndex = index;
        LoadPreset(index);
    }

    void LoadPreset(int index)
    {
        foreach (var card in unitCards)
        {
            card.SetVisible(true);
        }

        DeckPreset preset = presets[index];

        for (int i = 0; i < slots.Count; i++)
        {
            DeckUnitModel model = preset.units[i];
            slots[i].SetCommittedExternal(model);

            if (model != null)
            {
                NotifyUnitAssigned(model);
            }
        }

        highlightOverlay.SetActive(false);
        UpdateCompleteButton();
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
            UpdateCompleteButton();
        }
    }

    void EnterEditMode()
    {
        isEditing = true;
        highlightOverlay.SetActive(true);

        foreach (var slot in slots)
        {
            slot.BeginEdit();
        }

        UpdateCompleteButton();
    }

    void ExitEditMode()
    {
        isEditing = false;
        highlightOverlay.SetActive(false);

        foreach (var slot in slots)
        {
            slot.CancelPending();
        }

        UpdateCompleteButton();
    }

    void OnUnitCardClicked(DeckUnitModel model)
    {
        if (isEditing)
        {
            foreach (var slot in slots)
            {
                if (!slot.HasPending)
                {
                    slot.SetPending(model);
                    NotifyUnitAssigned(model);
                    UpdateCompleteButton();
                    return;
                }
            }
        }
        else
        {
            ExitEditModeIfEditing();

            detailedPanel.SetActive(true);
        }
    }


    public void NotifyUnitAssigned(DeckUnitModel data)
    {
        foreach (var card in unitCards)
        {
            if (card.Data == data)
            {
                card.SetVisible(false);
                return;
            }
        }
    }

    public void NotifyUnitCleared(DeckUnitModel data)
    {
        foreach (var card in unitCards)
        {
            if (card.Data == data)
            {
                card.SetVisible(true);
                return;
            }
        }
    }
    void UpdateCompleteButton()
    {
        foreach (var slot in slots)
        {
            if (!slot.HasPending)
            {
                completeButton.interactable = false;
                return;
            }
        }
        completeButton.interactable = true;
    }

    void OnCompleteClicked()
    {
        if (!isEditing)
        {
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].CommitPending();
            presets[activePresetIndex].units[i] = slots[i].GetCommitted();
        }

        SavePresets();
        ExitEditMode();
    }

    class SaveData { public int[] ids; }

    void SavePresets()
    {
        
    }

    void LoadPresets()
    {
        
    }

    void ExitEditModeIfEditing()
    {
        if (isEditing)
        {
            ExitEditMode();
        }
    }


}
