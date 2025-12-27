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

    /// <summary>
    /// 모든 슬롯 숨기기
    /// </summary>
    void HideAllSlots()
    {
        foreach (var slot in slots)
        {
            slot.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 모든 슬롯 표시
    /// </summary>
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
        if (!isInitialized)
            return;
        RefreshFromFirebase().Forget();
    }

    async UniTask RefreshFromFirebase()
    {
        await DatabaseManager.Instance.WaitForInitializationAsync();
        await DatabaseManager.Instance.LoadUserDataAsync();
        await CreateNewUnitCards();

        DatabaseManager.Instance.SyncPresetsToPlayData();
        LoadPresets();

        // OnEnable에서는 자동 편성하지 않음 - 저장된 프리셋 그대로 유지
        // (자동 편성은 Start()의 LoadAndSetupPresets()에서만 수행)
        Debug.Log($"[DeckControl RefreshFromFirebase] 프리셋 {activePresetIndex} 로드 완료 - 슬롯0: {PlayData.selectedDeckUnitIds[activePresetIndex, 0]}, 슬롯1: {PlayData.selectedDeckUnitIds[activePresetIndex, 1]}");

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
        LoadPresets();

        bool isFirstTime = true;
        for (int i = 0; i < 5; i++)
        {
            if (!PlayData.IsPresetEmptyOnUnlockedSlots(i))
            {
                isFirstTime = false;
                break;
            }
        }


        if (isFirstTime)
        {
            Debug.Log("[DeckControl] 첫 시작 감지 → 모든 프리셋을 기본 유닛으로 채웁니다");
            for (int i = 0; i < 5; i++)
            {
                await AutoFillPresetIfEmpty(i);
            }
        }
        else
        {
            bool activePresetEmpty = PlayData.IsPresetEmptyOnUnlockedSlots(activePresetIndex);

            if (activePresetEmpty)
            {
                Debug.LogWarning($"[DeckControl] 활성 프리셋({activePresetIndex})이 비어있음 → 자동 편성 시작");
                await AutoFillPresetIfEmpty(activePresetIndex);
            }
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
            return;

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
            if (!IsSlotUnlocked(presetIndex, i))
                continue;

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
            await DatabaseManager.Instance.SavePresetFromPlayDataAsync(presetIndex);
            Debug.Log($"[DeckControl] 프리셋 {presetIndex} 자동 편성 완료 - {filledCount}개 유닛 배치");
        }
    }

    public async void OnClickPresetButton(int index)
    {
        Debug.Log($"[DeckControl OnClickPresetButton] 프리셋 {index} 선택");

        activePresetIndex = index;
        PlayData.currentSelectedPreset = index;

        await DatabaseManager.Instance.SetActivePresetAsync(index);

        // 프리셋 전환 시 자동 편성하지 않음 - 저장된 프리셋 그대로 유지
        // (빈 프리셋이더라도 사용자가 직접 편성할 수 있도록 함)
        Debug.Log($"[DeckControl OnClickPresetButton] 프리셋 {index} 데이터 - 슬롯0: {PlayData.selectedDeckUnitIds[index, 0]}, 슬롯1: {PlayData.selectedDeckUnitIds[index, 1]}");

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
                if (!IsSlotUnlocked(p, i))
                {
                    preset.units[i] = null;
                    continue;
                }

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
            slot.SetInteractable(!slot.IsLocked);
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
        if (slot.IsLocked) return;

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

    /// <summary>
    /// 잠긴 슬롯 클릭 시 알림 패널 표시
    /// </summary>
    public void OnLockedSlotClicked(int slotIndex, int presetIndex)
    {
        pendingUnlockSlot = slotIndex;
        pendingUnlockPreset = presetIndex;

        // 비용 확인
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

        // 패널 표시
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

        // 구매 버튼 활성화 여부
        if (alertBuyButton != null)
        {
            alertBuyButton.interactable = canAfford;
        }
    }

    /// <summary>
    /// 알림 패널의 "구매" 버튼 클릭
    /// </summary>
    private async void OnAlertBuyClicked()
    {
        if (pendingUnlockSlot < 0 || pendingUnlockPreset < 0)
            return;

        // 패널 닫기
        if (unlockAlertPanel != null)
        {
            unlockAlertPanel.SetActive(false);
        }

        // 잠금 해제 시도
        bool success = await TryUnlockSlot(pendingUnlockSlot, pendingUnlockPreset);

        if (success)
        {
            Debug.Log($"[DeckControl] 슬롯 {pendingUnlockSlot} 해제 완료");
        }

        pendingUnlockSlot = -1;
        pendingUnlockPreset = -1;
    }

    /// <summary>
    /// 알림 패널의 "취소" 버튼 클릭
    /// </summary>
    private void OnAlertCancelClicked()
    {
        if (unlockAlertPanel != null)
        {
            unlockAlertPanel.SetActive(false);
        }

        pendingUnlockSlot = -1;
        pendingUnlockPreset = -1;
    }

    /// <summary>
    /// 슬롯 잠금 해제 실행
    /// </summary>
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
            // DB에 해제 상태 저장
            if (DatabaseManager.Instance.CurrentUser.presetSlotUnlocks == null)
            {
                DatabaseManager.Instance.CurrentUser.presetSlotUnlocks = new PresetSlotUnlockData();
            }

            DatabaseManager.Instance.CurrentUser.presetSlotUnlocks.UnlockSlot(presetIndex, slotIndex);
            await DatabaseManager.Instance.SaveSlotUnlocksAsync();

            // UI 업데이트 및 자동 유닛 배치
            UpdateAllSlotLockStates();
            await FillUnlockedSlotWithRandomUnit(presetIndex, slotIndex);

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

    /// <summary>
    /// 해제된 슬롯에 랜덤 유닛 배치
    /// </summary>
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
            Debug.LogWarning("[DeckControl] 배치할 수 있는 유닛이 없습니다!");
            return;
        }

        int randomIndex = Random.Range(0, availableUnits.Count);
        DeckUnitModel selectedUnit = availableUnits[randomIndex];

        presets[presetIdx].units[slotIdx] = selectedUnit;
        PlayData.selectedDeckUnitIds[presetIdx, slotIdx] = selectedUnit.unitId;
        PlayData.selectedDeckUnitIconAddresses[presetIdx, slotIdx] = selectedUnit.iconAddress;

        await DatabaseManager.Instance.SavePresetFromPlayDataAsync(presetIdx);

        Debug.Log($"[DeckControl] 슬롯 {slotIdx}에 유닛 {selectedUnit.unitId} 자동 배치 완료");
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
            if (slot.IsLocked)
                continue;

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
                NotifyUnitCleared(committed);
                UpdateCompleteButton();
                return;
            }
        }

        foreach (var slot in slots)
        {
            if (slot.IsLocked)
                continue;

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

        FillEmptySlotsWithRandomUnits();

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
                PlayData.selectedDeckUnitIds[activePresetIndex, i] = 0;
                PlayData.selectedDeckUnitIconAddresses[activePresetIndex, i] = "";
            }
        }

        bool saveSuccess = await DatabaseManager.Instance.SavePresetFromPlayDataAsync(activePresetIndex);

        if (saveSuccess)
        {
            Debug.Log($"[DeckControl OnCompleteClicked] 프리셋 {activePresetIndex} 저장 완료 - 슬롯0: {PlayData.selectedDeckUnitIds[activePresetIndex, 0]}, 슬롯1: {PlayData.selectedDeckUnitIds[activePresetIndex, 1]}");
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

    /// <summary>
    /// 모든 슬롯의 잠금 상태 업데이트
    /// </summary>
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
        if (!isEditing)
            return;

        if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame)
            return;
        if (EventSystem.current == null)
            return;

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
            return;

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
            Debug.LogError("[DeckControl] CRITICAL: selectedUnitIds가 비어있습니다!");
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
