using Cysharp.Threading.Tasks;
using GameData;
using UnityEngine;
using UnityEngine.UI;

public class UnitEnforceUI : MonoBehaviour
{
    [Header("Main Buttons")]
    [SerializeField] private Button normalEnforceButton;
    [SerializeField] private Button heroEnforceButton;

    [Header("Sub Components")]
    [SerializeField] private NormalEnforcePopup normalPopup;
    [SerializeField] private HeroEnforcePopup heroPopup;

    private BattleUnitManager battleUnitManager;
    private NormalEnforceSystem normalSystem;
    private HeroEnforceSystem heroSystem;

    private UnitInfoUI mainUI;
    private UIPopupManager popupManager;

    private DataTable_Unit unitTable;
    private DataTable_NormalEnforce normalTable;
    private DataTable_HeroEnforce heroTable;
    private DataTable_HeroEnforceEffect effectTable;


    private const int HERO_MAX = 4;
    private bool isTablesLoaded = false;

    private void Start()
    {
        InitializeTables().Forget();
        SetupButtons();
    }

    public void SetPopupManager(UIPopupManager manager)
    {
        popupManager = manager;
    }

    private async UniTaskVoid InitializeTables()
    {
        unitTable = new DataTable_Unit();
        normalTable = new DataTable_NormalEnforce();
        heroTable = new DataTable_HeroEnforce();
        effectTable = new DataTable_HeroEnforceEffect();

        await unitTable.LoadAsync("UnitTable");
        await normalTable.LoadAsync("NormalEnforceTable");
        await heroTable.LoadAsync("HeroEnforceTable");
        await effectTable.LoadAsync("HeroEnforceEffectTable");

        isTablesLoaded = true;
    }

    private void SetupButtons()
    {
        normalEnforceButton.onClick.AddListener(OnNormalEnforceClicked);
        heroEnforceButton.onClick.AddListener(OnHeroEnforceClicked);

        mainUI = GetComponentInParent<UnitInfoUI>();
    }

    public async void SetUnitManager(BattleUnitManager manager)
    {
        battleUnitManager = manager;

        // 테이블 로드 대기
        if (!isTablesLoaded)
        {
            await UniTask.WaitUntil(() => isTablesLoaded);
        }

        normalSystem = new NormalEnforceSystem(manager, normalTable, unitTable);
        heroSystem = new HeroEnforceSystem(manager, heroTable, effectTable);

        if (normalPopup != null)
        {
            normalPopup.SetEnforceSystem(normalSystem, mainUI, unitTable);
            normalPopup.SetPopupManager(popupManager);
        }

        if (heroPopup != null)
        {
            heroPopup.SetEnforceSystem(heroSystem, unitTable, heroTable, effectTable, mainUI);
            heroPopup.SetPopupManager(popupManager);
        }
    }

    public void UpdateButtons(OwnedCharacter character)
    {
        bool owned = character != null;
        int heroLv = owned ? character.heroEnforceLevel : 0;

        normalEnforceButton.interactable = owned;
        heroEnforceButton.interactable = owned && heroLv < HERO_MAX;
    }

    private void OnNormalEnforceClicked()
    {
        _ = OnNormalEnforceClickedAsync();
    }

    private async UniTaskVoid OnNormalEnforceClickedAsync()
    {
        if (mainUI == null)
        {
            popupManager?.ShowAlert("UI 초기화가 필요합니다.");
            return;
        }

        await mainUI.EnsureReady(); 

        int unitId = mainUI.GetCurrentUnitId();
        if (unitId < 0)
        {
            popupManager?.ShowAlert("유닛 정보를 불러오지 못했습니다.");
            return;
        }

        var preview = mainUI.GetPreviewUnit();
        if (preview == null)
        {
            await UniTask.WaitUntil(() => mainUI.GetPreviewUnit() != null);
            preview = mainUI.GetPreviewUnit();
        }

        normalPopup?.Open(unitId, preview);
    }



    private void OnHeroEnforceClicked()
    {
        _ = OnHeroEnforceClickedAsync();
    }

    private async UniTaskVoid OnHeroEnforceClickedAsync()
    {
        if (mainUI == null)
        {
            popupManager?.ShowAlert("UI 초기화가 필요합니다.");
            return;
        }

        await mainUI.EnsureReady();

        int unitId = mainUI.GetCurrentUnitId();
        if (unitId < 0)
        {
            popupManager?.ShowAlert("유닛 정보를 불러오지 못했습니다.");
            return;
        }

        var preview = mainUI.GetPreviewUnit();
        if (preview == null)
        {
            await UniTask.WaitUntil(() => mainUI.GetPreviewUnit() != null);
            preview = mainUI.GetPreviewUnit();
        }

        heroPopup?.Open(unitId, preview);
    }
}
