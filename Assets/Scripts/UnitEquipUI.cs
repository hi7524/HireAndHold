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
    [SerializeField] private Image[] arrowImages; // 화살표 이미지들 (Inspector에서 할당)

    [Header("Arrow Animation Settings")]
    [SerializeField] private float arrowMoveDistance = 10f; // 위아래 이동 거리
    [SerializeField] private float arrowMoveSpeed = 1f; // 애니메이션 속도
    [SerializeField] private float arrowAnimationDelay = 0.1f; // 각 화살표 사이의 딜레이

    private DeckControl deckControl;
    private UIPopupManager popupManager;
    private int currentUnitId;
    private UnitData currentUnitData;

    private Vector3[] arrowOriginalPositions;
    private bool isArrowAnimating = false;

    private void Awake()
    {
        Debug.Log("[UnitEquipUI] Awake 호출");

        // 모든 화살표 초기 위치 저장
        if (arrowImages != null && arrowImages.Length > 0)
        {
            arrowOriginalPositions = new Vector3[arrowImages.Length];
            for (int i = 0; i < arrowImages.Length; i++)
            {
                if (arrowImages[i] != null)
                {
                    arrowOriginalPositions[i] = arrowImages[i].transform.localPosition;
                }
            }
        }

        if (replacePopupRoot != null)
            replacePopupRoot.SetActive(false);

        if (replaceCancelButton != null)
        {
            replaceCancelButton.onClick.RemoveAllListeners();
            replaceCancelButton.onClick.AddListener(() =>
            {
                Debug.Log("[UnitEquipUI] Cancel 버튼 클릭");
                replacePopupRoot.SetActive(false);
                StopArrowAnimation();
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
            StartArrowAnimation();
        }
    }

    private void RefreshReplaceSlots()
    {
        if (replaceSlotParent == null)
        {
            Debug.LogError("[UnitEquipUI] replaceSlotParent가 null!");
            return;
        }

        // 기존 슬롯 정리
        foreach (Transform child in replaceSlotParent)
            Destroy(child.gameObject);

        if (deckControl == null)
        {
            Debug.LogError("[UnitEquipUI] deckControl이 null!");
            return;
        }

        if (replaceSlotPrefab == null)
        {
            Debug.LogError("[UnitEquipUI] replaceSlotPrefab이 null!");
            return;
        }

        int preset = PlayData.currentSelectedPreset;
        int createdCount = 0;

        for (int i = 0; i < 5; i++)
        {
            if (!deckControl.CanEquipSlot(preset, i))
            {
                Debug.Log($"[UnitEquipUI] 슬롯 {i}는 잠금 상태 - 스킵");
                continue;
            }

            int slotIndex = i;
            int unitId = PlayData.selectedDeckUnitIds[preset, i];

            if (unitId == 0)
            {
                Debug.Log($"[UnitEquipUI] 슬롯 {i}는 비어있음 - 스킵");
                continue;
            }

            // 현재 선택한 유닛이 이미 장착된 슬롯은 표시하지 않음
            if (unitId == currentUnitId)
            {
                Debug.Log($"[UnitEquipUI] 슬롯 {i}는 현재 유닛과 동일 - 스킵");
                continue;
            }

            var model = deckControl.GetDeckUnitModelFromPreset(preset, slotIndex);
            if (model == null)
            {
                Debug.LogWarning($"[UnitEquipUI] 슬롯 {i}의 모델을 가져올 수 없음");
                continue;
            }

            // 슬롯 생성
            var slot = Instantiate(replaceSlotPrefab, replaceSlotParent);

            // 교체 팝업용 슬롯이므로 잠금 관련 UI 비활성화
            if (slot.lockOverlay != null)
            {
                slot.lockOverlay.SetActive(false);
            }

            // 유닛 데이터 설정
            slot.SetCommittedExternal(model);

            // 버튼 직접 활성화 (UpdateLockState 호출하지 않음)
            if (slot.slotButton != null)
            {
                slot.slotButton.interactable = true;
                Debug.Log($"[UnitEquipUI] 슬롯 {i} 버튼 활성화: {slot.slotButton.interactable}");
            }
            else
            {
                Debug.LogError($"[UnitEquipUI] 슬롯 {i}의 slotButton이 null!");
            }

            // 클릭 이벤트 연결
            slot.onSlotClickedExternal = _ =>
            {
                Debug.Log($"[UnitEquipUI] 슬롯 {slotIndex} 클릭됨");
                ReplaceSlot(slotIndex);
            };

            // 화살표 애니메이션 시작
            StartArrowAnimation(slot.transform);

            createdCount++;
            Debug.Log($"[UnitEquipUI] 슬롯 {i} 생성 완료 (unitId: {unitId})");
        }

        Debug.Log($"[UnitEquipUI] 총 {createdCount}개의 교체 슬롯 생성됨");
    }

    /// <summary>
    /// 슬롯 하위의 화살표에 위아래 움직이는 애니메이션 추가
    /// </summary>
    private void StartArrowAnimation(Transform slotTransform)
    {
        // 슬롯 하위에서 화살표 오브젝트 찾기
        // 일반적인 화살표 이름들로 검색
        string[] arrowNames = { "Arrow", "ArrowIcon", "DownArrow", "arrow", "ArrowImage" };
        Transform arrow = null;

        foreach (string name in arrowNames)
        {
            arrow = slotTransform.Find(name);
            if (arrow != null)
                break;
        }

        // 재귀적으로 하위 오브젝트 검색 (이름에 arrow가 포함된 경우)
        if (arrow == null)
        {
            foreach (Transform child in slotTransform.GetComponentsInChildren<Transform>())
            {
                if (child.name.ToLower().Contains("arrow"))
                {
                    arrow = child;
                    break;
                }
            }
        }

        if (arrow != null)
        {
            StartCoroutine(AnimateArrow(arrow));
        }
        else
        {
            Debug.LogWarning($"[UnitEquipUI] 화살표 오브젝트를 찾을 수 없습니다: {slotTransform.name}");
        }
    }

    /// <summary>
    /// 화살표 위아래 애니메이션 코루틴
    /// </summary>
    private System.Collections.IEnumerator AnimateArrow(Transform arrow)
    {
        Vector3 originalPosition = arrow.localPosition;
        float animationSpeed = 2f; // 애니메이션 속도 (높을수록 빠름)
        float moveDistance = 10f; // 위아래 이동 거리 (픽셀)

        while (arrow != null && arrow.gameObject.activeSelf)
        {
            float offset = Mathf.Sin(Time.time * animationSpeed) * moveDistance;
            arrow.localPosition = originalPosition + new Vector3(0, offset, 0);
            yield return null;
        }

        // 애니메이션 종료 시 원래 위치로 복귀
        if (arrow != null)
        {
            arrow.localPosition = originalPosition;
        }
    }

    private void ReplaceSlot(int slotIndex)
    {
        Debug.Log($"[UnitEquipUI] ReplaceSlot 호출 - slotIndex: {slotIndex}");

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

            StopArrowAnimation();

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

    /// <summary>
    /// 화살표 애니메이션 시작
    /// </summary>
    private void StartArrowAnimation()
    {
        if (arrowImages == null || arrowImages.Length == 0)
            return;

        isArrowAnimating = true;

        for (int i = 0; i < arrowImages.Length; i++)
        {
            if (arrowImages[i] != null)
            {
                int index = i; 
                AnimateArrow(index).Forget();
            }
        }
    }

    /// <summary>
    /// 화살표 애니메이션 중지
    /// </summary>
    private void StopArrowAnimation()
    {
        isArrowAnimating = false;

        if (arrowImages != null && arrowOriginalPositions != null)
        {
            for (int i = 0; i < arrowImages.Length; i++)
            {
                if (arrowImages[i] != null && i < arrowOriginalPositions.Length)
                {
                    arrowImages[i].transform.localPosition = arrowOriginalPositions[i];
                }
            }
        }
    }

    /// <summary>
    /// 개별 화살표 위아래 애니메이션
    /// </summary>
    private async UniTaskVoid AnimateArrow(int index)
    {
        if (arrowImages == null || index >= arrowImages.Length || arrowImages[index] == null)
            return;

        if (arrowOriginalPositions == null || index >= arrowOriginalPositions.Length)
            return;

        await UniTask.Delay(System.TimeSpan.FromSeconds(arrowAnimationDelay * index));

        Image arrow = arrowImages[index];
        Vector3 originalPos = arrowOriginalPositions[index];

        while (isArrowAnimating)
        {
            // 아래로 이동
            float elapsedTime = 0f;
            Vector3 startPos = originalPos;
            Vector3 targetPos = originalPos + Vector3.down * arrowMoveDistance;

            while (elapsedTime < arrowMoveSpeed && isArrowAnimating)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / arrowMoveSpeed;
                float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);
                if (arrow != null)
                    arrow.transform.localPosition = Vector3.Lerp(startPos, targetPos, easedT);
                await UniTask.Yield();
            }

            if (!isArrowAnimating)
                break;

            // 위로 이동
            elapsedTime = 0f;
            startPos = targetPos;
            targetPos = originalPos;

            while (elapsedTime < arrowMoveSpeed && isArrowAnimating)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / arrowMoveSpeed;
                float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);
                if (arrow != null)
                    arrow.transform.localPosition = Vector3.Lerp(startPos, targetPos, easedT);
                await UniTask.Yield();
            }
        }

        if (arrow != null)
        {
            arrow.transform.localPosition = originalPos;
        }
    }
}
