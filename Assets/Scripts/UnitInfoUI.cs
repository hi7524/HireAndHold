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

    private int currentUnitId = -1;
    private Unit previewUnit;

    private bool isTableReady = false;


    private DataTable_Unit unitTable;
    private UniTask tablesLoadTask;

    private void Awake()
    {
        tablesLoadTask = InitializeTablesAsync();
        InitializeSubComponents();
        mainRoot.SetActive(false);
    }

    private void InitializeSubComponents()
    {
        if (enforceUI != null) enforceUI.SetPopupManager(popupManager);
        if (equipUI != null) equipUI.SetPopupManager(popupManager);
    }

    private async UniTask InitializeTablesAsync()
    {
        unitTable = new DataTable_Unit();
        await unitTable.LoadAsync("UnitTable");
        isTableReady = true;
    }
    public async UniTask EnsureReady()
    {
        if (!isTableReady)
            await UniTask.WaitUntil(() => isTableReady);

        if (currentUnitId < 0)
            await UniTask.WaitUntil(() => currentUnitId >= 0);

        if (previewUnit == null)
            await UniTask.WaitUntil(() => previewUnit != null);
        await UniTask.WaitUntil(() => previewUnit.IsInitialized);
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

    public void SetUnit(int id)
    {
        OpenAndRefreshAsync(id).Forget();
    }

    private async UniTaskVoid OpenAndRefreshAsync(int id)
    {
        currentUnitId = id;

        mainRoot.SetActive(true);

        await tablesLoadTask;

        await CreatePreviewAsync();

        await RefreshAsync();
    }

    private async UniTask RefreshAsync()
    {
        if (currentUnitId < 0) return;

        var data = unitTable.Get(currentUnitId);
        if (data == null)
        {
            Debug.LogError("[UnitInfoUI] UnitData 없음: " + currentUnitId);
            return;
        }

        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());

        if (infoDisplay != null)
            await infoDisplay.UpdateDisplay(currentUnitId, data, character, previewUnit);

        if (enforceUI != null)
            enforceUI.UpdateButtons(character);

        if (equipUI != null)
            equipUI.SetCurrentUnit(currentUnitId, data);
    }

    private async UniTask CreatePreviewAsync()
    {
        if (previewUnit != null)
            Destroy(previewUnit.gameObject);

        var go = new GameObject("PreviewUnit_" + currentUnitId);
        previewUnit = go.AddComponent<Unit>();
        previewUnit.SetUnitID(currentUnitId);

        await UniTask.WaitUntil(() => previewUnit.IsInitialized);
    }

    public Unit GetPreviewUnit() => previewUnit;
    public int GetCurrentUnitId() => currentUnitId;

    public void RefreshUI()
    {
        if (currentUnitId >= 0)
            OpenAndRefreshAsync(currentUnitId).Forget();
    }

}
