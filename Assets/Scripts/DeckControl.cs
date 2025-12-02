using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

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
            presetButtons[i].onClick.AddListener(() => OnClickPresetButton(idx).Forget());
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
        LoadPresets();              
        LoadPreset(activePresetIndex);

        UpdatePresetButtonsStates();
        unitInfoUI.SetUnitManager(unitManager);

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
        if (EventSystem.current == null || Pointer.current == null)
        {
            return false;
        }

        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = Pointer.current.position.ReadValue();

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

    async UniTask CreateUnitCards()
    {
        List<UniTask> loadTasks = new();

        foreach (int unitId in PlayData.selectedUnitIds)
        {
            UnitData data = unitTable.Get(unitId);
            if (data == null)
            {
                continue;
            }

            var pUnit = unitManager.GetPlayerUnit(unitId);

            var model = new DeckUnitModel
            {
                unitId = unitId,
                unitName = data.NAME,
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


    public async UniTaskVoid OnClickPresetButton(int index)
    {
       
        await DatabaseManager.Instance.SetActivePresetAsync(index);

        PlayData.currentSelectedPreset = index;
        activePresetIndex = index;

        DatabaseManager.Instance.SyncPresetsToPlayData();

        LoadPresets();
        LoadPreset(index);
        UpdatePresetButtonsStates();

        if (stageDeck != null && stageDeck.isActiveAndEnabled)
        {
            stageDeck.Refresh();
        }
      
    }



    void ApplyPresetToPlayData(int index)
    {
        DeckPreset preset = presets[index];

        for (int i = 0; i < 5; i++)
        {
            if (preset.units[i] != null)
            {
                PlayData.selectedDeckUnitIds[index, i] = preset.units[i].unitId;
                PlayData.selectedDeckUnitIconAddresses[index, i] = preset.units[i].iconAddress;
            }
            else
            {
                PlayData.selectedDeckUnitIds[index, i] = 0;
                PlayData.selectedDeckUnitIconAddresses[index, i] = "";
            }
        }

        Debug.Log("[DeckControl] PlayData updated for preset: " + index);
    }



    void LoadPresetFromPlayData(int index)
    {
        for (int s = 0; s < 5; s++)
        {
            int id = PlayData.selectedDeckUnitIds[index, s];

            if (id != 0 && unitModelMap.ContainsKey(id))
                presets[index].units[s] = unitModelMap[id];
            else
                presets[index].units[s] = null;
        }
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

    async UniTaskVoid OnCompleteClicked()
    {
        PlayData.currentSelectedPreset = activePresetIndex;

        if (!isEditing)
            return;

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

        Debug.Log("저장: " + string.Join(",", PlayData.selectedDeckUnitIds));

        await DatabaseManager.Instance.SavePresetFromPlayDataAsync(activePresetIndex);

        ExitEditMode();
        if (stageDeck != null)
        {

            if (stageDeck.isActiveAndEnabled)
            {
                stageDeck.Refresh();
            }
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
