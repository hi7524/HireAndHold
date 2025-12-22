using Cysharp.Threading.Tasks;
using GameData;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class UnitEquipUI : MonoBehaviour
{
    [Header("Main Button")]
    [SerializeField] private Button equipButton;

    [Header("Replace Popup")]
    [SerializeField] private GameObject replacePopupRoot;
    [SerializeField] private Transform replaceSlotParent;
    [SerializeField] private DeckSlot replaceSlotPrefab;
    [SerializeField] private Button replaceCancelButton;
    [SerializeField] private Image replaceSelectedUnitImage;

    private DeckControl deckControl;
    private UIPopupManager popupManager;

    private DataTable_Unit unitTable;
    private UniTask tableLoadTask;

    private int currentUnitId;
    private UnitData currentUnitData;

    private void Awake()
    {
        replacePopupRoot.SetActive(false);
        replaceCancelButton.onClick.AddListener(() => replacePopupRoot.SetActive(false));

        tableLoadTask = InitializeTableAsync();

        equipButton.onClick.AddListener(() => OnEquipButtonClickedAsync().Forget());
    }

    public void SetPopupManager(UIPopupManager manager) => popupManager = manager;
    public void SetDeckControl(DeckControl control) => deckControl = control;

    private async UniTask InitializeTableAsync()
    {
        unitTable = new DataTable_Unit();
        await unitTable.LoadAsync("UnitTable");
    }

    public void SetCurrentUnit(int unitId, UnitData data)
    {
        currentUnitId = unitId;
        currentUnitData = data;
        LoadSelectedUnitImage().Forget();
    }

    private async UniTaskVoid LoadSelectedUnitImage()
    {
        if (currentUnitData == null || replaceSelectedUnitImage == null) return;

        try
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(currentUnitData.UNIT_ICON).Task;
            replaceSelectedUnitImage.sprite = sprite;
        }
        catch { }
    }

    private async UniTaskVoid OnEquipButtonClickedAsync()
    {

        await tableLoadTask;

        if (deckControl == null)
        {
            popupManager?.ShowAlert("덱 컨트롤러를 찾을 수 없습니다.");
            return;
        }

        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
        {
            popupManager?.ShowAlert("미보유 유닛은 장착할 수 없습니다.");
            return;
        }

        int preset = PlayData.currentSelectedPreset;

        // 이미 편성인지 체크
        for (int i = 0; i < 5; i++)
        {
            if (PlayData.selectedDeckUnitIds[preset, i] == currentUnitId)
            {
                popupManager?.ShowAlert($"이미 슬롯 {i + 1}번에 편성되어 있습니다.");
                return;
            }
        }

        // 빈 슬롯 찾기
        for (int i = 0; i < 5; i++)
        {
            if (PlayData.selectedDeckUnitIds[preset, i] == 0)
            {
                await EquipToSlotAsync(i);
                return;
            }
        }

        // 교체 팝업
        ShowReplacePopup();
    }

    private async UniTask EquipToSlotAsync(int slotIndex)
    {
        int preset = PlayData.currentSelectedPreset;

        PlayData.selectedDeckUnitIds[preset, slotIndex] = currentUnitId;
        PlayData.selectedDeckUnitIconAddresses[preset, slotIndex] = currentUnitData.UNIT_ICON;

        await DatabaseManager.Instance.SavePresetFromPlayDataAsync(preset);

        deckControl.ApplyPresetToSelectedUnitIds();
        deckControl.LoadPresets();
        deckControl.OnClickPresetButton(preset);

        popupManager?.ShowSuccess(
            "장착 완료",
            $"{currentUnitData.StringName}이(가) 슬롯 {slotIndex + 1}번에 장착되었습니다."
        );

        GetComponentInParent<UnitInfoUI>()?.gameObject.SetActive(false);
    }

    private void ShowReplacePopup()
    {
        replacePopupRoot.SetActive(true);
        RefreshReplaceSlots();
    }

    private void RefreshReplaceSlots()
    {
        foreach (Transform child in replaceSlotParent)
            Destroy(child.gameObject);

        int preset = PlayData.currentSelectedPreset;

        for (int i = 0; i < 5; i++)
        {
            int slotIndex = i;
            int unitId = PlayData.selectedDeckUnitIds[preset, i];
            if (unitId == 0) continue;

            var model = deckControl.GetDeckUnitModelFromPreset(preset, slotIndex);
            if (model == null) continue;

            var slot = Instantiate(replaceSlotPrefab, replaceSlotParent);
            slot.SetCommittedExternal(model);
            slot.SetInteractable(true);

            slot.onSlotClickedExternal = _ => ReplaceSlotAsync(slotIndex).Forget();
        }
    }

    private async UniTaskVoid ReplaceSlotAsync(int slotIndex)
    {
        await tableLoadTask;

        int preset = PlayData.currentSelectedPreset;

        // 중복 편성 검사
        for (int i = 0; i < 5; i++)
        {
            if (i == slotIndex) continue;
            if (PlayData.selectedDeckUnitIds[preset, i] == currentUnitId)
            {
                popupManager?.ShowAlert("이미 다른 슬롯에 편성된 유닛입니다.");
                return;
            }
        }

        int oldUnitId = PlayData.selectedDeckUnitIds[preset, slotIndex];
        var oldData = unitTable.Get(oldUnitId);

        PlayData.selectedDeckUnitIds[preset, slotIndex] = currentUnitId;
        PlayData.selectedDeckUnitIconAddresses[preset, slotIndex] = currentUnitData.UNIT_ICON;

        await DatabaseManager.Instance.SavePresetFromPlayDataAsync(preset);

        deckControl.ApplyPresetToSelectedUnitIds();
        deckControl.LoadPresets();
        deckControl.OnClickPresetButton(preset);

        replacePopupRoot.SetActive(false);
        GetComponentInParent<UnitInfoUI>()?.gameObject.SetActive(false);

        popupManager?.ShowSuccess(
            "교체 완료",
            $"슬롯 {slotIndex + 1}번\n{oldData?.StringName ?? "빈 슬롯"} → {currentUnitData.StringName}"
        );
    }
}
