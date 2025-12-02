using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
    public UnitInfoUI unitInfoUI;

    private DataTable_Unit unitTable;

    private UnitManager unitManager;

    private List<UnitCard> unitCards = new();
    private Dictionary<int, DeckUnitModel> unitModelMap = new();
    private DeckPreset[] presets;
    private bool isEditing = false;
    private int activePresetIndex = 0;


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

        var normalTable = new DataTable_NormalEnforce();
        await normalTable.LoadAsync("NormalEnforceTable");

        var heroTable = new DataTable_HeroEnforce();
        await heroTable.LoadAsync("HeroEnforceTable");

        var heroEffectTable = new DataTable_HeroEnforceEffect();
        await heroEffectTable.LoadAsync("HeroEnforceEffectTable");

        unitManager = new UnitManager(unitTable, normalTable, heroTable, heroEffectTable);


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
        unitInfoUI.SetUnitManager(unitManager);
        Debug.Log("EnforceUI에 UnitManager 전달됨");

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
        foreach (int unitId in PlayData.selectedUnitIds)
        {
            unitManager.AddUnit(unitId);
            UnitData data = unitTable.Get(unitId);
            PlayerUnit pUnit = unitManager.GetPlayerUnit(unitId);

            DeckUnitModel model = new DeckUnitModel
            {
                unitId = unitId,
                unitName = data.NAME,
                iconAddress = data.UNIT_ICON,   
                rawData = data,
                playerUnit = pUnit
            };

            Addressables.LoadAssetAsync<Sprite>(model.iconAddress).Completed += (handle) =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    model.icon = handle.Result;
                }
                else
                {
                    Debug.LogError($"아이콘 로드 실패: {model.iconAddress}");
                }

                UnitCard card = Instantiate(cardPrefab, unitListParent);
                card.Init(model);
                card.Setup(OnUnitCardClicked);
                card.SetVisible(true);

                unitCards.Add(card);
                model.iconAddress = data.UNIT_ICON;
                unitModelMap[unitId] = model;

            };
        }
    }


    //void AutoFillDefaultUnits()
    //{
    //    var defaultUnits = new int[] { 11101, 11104, 11107, 11110, 11113 };

    //    for (int i = 0; i < defaultUnits.Length && i < slots.Count; i++)
    //    {
    //        int id = defaultUnits[i];
    //        if (unitModelMap.ContainsKey(id))
    //        {
    //            slots[i].SetCommittedExternal(unitModelMap[id]);
    //        }
    //    }
    //}


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
            var pending = slot.GetPending();

            slot.CancelPending();

            if (pending != null)
            {
                NotifyUnitCleared(pending);
            }
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
            unitInfoUI.SetUnit(model.unitId);

            Debug.Log("선택된 유닛 " + model.unitId);
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
            var committed = slots[i].GetCommitted();

            if (committed != null)
            {
                committed.FixMissingAddress();
            }

            PlayData.selectedDeckUnitIds[i] = committed != null ? committed.unitId : 0;
            PlayData.selectedDeckUnitIconAddresses[i] = committed != null ? committed.iconAddress : "";
        }


        Debug.Log("저장돰" + string.Join(",", PlayData.selectedDeckUnitIds));

        SavePresets();
        ExitEditMode();
    }



    class SaveData { public int[] ids; }

    void SavePresets()
    {
        for (int i = 0; i < 5; i++)
        {
            var model = presets[activePresetIndex].units[i];

            if (model != null)
            {
                PlayData.selectedDeckUnitIds[i] = model.unitId;
                PlayData.selectedDeckUnitIconAddresses[i] = model.iconAddress;
            }
            else
            {
                PlayData.selectedDeckUnitIds[i] = 0;
                PlayData.selectedDeckUnitIconAddresses[i] = "";                
            }
        }

        Debug.Log(" wjwkd " + string.Join(", ", PlayData.selectedDeckUnitIds));
        Debug.Log(" 아이콘  " + string.Join(", ", PlayData.selectedDeckUnitIconAddresses));
    }



    void LoadPresets()
    {
        DeckPreset preset = presets[activePresetIndex];

        for (int i = 0; i < 5; i++)
        {
            int id = PlayData.selectedDeckUnitIds[i];

            if (id != 0 && unitModelMap.ContainsKey(id))
            {
                preset.units[i] = unitModelMap[id];
            }
            else
            {
                preset.units[i] = null;
            }
        }
    }

    void ExitEditModeIfEditing()
    {
        if (isEditing)
        {
            ExitEditMode();
        }
    }


}
