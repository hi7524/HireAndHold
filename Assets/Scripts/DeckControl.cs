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

    private bool isInitialized = false;

    async void Start()
    {
        await InitializeData();
        await CreateUnitCards();

        await LoadAndSetupPresets();

        ApplyPresetToSelectedUnitIds();

        UpdateAllUI();
        unitInfoUI.SetUnitManager(battleUnitManager);
        unitInfoUI.SetDeckControl(this);
        isInitialized = true;
    }


    async void OnEnable()
    {
        if (!isInitialized)
            return;
        await RefreshFromFirebase();
    }

    async UniTask RefreshFromFirebase()
    {
        await DatabaseManager.Instance.WaitForInitializationAsync();

        await DatabaseManager.Instance.LoadUserDataAsync();

        await CreateNewUnitCards();

  
        DatabaseManager.Instance.SyncPresetsToPlayData();

        LoadPresets();

        if (PlayData.IsPresetCompletelyEmpty(activePresetIndex))
        {
            Debug.LogWarning("[DeckControl OnEnable] 활성 프리셋 비어있음 → 자동 편성");
            await AutoFillPresetIfEmpty(activePresetIndex);
        }

        LoadPreset(activePresetIndex);

        ApplyPresetToSelectedUnitIds();

        UpdateAllUI();
    }

    async UniTask CreateNewUnitCards()
    {
        var ownedCharacters = DatabaseManager.Instance.GetAllCharacters();
        List<UniTask> loadTasks = new();

        foreach (var character in ownedCharacters)
        {
            int unitId = int.Parse(character.id);

            // 이미 카드가 있으면 스킵
            if (unitModelMap.ContainsKey(unitId))
                continue;

            UnitData data = unitTable.Get(unitId);
            if (data == null)
                continue;

            int enforceLevel = character.enforceLevel;

            var model = new DeckUnitModel
            {
                unitId = unitId,
                unitName = data.StringName,
                iconAddress = data.UNIT_ICON,
                rawData = data,
                enforceLevel = enforceLevel
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

        if (loadTasks.Count > 0)
        {
            await UniTask.WhenAll(loadTasks);
        }
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


        bool activePresetEmpty = PlayData.IsPresetCompletelyEmpty(activePresetIndex);

        if (activePresetEmpty)
        {
            Debug.LogWarning($"[DeckControl] 활성 프리셋({activePresetIndex})이 비어있음 → 자동 편성 시작");
            await AutoFillPresetIfEmpty(activePresetIndex);
        }
        else
        {
        }

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
                continue;

            // DatabaseManager 캐릭터 데이터 → 강화 레벨 가져오기
            int enforceLevel = character.enforceLevel;

            var model = new DeckUnitModel
            {
                unitId = unitId,
                unitName = data.StringName,
                iconAddress = data.UNIT_ICON,
                rawData = data,

                enforceLevel = enforceLevel 
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
                    unitModelMap[unitId] = model;  // 강화 정보 포함된 모델 저장
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
            Debug.LogError("[DeckControl] 보유한 캐릭터가 없음!");
            return;
        }

        await UniTask.WaitUntil(() => unitModelMap.Count > 0);

        int filledCount = 0;
        for (int i = 0; i < 5 && i < owned.Count; i++)
        {
            int unitId = int.Parse(owned[i].id);

            if (!unitModelMap.ContainsKey(unitId))
            {
                Debug.LogWarning($"[DeckControl] UnitModelMap에 {unitId} 없음 - 스킵");
                continue;
            }

            DeckUnitModel model = unitModelMap[unitId];

 
            if (string.IsNullOrEmpty(model.iconAddress))
            {
                Debug.LogWarning($"[DeckControl] {unitId} 아이콘 주소 누락 - 보정 시도");
                model.FixMissingAddress();
            }

            preset.units[i] = model;
            PlayData.selectedDeckUnitIds[presetIndex, i] = model.unitId;
            PlayData.selectedDeckUnitIconAddresses[presetIndex, i] = model.iconAddress;

            filledCount++;
        }

        if (filledCount > 0)
        {

            bool saved = await DatabaseManager.Instance.SavePresetFromPlayDataAsync(presetIndex);
        }
        else
        {
            Debug.LogError("[DeckControl] 자동 편성 실패 - 유효한 유닛 없음");
        }
    }

    public async void OnClickPresetButton(int index)
    {
        activePresetIndex = index;
        PlayData.currentSelectedPreset = index;

        await DatabaseManager.Instance.SetActivePresetAsync(index);

        LoadPreset(index);
        UpdatePresetButtonsStates();

        ApplyPresetToSelectedUnitIds();

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

    public void LoadPresets()
    {
        for (int p = 0; p < 5; p++)
        {
            DeckPreset preset = presets[p];

            for (int i = 0; i < 5; i++)
            {
                int id = PlayData.selectedDeckUnitIds[p, i];

                if (id != 0)
                {
                    if (unitModelMap.ContainsKey(id))
                        preset.units[i] = unitModelMap[id];
                    else
                    {
         
                        preset.units[i] = new DeckUnitModel
                        {
                            unitId = id,
                            iconAddress = PlayData.selectedDeckUnitIconAddresses[p, i]
                        };
                    }
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
                return;
            }

            var committed = slot.GetCommitted();
            if (committed != null && committed.unitId == model.unitId)
            {
                slot.SetPending(null);
                slot.CommitPending();
                NotifyUnitCleared(model);
                UpdateCompleteButton();
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
                return;
            }
        }
    }

    void HandleViewModeCardClick(DeckUnitModel model)
    {
        ExitEditModeIfEditing();
        detailedPanel.SetActive(true);
        unitInfoUI.SetUnit(model.unitId);
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

        ExitEditMode();


        ApplyPresetToSelectedUnitIds();

        if (stageDeck != null && stageDeck.isActiveAndEnabled)
        {
            stageDeck.Refresh();
        }
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
    public void ApplyPresetToSelectedUnitIds()
    {
        PlayData.selectedUnitIds.Clear();

        for (int i = 0; i < 5; i++)
        {
            int unitId = PlayData.selectedDeckUnitIds[activePresetIndex, i];

            if (unitId != 0)
            {
                PlayData.selectedUnitIds.Add(unitId);
            }
        }
        if (PlayData.selectedUnitIds.Count == 0)
        {
            Debug.LogError("[DeckControl] CRITICAL: selectedUnitIds가 비어있습니다!");
        }
    }


    public DeckUnitModel GetDeckUnitModelFromPreset(int preset, int slot)
    {
        if (preset < 0 || preset >= presets.Length)
            return null;

        return presets[preset].units[slot];
    }


}

