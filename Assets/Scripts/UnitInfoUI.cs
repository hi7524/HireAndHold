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

    private void OnStarChangedFromSelector(int star)
    {
        if (currentStar == star)
            return;

        currentStar = star;
        RefreshByStar();
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

        int displayUnitId = unitData.UNIT_ID;

        OwnedCharacter character = null;
        if (currentStar == 1)
            character = DatabaseManager.Instance.GetCharacter(baseUnitId.ToString());

        if (infoDisplay != null)
            infoDisplay.UpdateDisplay(displayUnitId, unitData, character, previewUnit);

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
            RefreshByStar();
    }

    public int GetCurrentUnitId()
    {
        var data = FindUnitDataByStar(baseUnitId, currentStar);
        return data != null ? data.UNIT_ID : -1;
    }

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
}
