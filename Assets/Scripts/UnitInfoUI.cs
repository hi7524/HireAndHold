using Cysharp.Threading.Tasks;
using GameData;
using UnityEngine;

public class UnitInfoUI : MonoBehaviour
{
    [Header("Main Root")]
    [SerializeField] private GameObject mainRoot;

    [Header("Sub Components")]
    [SerializeField] private UnitInfoDisplay infoDisplay;
    [SerializeField] private UnitEnforceUI enforceUI;
    [SerializeField] private UnitEquipUI equipUI;
    [SerializeField] private UIPopupManager popupManager;
    [Header("Star Selector")]
    [SerializeField] private UnitStarSelector starSelector;

    private int baseUnitId = -1;
    private int currentStar = 1;
    private Unit previewUnit;
    private bool isPreviewUnitReady = false;

    private void Awake()
    {
        InitializeSubComponents();

        if (starSelector != null)
        {
            starSelector.OnStarChanged += OnStarChangedFromSelector;
        }

        mainRoot.SetActive(false);
    }


    public void OnHeroEnforceCompleted()
    {
        Debug.Log("[UnitInfoUI] OnHeroEnforceCompleted 호출");

        RefreshByStar();
    }


    private void OnStarChangedFromSelector(int star)
    {
        if (currentStar == star)
            return;

        currentStar = star;

        // 별이 변경되면 해당 별의 유닛으로 previewUnit 재생성
        var unitData = FindUnitDataByStar(baseUnitId, currentStar);
        if (unitData != null)
        {
            CreatePreviewUnit(unitData.UNIT_ID);
        }
        else
        {
            RefreshByStar();
        }
    }

    private void InitializeSubComponents()
    {
        if (enforceUI != null)
            enforceUI.SetPopupManager(popupManager);

        if (equipUI != null)
            equipUI.SetPopupManager(popupManager);
    }

    public void SetUnitManager(BattleUnitManager manager)
    {
        if (enforceUI != null)
            enforceUI.SetUnitManager(manager);
    }

    public void SetDeckControl(DeckControl control)
    {
        if (equipUI != null)
            equipUI.SetDeckControl(control);
    }

    public void SetUnit(int unitId)
    {
        Open(unitId);
    }

    private void Open(int unitId)
    {
        if (!DataTableManager.IsInitialized)
        {
            Debug.LogWarning("[UnitInfoUI] 테이블이 아직 로딩되지 않았습니다.");
            return;
        }

        baseUnitId = unitId;
        currentStar = 1;
        mainRoot.SetActive(true);

        CreatePreviewUnit(baseUnitId);

        int maxStar = GetMaxStarForBaseUnit(baseUnitId);
        if (starSelector != null)
            starSelector.Initialize(maxStar);
    }

    private void CreatePreviewUnit(int unitId)
    {
        if (previewUnit != null)
            Destroy(previewUnit.gameObject);

        isPreviewUnitReady = false;

        var go = new GameObject($"PreviewUnit_{unitId}");
        previewUnit = go.AddComponent<Unit>();
        previewUnit.IsPreview = true;
        previewUnit.SetUnitID(unitId);

        WaitForPreviewUnitAndRefresh().Forget();
    }

    private async UniTaskVoid WaitForPreviewUnitAndRefresh()
    {
        await UniTask.WaitUntil(() => previewUnit != null && previewUnit.IsInitialized);
        isPreviewUnitReady = true;
        RefreshByStar();
    }

    private void RefreshByStar()
    {
        Debug.Log($"[UnitInfoUI] RefreshByStar 호출 - baseUnitId: {baseUnitId}, currentStar: {currentStar}");

        if (!DataTableManager.IsInitialized)
        {
            Debug.LogWarning("[UnitInfoUI] 테이블이 아직 로딩되지 않았습니다.");
            return;
        }

        if (!isPreviewUnitReady)
        {
            Debug.LogWarning("[UnitInfoUI] PreviewUnit이 아직 준비되지 않았습니다.");
            return;
        }

        var unitData = FindUnitDataByStar(baseUnitId, currentStar);
        if (unitData == null)
        {
            Debug.LogError($"[UnitInfoUI] UnitData 없음: base={baseUnitId}, star={currentStar}");
            return;
        }

        Debug.Log($"[UnitInfoUI] UnitData 발견 - UNIT_ID: {unitData.UNIT_ID}, LEVEL: {unitData.LEVEL}, SKILL1: {unitData.UNIT_SKILL1}, SKILL2: {unitData.UNIT_SKILL2}");

        int displayUnitId = unitData.UNIT_ID;

        // 강화/장착은 항상 1성(baseUnitId) 기준으로 동작
        OwnedCharacter character = DatabaseManager.Instance.GetCharacter(baseUnitId.ToString());

        if (infoDisplay != null)
        {
            Debug.Log($"[UnitInfoUI] infoDisplay.UpdateDisplay 호출 - displayUnitId: {displayUnitId}");
            infoDisplay.UpdateDisplay(displayUnitId, unitData, character, previewUnit);
        }
        else
        {
            Debug.LogWarning("[UnitInfoUI] infoDisplay가 null입니다!");
        }

        if (enforceUI != null)
            enforceUI.UpdateButtons(character);

        if (equipUI != null)
            equipUI.SetCurrentUnit(displayUnitId, unitData);
    }

    public Unit GetPreviewUnit() => previewUnit;
    public int GetBaseUnitId() => baseUnitId;
    public int GetCurrentStar() => currentStar;

    public void RefreshUI()
    {
        if (baseUnitId >= 0 && isPreviewUnitReady)
        {
            PlayData.SyncCharactersFromDatabase();

            RefreshByStar();
        }
    }


    public int GetCurrentUnitId()
    {
        var data = FindUnitDataByStar(baseUnitId, currentStar);
        return data != null ? data.UNIT_ID : -1;
    }

    // 강화/장착용으로 항상 1성(baseUnitId) 반환
    public int GetBaseUnitIdForEnforce() => baseUnitId;

    public async UniTask WaitUntilReady()
    {
        if (!DataTableManager.IsInitialized)
            await UniTask.WaitUntil(() => DataTableManager.IsInitialized);

        if (baseUnitId < 0)
            await UniTask.WaitUntil(() => baseUnitId >= 0);

        if (!isPreviewUnitReady)
            await UniTask.WaitUntil(() => isPreviewUnitReady);
    }

    private UnitData FindUnitDataByStar(int baseUnitId, int star)
    {
        var unitTable = DataTableManager.UnitTable;
        if (unitTable == null)
            return null;

        var baseData = unitTable.Get(baseUnitId);
        if (baseData == null)
            return null;

        string groupKey = baseData.GRID_DATA;

        foreach (var data in unitTable.GetAll())
        {
            if (data.GRID_DATA == groupKey && data.LEVEL == star)
                return data;
        }

        return null;
    }

    private int GetMaxStarForBaseUnit(int baseUnitId)
    {
        var unitTable = DataTableManager.UnitTable;
        if (unitTable == null)
            return 1;

        var baseData = unitTable.Get(baseUnitId);
        if (baseData == null)
            return 1;

        string groupKey = baseData.GRID_DATA;
        int maxStar = 1;

        foreach (var data in unitTable.GetAll())
        {
            if (data.GRID_DATA == groupKey)
                maxStar = Mathf.Max(maxStar, data.LEVEL);
        }

        return maxStar;
    }

    private void OnDestroy()
    {
        if (starSelector != null)
            starSelector.OnStarChanged -= OnStarChangedFromSelector;

        if (previewUnit != null)
            Destroy(previewUnit.gameObject);
    }
    private void OnCharacterUpdated(string characterId)
    {

        if (baseUnitId < 0 || characterId != baseUnitId.ToString())
            return;

        Debug.Log("[UnitInfoUI] 캐릭터 변경 이벤트 수신 → 즉시 UI 갱신");
        RefreshUI();
    }


    private void OnEnable()
    {
        PlayData.OnCharacterUpdated += OnCharacterUpdated;
    }

    private void OnDisable()
    {
        PlayData.OnCharacterUpdated -= OnCharacterUpdated;
    }
}
