using Cysharp.Threading.Tasks;
using GameData;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DeckPreset
{
    public DeckUnitModel[] units;
}

public class DeckControl : MonoBehaviour
{
    [Header("UI References")]
    public GameObject highlightOverlay;
    public List<DeckSlot> slots;
    public Transform unitListParent;
    public Button completeButton;
    public Button editButton;
    public GameObject detailedPanel;
    public List<Button> presetButtons;
    public UnitInfoUI unitInfoUI;

    [Header("Prefabs")]
    public UnitCard cardPrefab;

    [Header("External References")]
    public StageDeck stageDeck;
    public BattleUnitManager battleUnitManager; 

    // Data
    private DataTable_Unit unitTable;
    private List<UnitCard> unitCards = new();
    private Dictionary<int, DeckUnitModel> unitModelMap = new();
    private DeckPreset[] presets;
    private bool isEditing = false;
    private int activePresetIndex = 0;

    void Awake()
    {
        InitializeSlots();
        InitializeButtons();
        InitializePresets();
    }

    void InitializeSlots()
    {
        foreach (var slot in slots)
        {
            slot.SetDeckControl(this);
        }
    }

    void InitializeButtons()
    {
        completeButton.onClick.AddListener(() => OnCompleteClicked().Forget());
        editButton.onClick.AddListener(OnEditButtonClicked);

        for (int i = 0; i < presetButtons.Count; i++)
        {
            int idx = i;
            presetButtons[i].onClick.AddListener(() => OnClickPresetButton(idx));
            presetButtons[i].onClick.AddListener(ExitEditModeIfEditing);
        }
    }

    void InitializePresets()
    {
        presets = new DeckPreset[5];
        for (int i = 0; i < presets.Length; i++)
        {
            presets[i] = new DeckPreset();
            presets[i].units = new DeckUnitModel[5];
        }
    }

    async void Start()
    {
        await InitializeData();

        //임시 유닛 추가 (인게임 유닛 편성 데이터 전달 테스트용 )
        if (!DatabaseManager.Instance.CurrentUser.characters.ContainsKey("11119"))
        {
            var tempChar = new OwnedCharacter
            {
                id = "11119",
                level = 1,
                star = 1,
                exp = 0,
                awakening = 0
            };

            DatabaseManager.Instance.CurrentUser.characters["11119"] = tempChar;
            Debug.Log("테스트 유닛 11119 임시 추가됨");
        }
        
        await CreateUnitCards();
        await LoadAndSetupPresets();
        UpdateAllUI();
        unitInfoUI.SetUnitManager(battleUnitManager);
        PlayData.AddEnhanceStone(999999); //임시 강화석

    }

    async UniTask InitializeData()
    {
        await DatabaseManager.Instance.WaitForInitializationAsync();

        if (DatabaseManager.Instance.CurrentUser == null)
        {
            await DatabaseManager.Instance.LoadUserDataAsync();
        }

        activePresetIndex = PlayData.currentSelectedPreset;

        unitTable = new DataTable_Unit();
        await unitTable.LoadAsync("UnitTable");

        highlightOverlay.SetActive(false);
        detailedPanel.SetActive(false);
    }

    async UniTask LoadAndSetupPresets()
    {
        DatabaseManager.Instance.SyncPresetsToPlayData();
        LoadPresets();
        await AutoFillPresetIfEmpty(activePresetIndex);
        LoadPreset(activePresetIndex);
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

            var model = new DeckUnitModel
            {
                unitId = unitId,
                unitName = data.StringName,
                iconAddress = data.UNIT_ICON,
                rawData = data
                
            };

            var loadTask = Addressables.LoadAssetAsync<Sprite>(model.iconAddress).Task.AsUniTask()
                .ContinueWith(result =>
                {
                    model.icon = result;
                    var card = Instantiate(cardPrefab, unitListParent);
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

    private async UniTask AutoFillPresetIfEmpty(int presetIndex)
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
            {
                continue;
            }

            DeckUnitModel model = unitModelMap[unitId];
            preset.units[i] = model;

            PlayData.selectedDeckUnitIds[presetIndex, i] = model.unitId;
            PlayData.selectedDeckUnitIconAddresses[presetIndex, i] = model.iconAddress;
        }

        await DatabaseManager.Instance.SavePresetFromPlayDataAsync(presetIndex);
        Debug.Log($"Preset {presetIndex} 자동 편성 완료");
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
            slots[i].SetInteractable(false);

            if (model != null)
            {
                NotifyUnitAssigned(model);
            }
        }

        highlightOverlay.SetActive(false);
        UpdateCompleteButton();
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

    void OnEditButtonClicked()
    {
        if (!isEditing)
        {
            EnterEditMode();
        }
    }

    void EnterEditMode()
    {
        isEditing = true;
        highlightOverlay.SetActive(true);

        foreach (var slot in slots)
        {
            slot.BeginEdit();
            slot.SetInteractable(true);
        }

        UpdateAllUI();
    }

    void ExitEditMode()
    {
        isEditing = false;
        highlightOverlay.SetActive(false);
        LoadPreset(activePresetIndex);

        foreach (var slot in slots)
        {
            slot.SetInteractable(false);
        }

        UpdateAllUI();
    }

    public void OnSlotClicked(DeckSlot slot)
    {
        if (!isEditing) return;

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

    void OnUnitCardClicked(DeckUnitModel model)
    {
        if (isEditing)
        {
            HandleEditModeCardClick(model);
        }
        else
        {
            HandleViewModeCardClick(model);
        }
    }

    void HandleEditModeCardClick(DeckUnitModel model)
    {
        foreach (var slot in slots)
        {
            var pending = slot.GetPending();
            if (pending != null && pending.unitId == model.unitId)
            {
                slot.ClearPending();
                NotifyUnitCleared(model);
                UpdateCompleteButton();
                Debug.Log($"슬롯에서 제거: {model.unitName}");
                return;
            }

            var committed = slot.GetCommitted();
            if (committed != null && committed.unitId == model.unitId)
            {
                slot.SetPending(null);
                slot.CommitPending();
                NotifyUnitCleared(model);
                UpdateCompleteButton();
                Debug.Log($"슬롯에서 제거: {model.unitName}");
                return;
            }
        }

        foreach (var slot in slots)
        {
            if (!slot.HasCommitted && !slot.HasPending)
            {
                slot.SetPending(model);
                NotifyUnitAssigned(model);
                UpdateCompleteButton();
                Debug.Log($"빈 슬롯에 배치: {model.unitName}");
                return;
            }
        }

        Debug.Log("빈 슬롯이 없음.");
    }

    void HandleViewModeCardClick(DeckUnitModel model)
    {
        ExitEditModeIfEditing();
        detailedPanel.SetActive(true);
        unitInfoUI.SetUnit(model.unitId);
        Debug.Log("선택된 유닛: " + model.unitId);
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

    async UniTaskVoid OnCompleteClicked()
    {
        if (!isEditing) return;

        bool hasRandomFill = FillEmptySlotsWithRandomUnits();

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].HasPending)
            {
                slots[i].CommitPending();
            }

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

        //if (hasRandomFill && notification != null)
        //{
        //    notification.Show("빈 슬롯이 랜덤으로 채워졌습니다!");
        //}

        ExitEditMode();

        if (stageDeck != null && stageDeck.isActiveAndEnabled)
        {
            stageDeck.Refresh();
        }

        ApplyPresetToSelectedUnitIds();
    }

    bool FillEmptySlotsWithRandomUnits()
    {
        HashSet<int> assignedUnitIds = new HashSet<int>();

        foreach (var slot in slots)
        {
            var pending = slot.GetPending();
            if (pending != null)
            {
                assignedUnitIds.Add(pending.unitId);
            }
            else
            {
                var committed = slot.GetCommitted();
                if (committed != null)
                {
                    assignedUnitIds.Add(committed.unitId);
                }
            }
        }

        List<DeckUnitModel> availableUnits = new List<DeckUnitModel>();

        foreach (var kv in unitModelMap)
        {
            if (!assignedUnitIds.Contains(kv.Key))
            {
                availableUnits.Add(kv.Value);
            }
        }

        bool hasRandomFill = false;

        foreach (var slot in slots)
        {
            if (!slot.HasPending && !slot.HasCommitted && availableUnits.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableUnits.Count);
                DeckUnitModel randomUnit = availableUnits[randomIndex];

                slot.SetPending(randomUnit);
                NotifyUnitAssigned(randomUnit);

                availableUnits.RemoveAt(randomIndex);
                hasRandomFill = true;

                Debug.Log($"빈 슬롯에 랜덤 {randomUnit.unitName}");
            }
        }

        return hasRandomFill;
    }

    void UpdateCompleteButton()
    {
        if (!isEditing)
        {
            completeButton.interactable = false;
            completeButton.gameObject.SetActive(false);
            return;
        }

        completeButton.gameObject.SetActive(true);

        bool hasChanges = false;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var committed = slot.GetCommitted();
            var pending = slot.GetPending();

            if (committed != pending)
            {
                hasChanges = true;
                break;
            }
        }

        completeButton.interactable = hasChanges;
    }

    void UpdateEditButton()
    {
        editButton.gameObject.SetActive(!isEditing);
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

    void UpdateAllUI()
    {
        UpdateCompleteButton();
        UpdateEditButton();
        UpdatePresetButtonsStates();
    }

    void ExitEditModeIfEditing()
    {
        if (isEditing)
        {
            ExitEditMode();
        }
    }

    void Update()
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
        foreach (var r in results)
        {
            if (r.gameObject.GetComponent<DeckSlot>() != null ||
                r.gameObject.GetComponent<UnitCard>() != null ||
                presetButtons.Exists(b => r.gameObject.transform.IsChildOf(b.transform)) ||
                r.gameObject.transform.IsChildOf(completeButton.transform) ||
                r.gameObject.transform.IsChildOf(editButton.transform))
            {
                clickedEditableUI = true;
                break;
            }
        }

        if (clickedEditableUI)
        {
            return;
        }

        bool clickedAnyButton = false;
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
    private void ApplyPresetToSelectedUnitIds()
    {
        PlayData.selectedUnitIds.Clear(); //초기화

        for (int i = 0; i < 5; i++)
        {
            int unitId = PlayData.selectedDeckUnitIds[activePresetIndex, i];

            if (unitId != 0)
            {
                PlayData.selectedUnitIds.Add(unitId);
            }
        }

        Debug.Log("PlayData.selectedUnitIds updated " + string.Join(", ", PlayData.selectedUnitIds));
    }
}
