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
    private int currentUnitId;
    private UnitData currentUnitData;

    private void Awake()
    {
        Debug.Log("[UnitEquipUI] Awake 호출");


        if (replacePopupRoot != null)
            replacePopupRoot.SetActive(false);

        if (replaceCancelButton != null)
        {
            replaceCancelButton.onClick.RemoveAllListeners();
            replaceCancelButton.onClick.AddListener(() =>
            {
                Debug.Log("[UnitEquipUI] Cancel 버튼 클릭");
                replacePopupRoot.SetActive(false);
            });
        }

        if (equipButton != null)
        {

            equipButton.interactable = true;

            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnEquipButtonClicked);

            equipButton.onClick.Invoke();

        }
        else
        {
            Debug.LogError("[UnitEquipUI] equipButton이 null!");
        }
    }

    public void SetPopupManager(UIPopupManager manager) => popupManager = manager;
    public void SetDeckControl(DeckControl control) => deckControl = control;

    public void SetCurrentUnit(int unitId, UnitData data)
    {
        currentUnitId = unitId;
        currentUnitData = data;

        if (currentUnitData != null && replaceSelectedUnitImage != null)
            LoadSelectedUnitImageAsync().Forget();
    }

    private async UniTaskVoid LoadSelectedUnitImageAsync()
    {
        if (currentUnitData == null || replaceSelectedUnitImage == null)
            return;

        try
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(currentUnitData.UNIT_ICON).Task;
            if (replaceSelectedUnitImage != null)
                replaceSelectedUnitImage.sprite = sprite;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UnitEquipUI] 유닛 아이콘 로드 실패: {ex.Message}");
        }
    }

    private void OnEquipButtonClicked()
    {

        // 먼저 교체 팝업이 열려있으면 닫기
        if (replacePopupRoot != null && replacePopupRoot.activeSelf)
        {
            replacePopupRoot.SetActive(false);
        }

        if (!DataTableManager.IsInitialized)
        {
            popupManager?.ShowAlert("데이터 로딩 중입니다.");
            return;
        }

        if (deckControl == null)
        {
            Debug.LogError("[UnitEquipUI] deckControl이 null");
            popupManager?.ShowAlert("덱 컨트롤러를 찾을 수 없습니다.");
            return;
        }

        if (currentUnitId <= 0 || currentUnitData == null)
        {
            popupManager?.ShowAlert("유닛 정보가 설정되지 않았습니다.");
            return;
        }

        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
        {
            Debug.LogWarning($"[UnitEquipUI] 미보유 유닛 - unitId: {currentUnitId}");
            popupManager?.ShowAlert("미보유 유닛은 장착할 수 없습니다.");
            return;
        }

        int preset = PlayData.currentSelectedPreset;
        Debug.Log($"[UnitEquipUI] preset: {preset}, unitId: {currentUnitId}");

        for (int i = 0; i < 5; i++)
        {
            if (PlayData.selectedDeckUnitIds[preset, i] == currentUnitId)
            {
                Debug.Log($"[UnitEquipUI] 이미 슬롯 {i + 1}에 장착됨");
                popupManager?.ShowAlert($"이미 슬롯 {i + 1}번에 편성되어 있습니다.");
                return;
            }
        }

        for (int i = 0; i < 5; i++)
        {
            if (!deckControl.CanEquipSlot(preset, i))
                continue;

            if (PlayData.selectedDeckUnitIds[preset, i] == 0)
            {
                EquipToSlot(i);
                return;
            }
        }



        ShowReplacePopup();
    }

    private void EquipToSlot(int slotIndex)
    {
        int preset = PlayData.currentSelectedPreset;

        PlayData.selectedDeckUnitIds[preset, slotIndex] = currentUnitId;
        PlayData.selectedDeckUnitIconAddresses[preset, slotIndex] = currentUnitData.UNIT_ICON;

        SaveAndRefreshDeck(preset, slotIndex).Forget();
    }

    private async UniTaskVoid SaveAndRefreshDeck(int preset, int slotIndex)
    {
        try
        {
            await DatabaseManager.Instance.SavePresetFromPlayDataAsync(preset);

            if (deckControl != null)
            {
                deckControl.ApplyPresetToSelectedUnitIds();
                deckControl.LoadPresets();
                deckControl.OnClickPresetButton(preset);
            }

            popupManager?.ShowSuccess(
                "장착 완료",
                $"{currentUnitData.StringName}이(가) 슬롯 {slotIndex + 1}번에 장착되었습니다."
            );

            GetComponentInParent<UnitInfoUI>()?.gameObject.SetActive(false);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UnitEquipUI] 저장 실패: {ex}");
            popupManager?.ShowAlert("저장에 실패했습니다.");
        }
    }

    private void ShowReplacePopup()
    {
        int preset = PlayData.currentSelectedPreset;

        // 이미 장착되어 있는지 최종 확인
        for (int i = 0; i < 5; i++)
        {
            if (PlayData.selectedDeckUnitIds[preset, i] == currentUnitId)
            {
                Debug.Log($"[UnitEquipUI] ShowReplacePopup - 이미 슬롯 {i + 1}에 장착됨");
                popupManager?.ShowAlert($"이미 슬롯 {i + 1}번에 편성되어 있습니다.");
                return;
            }
        }

        if (replacePopupRoot != null)
        {
            replacePopupRoot.SetActive(true);
            Debug.Log("[UnitEquipUI] replacePopupRoot 활성화");
            RefreshReplaceSlots();
        }
    }

    private void RefreshReplaceSlots()
    {
        if (replaceSlotParent == null)
            return;

        foreach (Transform child in replaceSlotParent)
            Destroy(child.gameObject);

        if (deckControl == null || replaceSlotPrefab == null)
            return;

        int preset = PlayData.currentSelectedPreset;

        for (int i = 0; i < 5; i++)
        {
            if (!deckControl.CanEquipSlot(preset, i))
                continue;
            int slotIndex = i;
            int unitId = PlayData.selectedDeckUnitIds[preset, i];
            if (unitId == 0) continue;

            // 현재 선택한 유닛이 이미 장착된 슬롯은 표시하지 않음
            if (unitId == currentUnitId) continue;

            var model = deckControl.GetDeckUnitModelFromPreset(preset, slotIndex);
            if (model == null) continue;

            var slot = Instantiate(replaceSlotPrefab, replaceSlotParent);
            slot.SetCommittedExternal(model);
            slot.SetInteractable(true);

            slot.onSlotClickedExternal = _ => ReplaceSlot(slotIndex);
        }
    }

    private void ReplaceSlot(int slotIndex)
    {
        if (!DataTableManager.IsInitialized)
        {
            popupManager?.ShowAlert("데이터 로딩 중입니다.");
            return;
        }



        int preset = PlayData.currentSelectedPreset;

        // 이미 다른 슬롯에 편성되어 있는지 확인
        for (int i = 0; i < 5; i++)
        {
            if (i == slotIndex) continue;
            if (PlayData.selectedDeckUnitIds[preset, i] == currentUnitId)
            {
                // 교체 팝업 닫기
                if (replacePopupRoot != null)
                    replacePopupRoot.SetActive(false);

                popupManager?.ShowAlert("이미 다른 슬롯에 편성된 유닛입니다.");
                return;
            }
        }

        if (!deckControl.CanEquipSlot(preset, slotIndex))
        {
            popupManager?.ShowAlert("잠긴 슬롯에는 장착할 수 없습니다.");
            return;
        }


        if (PlayData.selectedDeckUnitIds[preset, slotIndex] == currentUnitId)
        {
            // 교체 팝업 닫기
            if (replacePopupRoot != null)
                replacePopupRoot.SetActive(false);

            popupManager?.ShowAlert("이미 해당 슬롯에 편성된 유닛입니다.");
            return;
        }

        int oldUnitId = PlayData.selectedDeckUnitIds[preset, slotIndex];
        var unitTable = DataTableManager.UnitTable;
        var oldData = unitTable?.Get(oldUnitId);

        PlayData.selectedDeckUnitIds[preset, slotIndex] = currentUnitId;
        PlayData.selectedDeckUnitIconAddresses[preset, slotIndex] = currentUnitData.UNIT_ICON;

        SaveAndRefreshAfterReplace(preset, slotIndex, oldData).Forget();
    }

    private async UniTaskVoid SaveAndRefreshAfterReplace(int preset, int slotIndex, UnitData oldData)
    {
        try
        {
            await DatabaseManager.Instance.SavePresetFromPlayDataAsync(preset);

            if (deckControl != null)
            {
                deckControl.ApplyPresetToSelectedUnitIds();
                deckControl.LoadPresets();
                deckControl.OnClickPresetButton(preset);
            }

            if (replacePopupRoot != null)
                replacePopupRoot.SetActive(false);

            GetComponentInParent<UnitInfoUI>()?.gameObject.SetActive(false);

            popupManager?.ShowSuccess(
                "교체 완료",
                $"슬롯 {slotIndex + 1}번\n{oldData?.StringName ?? "빈 슬롯"} → {currentUnitData.StringName}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UnitEquipUI] 교체 저장 실패: {ex}");
            popupManager?.ShowAlert("교체 저장에 실패했습니다.");
        }
    }
}
