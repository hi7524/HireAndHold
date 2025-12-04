using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
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
    public UnitInfoUI unitInfoUI;

    private DataTable_Unit unitTable;
    private UnitManager unitManager;

    private List<UnitCard> unitCards = new();
    private Dictionary<int, DeckUnitModel> unitModelMap = new();
    private DeckPreset[] presets;
    private bool isEditing = false;
    private int activePresetIndex = 0;

    public StageDeck stageDeck;


    void Awake()
    {
        foreach (var slot in slots)
        {
            slot.SetDeckControl(this);
        }

        completeButton.onClick.AddListener(() => OnCompleteClicked().Forget());

        // preset 버튼
        for (int i = 0; i < presetButtons.Count; i++)
        {
            int idx = i;
            presetButtons[i].onClick.AddListener(() => OnClickPresetButton(idx));
            presetButtons[i].onClick.AddListener(ExitEditModeIfEditing);
        }

        completeButton.onClick.AddListener(ExitEditModeIfEditing);

        // presets 배열 초기화 
        presets = new DeckPreset[5];
        for (int i = 0; i < presets.Length; i++)
        {
            presets[i] = new DeckPreset();
            presets[i].units = new DeckUnitModel[5];
        }
    }

    async void Start()
    {

        await DatabaseManager.Instance.WaitForInitializationAsync();

        if (DatabaseManager.Instance.CurrentUser == null)
        {
            await DatabaseManager.Instance.LoadUserDataAsync();
        }

        activePresetIndex = PlayData.currentSelectedPreset;

        unitTable = new DataTable_Unit();
        await unitTable.LoadAsync("UnitTable");

        var normalTable = new DataTable_NormalEnforce();
        await normalTable.LoadAsync("NormalEnforceTable");

        var heroTable = new DataTable_HeroEnforce();
        await heroTable.LoadAsync("HeroEnforceTable");

        var heroEffectTable = new DataTable_HeroEnforceEffect();
        await heroEffectTable.LoadAsync("HeroEnforceEffectTable");

        unitManager = new UnitManager(unitTable, normalTable, heroTable, heroEffectTable);

        foreach (var kv in unitTable.RawTable)
        {

            unitManager.AddUnit(kv.Key);
        }

        highlightOverlay.SetActive(false);
        detailedPanel.SetActive(false);
        await CreateUnitCards();


        DatabaseManager.Instance.SyncPresetsToPlayData();  

        LoadPresets();
        AutoFillPresetIfEmpty(activePresetIndex);
        LoadPreset(activePresetIndex);


        UpdatePresetButtonsStates();
        unitInfoUI.SetUnitManager(unitManager);

    }
    private async void AutoFillPresetIfEmpty(int presetIndex)
    {
        DeckPreset preset = presets[presetIndex];

        bool isEmpty = true;
        for (int i = 0; i < preset.units.Length; i++)
        {
            if (preset.units[i] != null)
            {
                isEmpty = false;
                break;
            }
        }

        if (!isEmpty)
        {
            return;
        }

  
        var owned = DatabaseManager.Instance.GetAllCharacters();

        if (owned.Count == 0)
        {
            return;
        }

        for (int i = 0; i < 5 && i < owned.Count; i++)
        {
            int unitId = int.Parse(owned[i].id);

            if (!unitModelMap.ContainsKey(unitId))
                continue;

            DeckUnitModel model = unitModelMap[unitId];
            preset.units[i] = model;

            PlayData.selectedDeckUnitIds[presetIndex, i] = model.unitId;
            PlayData.selectedDeckUnitIconAddresses[presetIndex, i] = model.iconAddress;
        }

        await DatabaseManager.Instance.SavePresetFromPlayDataAsync(presetIndex);

        Debug.Log($"[AutoFillPresetIfEmpty] Preset {presetIndex} 자동 편성 완료");
    }


    private void Update()
    {
        if (!isEditing)
        {
            return;
        }

        if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame)
        {
            return;
        }

        if (EventSystem.current == null)
        {
            return;
        }
    
        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = Pointer.current.position.ReadValue();
    
        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(pointer, results);
    
        bool clickedEditableUI = false;
        bool clickedAnyButton = false;
    
        foreach (var r in results)
        {
            if (r.gameObject.GetComponent<DeckSlot>() != null)
            {
                clickedEditableUI = true;
                break;
            }
    
            if (r.gameObject.GetComponent<UnitCard>() != null)
            {
                clickedEditableUI = true;
                break;
            }
    
            if (presetButtons.Exists(b => r.gameObject.transform.IsChildOf(b.transform)))
            {
                clickedEditableUI = true;
                break;
            }
    
            if (r.gameObject.transform.IsChildOf(completeButton.transform))
            {
                clickedEditableUI = true;
                break;
            }
        }
    
        if (clickedEditableUI)
        {
            return;
        }

        foreach (var r in results)
        {
            if (r.gameObject.GetComponent<Button>() != null)
            {
                clickedAnyButton = true;
                break;
            }
        }
    
        if (clickedAnyButton)
        {
            ExitEditMode();
        }
    }


    async UniTask CreateUnitCards()
    {
        List<UniTask> loadTasks = new();

        var ownedCharacters = DatabaseManager.Instance.GetAllCharacters();

        foreach (var character in ownedCharacters)
        {
            int unitId = int.Parse(character.id);
            UnitData data = unitTable.Get(unitId);
            if (data == null)
            {
                continue;
            }

            var pUnit = unitManager.GetPlayerUnit(unitId);

            var model = new DeckUnitModel
            {
                unitId = unitId,
                unitName = data.StringName,
                iconAddress = data.UNIT_ICON,
                rawData = data,
                playerUnit = pUnit
            };

            var loadTask = Addressables.LoadAssetAsync<Sprite>(model.iconAddress).Task.AsUniTask().ContinueWith(result =>
                {
                    model.icon = result;

                    var card = GameObject.Instantiate(cardPrefab, unitListParent);
                    card.Init(model);
                    card.Setup(OnUnitCardClicked);
                    card.SetVisible(true);

                    unitCards.Add(card);
                    unitModelMap[unitId] = model;
                });

            loadTasks.Add(loadTask);
        }

        await UniTask.WhenAll(loadTasks);
    }


    public async void OnClickPresetButton(int index)
    {
        activePresetIndex = index;
        PlayData.currentSelectedPreset = index;

        await DatabaseManager.Instance.SetActivePresetAsync(index);

        LoadPreset(index);
        UpdatePresetButtonsStates();

        if (stageDeck != null && stageDeck.isActiveAndEnabled)
        {
            stageDeck.Refresh();
        }
    }

    void LoadPreset(int index)
    {
        foreach (var card in unitCards)
        {
            card.SetAssigned(false);
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

        //Pending 제거
        if (slot.HasPending)
        {
            var pending = slot.GetPending();
            slot.ClearPending();
            NotifyUnitCleared(pending);
            UpdateCompleteButton();
            return;
        }

        if (slot.HasCommitted)
        {
            var committed = slot.GetCommitted();
            slot.SetPending(null);
            slot.CommitPending();  
            NotifyUnitCleared(committed);
            UpdateCompleteButton();
            return;
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
        LoadPreset(activePresetIndex);
        UpdateCompleteButton();
    }




    void OnUnitCardClicked(DeckUnitModel model)
    {
        if (isEditing)
        {

            foreach (var slot in slots)
            {
                var pending = slot.GetPending();
                if (pending != null && pending.unitId == model.unitId)
                {
                    Debug.Log("이미편성 있음 (pending)");
                    return;
                }

                var committed = slot.GetCommitted();
                if (committed != null && committed.unitId == model.unitId)
                {
                    Debug.Log("이미편성 있음 (committed)");
                    return;
                }
            }

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

            Debug.Log("선택된 " + model.unitId);
        }
    }



    public void NotifyUnitAssigned(DeckUnitModel data)
    {
        foreach (var card in unitCards)
        {
            if (card.Data == data)
            {
                card.SetAssigned(true); 
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
                card.SetAssigned(false);  
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

    async UniTaskVoid OnCompleteClicked()
    {
        if (!isEditing)
        {
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].CommitPending();

            var committed = slots[i].GetCommitted();
            presets[activePresetIndex].units[i] = committed;

            if (committed != null)
            {
                committed.FixMissingAddress();
                PlayData.selectedDeckUnitIds[activePresetIndex, i] = committed.unitId;
                PlayData.selectedDeckUnitIconAddresses[activePresetIndex, i] = committed.iconAddress;
            }
            else
            {
                PlayData.selectedDeckUnitIds[activePresetIndex, i] = 0;
                PlayData.selectedDeckUnitIconAddresses[activePresetIndex, i] = "";
            }
        }

        await DatabaseManager.Instance.SavePresetFromPlayDataAsync(activePresetIndex);

        ExitEditMode(); 
        if (stageDeck != null && stageDeck.isActiveAndEnabled)
        {
            stageDeck.Refresh();
        }
    }



    void UpdatePresetButtonsStates()
    {
        for (int i = 0; i < presetButtons.Count; i++)
        {
            bool isActive = (i == activePresetIndex);

            var colors = presetButtons[i].colors;
            colors.normalColor = isActive ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
            colors.selectedColor = colors.normalColor;
            presetButtons[i].colors = colors;
        }
    }

    void LoadPresets()
    {
        for (int p = 0; p < 5; p++)
        {
            DeckPreset preset = presets[p];

            for (int i = 0; i < 5; i++)
            {
                int id = PlayData.selectedDeckUnitIds[p, i];

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
    }


    void ExitEditModeIfEditing()
    {
        if (isEditing)
        {
            ExitEditMode();
        }
    }
}
