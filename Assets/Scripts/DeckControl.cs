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

    [Header("Loading")]
    public GameObject loadingPanel;

    [Header("Prefabs")]
    public UnitCard cardPrefab;

    [Header("External References")]
    public StageDeck stageDeck;
    public BattleUnitManager battleUnitManager;
    public WindowManager windowManager;

    [Header("Unlock Alert Panel")]
    public GameObject unlockAlertPanel;
    public TMPro.TextMeshProUGUI alertMessageText;
    public TMPro.TextMeshProUGUI alertCostText;
    public Button alertBuyButton;
    public Button alertCancelButton;

    private const long SLOT_2_GOLD_COST = 10000;
    private const int SLOT_3_DIAMOND_COST = 100;
    private const int SLOT_4_DIAMOND_COST = 500;

    private int pendingUnlockSlot = -1;
    private int pendingUnlockPreset = -1;

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
        InitializeUnlockAlert();

        HideAllSlots();

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
    }

    void InitializeSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].slotIndex = i;
            slots[i].SetDeckControl(this);
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

    void InitializeUnlockAlert()
    {
        if (unlockAlertPanel != null)
        {
            unlockAlertPanel.SetActive(false);
        }

        if (alertBuyButton != null)
        {
            alertBuyButton.onClick.RemoveAllListeners();
            alertBuyButton.onClick.AddListener(OnAlertBuyClicked);
        }

        if (alertCancelButton != null)
        {
            alertCancelButton.onClick.RemoveAllListeners();
            alertCancelButton.onClick.AddListener(OnAlertCancelClicked);
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

    void HideAllSlots()
    {
        foreach (var slot in slots)
        {
            slot.gameObject.SetActive(false);
        }
    }

    void ShowAllSlots()
    {
        foreach (var slot in slots)
        {
            slot.gameObject.SetActive(true);
        }
    }

    private bool isInitialized = false;

    async void Start()
    {
        await DatabaseManager.Instance.WaitForInitializationAsync();
        await InitializeData();

        UpdateAllSlotLockStates();
        await CreateUnitCards();

        await LoadAndSetupPresets();

        ApplyPresetToSelectedUnitIds();

        UpdateAllUI();
        ShowAllSlots();

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        unitInfoUI.SetUnitManager(battleUnitManager);
        unitInfoUI.SetDeckControl(this);
        isInitialized = true;
    }

    void OnEnable()
    {
        if (!isInitialized || presets == null)
            return;

        QuickRefresh();
    }

    void OnDisable()
    {
        if (isEditing)
        {
            isEditing = false;
            if (highlightOverlay != null)
                highlightOverlay.SetActive(false);
        }

        if (detailedPanel != null)
            detailedPanel.SetActive(false);
    }

    void QuickRefresh()
    {
        Debug.Log("[DeckControl QuickRefresh] 로컬 데이터로 빠른 갱신");

        LoadPresets();
        LoadPreset(activePresetIndex);

        ApplyPresetToSelectedUnitIds();
        UpdateAllUI();
        UpdateAllSlotLockStates();
    }

    async UniTask RefreshFromFirebase()
    {
        Debug.Log("[DeckControl RefreshFromFirebase] Firebase 데이터 갱신 시작");

        await DatabaseManager.Instance.WaitForInitializationAsync();
        await DatabaseManager.Instance.LoadUserDataAsync();
        await CreateNewUnitCards();

        DatabaseManager.Instance.SyncPresetsToPlayData();
        LoadPresets();

        Debug.Log($"[DeckControl RefreshFromFirebase] 프리셋 {activePresetIndex} 로드 완료");

        LoadPreset(activePresetIndex);
        ApplyPresetToSelectedUnitIds();
        UpdateAllUI();
        UpdateAllSlotLockStates();
    }

    async UniTask CreateNewUnitCards()
    {
        var ownedCharacters = DatabaseManager.Instance.GetAllCharacters();
        List<UniTask> loadTasks = new();

        foreach (var character in ownedCharacters)
        {
            int unitId = int.Parse(character.id);
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

        await UniTask.WaitUntil(() => unitModelMap.Count > 0);

        LoadPresets();

        bool needsInitialization = false;
        for (int i = 0; i < 5; i++)
        {
            if (PlayData.IsPresetEmptyOnUnlockedSlots(i))
            {
                needsInitialization = true;
                break;
            }
        }

        if (needsInitialization)
        {
            Debug.Log("[DeckControl] 비어있는 프리셋 감지 → 자동 편성 시작");

            // 모든 프리셋의 잠금 해제된 슬롯 채우기
            for (int i = 0; i < 5; i++)
            {
                if (PlayData.IsPresetEmptyOnUnlockedSlots(i))
                {
                    await AutoFillPresetIfEmpty(i);
                }
            }

            LoadPresets();
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

        await UniTask.WhenAll(loadTasks);
    }

    private async UniTask AutoFillPresetIfEmpty(int presetIndex)
    {
        List<int> emptySlots = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            if (!IsSlotUnlocked(presetIndex, i))
                continue;

            if (PlayData.selectedDeckUnitIds[presetIndex, i] == 0)
            {
                emptySlots.Add(i);
            }
        }

        if (emptySlots.Count == 0)
        {
            Debug.Log($"[DeckControl] 프리셋 {presetIndex} 이미 유닛이 배치되어 있음 - 스킵");
            return;
        }

        Debug.Log($"[DeckControl] 프리셋 {presetIndex} 비어있는 슬롯 {emptySlots.Count}개 발견 - 자동 편성 시작");

        var owned = DatabaseManager.Instance.GetAllCharacters();
        if (owned.Count == 0)
        {
            Debug.LogError("[DeckControl] 보유한 캐릭터가 없음!");
            return;
        }

        if (unitModelMap.Count == 0)
        {
            Debug.LogError("[DeckControl] unitModelMap이 비어있습니다!");
            return;
        }

        // 이미 배치된 유닛 찾기
        HashSet<int> assignedUnits = new HashSet<int>();
        for (int i = 0; i < 5; i++)
        {
            int unitId = PlayData.selectedDeckUnitIds[presetIndex, i];
            if (unitId != 0)
            {
                assignedUnits.Add(unitId);
            }
        }

        // 사용 가능한 유닛 목록
        List<DeckUnitModel> availableUnits = new List<DeckUnitModel>();
        foreach (var character in owned)
        {
            int unitId = int.Parse(character.id);
            if (unitModelMap.ContainsKey(unitId) && !assignedUnits.Contains(unitId))
            {
                availableUnits.Add(unitModelMap[unitId]);
            }
        }

        if (availableUnits.Count == 0)
        {
            Debug.LogWarning("[DeckControl] 배치할 수 있는 유닛이 없습니다!");
            return;
        }

        // 랜덤 섞기
        for (int i = availableUnits.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            var temp = availableUnits[i];
            availableUnits[i] = availableUnits[randomIndex];
            availableUnits[randomIndex] = temp;
        }

        int unitIndex = 0;
        int filledCount = 0;

        DeckPreset preset = presets[presetIndex];

        // 비어있는 슬롯에만 유닛 배치
        foreach (int slotIndex in emptySlots)
        {
            if (unitIndex >= availableUnits.Count)
            {
                Debug.LogWarning($"[DeckControl] 프리셋 {presetIndex} - 배치할 유닛 부족");
                break;
            }

            DeckUnitModel model = availableUnits[unitIndex];

            if (string.IsNullOrEmpty(model.iconAddress))
            {
                Debug.LogWarning($"[DeckControl] {model.unitId} 아이콘 주소 누락 - 보정 시도");
                model.FixMissingAddress();
            }

            preset.units[slotIndex] = model;
            PlayData.selectedDeckUnitIds[presetIndex, slotIndex] = model.unitId;
            PlayData.selectedDeckUnitIconAddresses[presetIndex, slotIndex] = model.iconAddress;

            Debug.Log($"[DeckControl] 프리셋 {presetIndex} 슬롯 {slotIndex} - 유닛 {model.unitId} ({model.unitName}) 배치");

            unitIndex++;
            filledCount++;
        }

        if (filledCount > 0)
        {
            await DatabaseManager.Instance.SavePresetFromPlayDataAsync(presetIndex);
            Debug.Log($"[DeckControl] 프리셋 {presetIndex} 자동 편성 완료 - {filledCount}개 유닛 배치 완료");
        }
    }

    public async void OnClickPresetButton(int index)
    {
        Debug.Log($"[DeckControl OnClickPresetButton] 프리셋 {index} 선택");

        activePresetIndex = index;
        PlayData.currentSelectedPreset = index;

        await DatabaseManager.Instance.SetActivePresetAsync(index);

        bool isEmpty = PlayData.IsPresetEmptyOnUnlockedSlots(index);
        if (isEmpty)
        {
            Debug.LogWarning($"[DeckControl] 프리셋 {index} 비어있음 - 자동 편성 실행");
            await AutoFillPresetIfEmpty(index);
            LoadPresets();
        }

        LoadPreset(index);
        UpdatePresetButtonsStates();
        UpdateAllSlotLockStates();

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
            bool isSlotLocked = !IsSlotUnlocked(index, i);

            DeckUnitModel model = isSlotLocked ? null : preset.units[i];
            slots[i].SetCommittedExternal(model);

            if (isSlotLocked)
            {
                slots[i].SetInteractable(true); 
            }
            else
            {
                slots[i].SetInteractable(false); 
            }

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
        if (presets == null)
        {
            Debug.LogWarning("[DeckControl] LoadPresets: presets가 null - 초기화 대기 중");
            return;
        }

        for (int p = 0; p < 5; p++)
        {
            DeckPreset preset = presets[p];

            for (int i = 0; i < 5; i++)
            {
                if (!IsSlotUnlocked(p, i))
                {
                    preset.units[i] = null;
                    continue;
                }

                int id = PlayData.selectedDeckUnitIds[p, i];

                if (id != 0)
                {
                    if (unitModelMap.ContainsKey(id))
                    {
                        preset.units[i] = unitModelMap[id];
                    }
                    else
                    {
                        Debug.LogWarning($"[LoadPresets] 유닛 {id}가 unitModelMap에 없음 - 프리셋 {p} 슬롯 {i}");
                        preset.units[i] = null;
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
        if (!isEditing)
            return;

        isEditing = false;
        highlightOverlay.SetActive(false);
        LoadPreset(activePresetIndex);

        UpdateAllUI();
    }

    public void OnSlotClicked(DeckSlot slot)
    {
        if (!isEditing) return;
        if (slot.IsLocked) return;

        if (slot.HasPending)
        {
            var unit = slot.GetPending();
            slot.ClearPending();
            NotifyUnitCleared(unit);
            UpdateCompleteButton();
            return;
        }

        if (slot.HasCommitted)
        {
            var unit = slot.GetCommitted();
            slot.SetCommittedExternal(null);
            NotifyUnitCleared(unit);
            UpdateCompleteButton();
        }
    }


    public void OnLockedSlotClicked(int slotIndex, int presetIndex)
    {
        pendingUnlockSlot = slotIndex;
        pendingUnlockPreset = presetIndex;

        string costText = "";
        bool canAfford = false;

        switch (slotIndex)
        {
            case 2:
                costText = $"골드 {SLOT_2_GOLD_COST:N0}";
                canAfford = PlayData.HasEnoughGold(SLOT_2_GOLD_COST);
                break;
            case 3:
                costText = $"다이아 {SLOT_3_DIAMOND_COST}";
                canAfford = PlayData.HasEnoughDiamond(SLOT_3_DIAMOND_COST);
                break;
            case 4:
                costText = $"다이아 {SLOT_4_DIAMOND_COST}";
                canAfford = PlayData.HasEnoughDiamond(SLOT_4_DIAMOND_COST);
                break;
        }

        if (unlockAlertPanel != null)
        {
            unlockAlertPanel.SetActive(true);
        }

        if (alertMessageText != null)
        {
            alertMessageText.text = "이 슬롯을 해제하시겠습니까?";
        }

        if (alertCostText != null)
        {
            alertCostText.text = costText;
        }

        if (alertBuyButton != null)
        {
            alertBuyButton.interactable = canAfford;
        }
    }

    private async void OnAlertBuyClicked()
    {
        if (pendingUnlockSlot < 0 || pendingUnlockPreset < 0)
            return;

        if (unlockAlertPanel != null)
        {
            unlockAlertPanel.SetActive(false);
        }

        bool success = await TryUnlockSlot(pendingUnlockSlot, pendingUnlockPreset);

        if (success)
        {
            Debug.Log($"[DeckControl] 슬롯 {pendingUnlockSlot} 해제 완료 (모든 프리셋)");
        }

        pendingUnlockSlot = -1;
        pendingUnlockPreset = -1;
    }

    private void OnAlertCancelClicked()
    {
        if (unlockAlertPanel != null)
        {
            unlockAlertPanel.SetActive(false);
        }

        pendingUnlockSlot = -1;
        pendingUnlockPreset = -1;
    }

    private async UniTask<bool> TryUnlockSlot(int slotIndex, int presetIndex)
    {
        bool success = false;

        switch (slotIndex)
        {
            case 2:
                if (PlayData.HasEnoughGold(SLOT_2_GOLD_COST))
                {
                    await DatabaseManager.Instance.AddGoldAsync(-SLOT_2_GOLD_COST);
                    success = true;
                }
                break;
            case 3:
                if (PlayData.HasEnoughDiamond(SLOT_3_DIAMOND_COST))
                {
                    await DatabaseManager.Instance.AddDiamondAsync(-SLOT_3_DIAMOND_COST);
                    success = true;
                }
                break;
            case 4:
                if (PlayData.HasEnoughDiamond(SLOT_4_DIAMOND_COST))
                {
                    await DatabaseManager.Instance.AddDiamondAsync(-SLOT_4_DIAMOND_COST);
                    success = true;
                }
                break;
        }

        if (success)
        {
            if (DatabaseManager.Instance.CurrentUser.presetSlotUnlocks == null)
            {
                DatabaseManager.Instance.CurrentUser.presetSlotUnlocks = new PresetSlotUnlockData();
            }

            for (int p = 0; p < 5; p++)
            {
                DatabaseManager.Instance.CurrentUser.presetSlotUnlocks.UnlockSlot(p, slotIndex);
            }

            await DatabaseManager.Instance.SaveSlotUnlocksAsync();

            for (int p = 0; p < 5; p++)
            {
                await FillUnlockedSlotWithRandomUnit(p, slotIndex);
            }

            UpdateAllSlotLockStates();

            DatabaseManager.Instance.SyncPresetsToPlayData();
            LoadPresets();
            LoadPreset(activePresetIndex);

            if (stageDeck != null && stageDeck.isActiveAndEnabled)
            {
                stageDeck.Refresh();
            }
        }

        return success;
    }

    private async UniTask FillUnlockedSlotWithRandomUnit(int presetIdx, int slotIdx)
    {
        HashSet<int> assignedUnits = new HashSet<int>();
        for (int i = 0; i < 5; i++)
        {
            int unitId = PlayData.selectedDeckUnitIds[presetIdx, i];
            if (unitId != 0)
            {
                assignedUnits.Add(unitId);
            }
        }

        List<DeckUnitModel> availableUnits = new List<DeckUnitModel>();

        foreach (var kvp in unitModelMap)
        {
            if (!assignedUnits.Contains(kvp.Key))
            {
                availableUnits.Add(kvp.Value);
            }
        }

        if (availableUnits.Count == 0)
        {
            Debug.LogWarning($"[DeckControl] 프리셋 {presetIdx} - 배치할 수 있는 유닛이 없습니다!");
            return;
        }

        int randomIndex = Random.Range(0, availableUnits.Count);
        DeckUnitModel selectedUnit = availableUnits[randomIndex];

        presets[presetIdx].units[slotIdx] = selectedUnit;
        PlayData.selectedDeckUnitIds[presetIdx, slotIdx] = selectedUnit.unitId;
        PlayData.selectedDeckUnitIconAddresses[presetIdx, slotIdx] = selectedUnit.iconAddress;

        await DatabaseManager.Instance.SavePresetFromPlayDataAsync(presetIdx);

        Debug.Log($"[DeckControl] 프리셋 {presetIdx} 슬롯 {slotIdx}에 유닛 {selectedUnit.unitId} 자동 배치 완료");
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
            if (slot.IsLocked) continue;

            if (slot.GetCommitted()?.unitId == model.unitId) return;
            if (slot.GetPending()?.unitId == model.unitId) return;
        }

        foreach (var slot in slots)
        {
            if (slot.IsLocked) continue;

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

        bool hasEmptySlot = false;
        foreach (var slot in slots)
        {
            if (slot.IsLocked)
                continue;

            if (!slot.HasPending && !slot.HasCommitted)
            {
                hasEmptySlot = true;
                break;
            }
        }

        if (hasEmptySlot)
        {
            FillEmptySlotsWithRandomUnits();
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsLocked)
            {
                presets[activePresetIndex].units[i] = null;
                PlayData.selectedDeckUnitIds[activePresetIndex, i] = 0;
                PlayData.selectedDeckUnitIconAddresses[activePresetIndex, i] = "";
                continue;
            }

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
                // 빈 슬롯이 있으면 경고
                Debug.LogWarning($"[DeckControl] 슬롯 {i}에 유닛이 없습니다!");
                PlayData.selectedDeckUnitIds[activePresetIndex, i] = 0;
                PlayData.selectedDeckUnitIconAddresses[activePresetIndex, i] = "";
            }
        }

        bool saveSuccess = await DatabaseManager.Instance.SavePresetFromPlayDataAsync(activePresetIndex);

        if (saveSuccess)
        {
            Debug.Log($"[DeckControl OnCompleteClicked] 프리셋 {activePresetIndex} 저장 완료");
        }
        else
        {
            Debug.LogError($"[DeckControl OnCompleteClicked] 프리셋 {activePresetIndex} 저장 실패!");
        }

        ExitEditMode();
        ApplyPresetToSelectedUnitIds();

        if (stageDeck != null && stageDeck.isActiveAndEnabled)
        {
            stageDeck.Refresh();
        }
    }

    void FillEmptySlotsWithRandomUnits()
    {
        HashSet<int> assignedUnitIds = new HashSet<int>();

        foreach (var slot in slots)
        {
            if (slot.IsLocked)
                continue;

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

        foreach (var slot in slots)
        {
            if (slot.IsLocked)
                continue;

            if (!slot.HasPending && !slot.HasCommitted && availableUnits.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableUnits.Count);
                DeckUnitModel randomUnit = availableUnits[randomIndex];

                slot.SetPending(randomUnit);
                NotifyUnitAssigned(randomUnit);

                availableUnits.RemoveAt(randomIndex);
            }
        }
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
            if (slots[i].IsLocked)
                continue;

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

    void UpdateAllSlotLockStates()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].UpdateLockState(activePresetIndex);
        }
    }

    private bool IsSlotUnlocked(int presetIndex, int slotIndex)
    {
        if (slotIndex < 2)
            return true;

        var userData = DatabaseManager.Instance.CurrentUser;
        if (userData?.presetSlotUnlocks == null)
            return false;

        return userData.presetSlotUnlocks.IsSlotUnlocked(presetIndex, slotIndex);
    }

    public int GetActivePresetIndex()
    {
        return activePresetIndex;
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
        if (!gameObject.activeInHierarchy)
            return;

        if (!isEditing)
            return;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();

            if (EventSystem.current != null)
            {
                PointerEventData pointer = new PointerEventData(EventSystem.current);
                pointer.position = touchPos;

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

                if (!clickedEditableUI && results.Count > 0)
                {
                    ExitEditMode();
                }
            }
        }
    }

    public void ApplyPresetToSelectedUnitIds()
    {
        PlayData.selectedUnitIds.Clear();

        for (int i = 0; i < 5; i++)
        {
            if (!IsSlotUnlocked(activePresetIndex, i))
                continue;

            int unitId = PlayData.selectedDeckUnitIds[activePresetIndex, i];

            if (unitId != 0)
            {
                PlayData.selectedUnitIds.Add(unitId);
            }
        }

        if (PlayData.selectedUnitIds.Count == 0)
        {
            Debug.LogWarning($"[DeckControl] 프리셋 {activePresetIndex} - selectedUnitIds가 비어있습니다. 잠금 해제된 슬롯에 유닛을 배치하세요.");
        }
        else
        {
            Debug.Log($"[DeckControl] 프리셋 {activePresetIndex} - {PlayData.selectedUnitIds.Count}개 유닛 활성화");
        }
    }

    public DeckUnitModel GetDeckUnitModelFromPreset(int preset, int slot)
    {
        if (preset < 0 || preset >= presets.Length)
            return null;

        return presets[preset].units[slot];
    }

    public bool CanEquipSlot(int presetIndex, int slotIndex)
    {
        if (slotIndex < 2)
            return true;

        var user = DatabaseManager.Instance.CurrentUser;
        if (user?.presetSlotUnlocks == null)
            return false;

        return user.presetSlotUnlocks.IsSlotUnlocked(presetIndex, slotIndex);
    }
}
