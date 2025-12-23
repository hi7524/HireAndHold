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

    private const int HERO_MAX = 4;
    private bool isInitialized = false;

    private void Start()
    {
        if (normalEnforceButton != null)
        {
            normalEnforceButton.onClick.RemoveAllListeners();
            normalEnforceButton.onClick.AddListener(OnNormalEnforceClicked);
        }

        if (heroEnforceButton != null)
        {
            heroEnforceButton.onClick.RemoveAllListeners();
            heroEnforceButton.onClick.AddListener(OnHeroEnforceClicked);
        }

        mainUI = GetComponentInParent<UnitInfoUI>();
    }

    public void SetPopupManager(UIPopupManager manager)
    {
        popupManager = manager;
    }

    public void SetUnitManager(BattleUnitManager manager)
    {
        if (!DataTableManager.IsInitialized)
            return;

        battleUnitManager = manager;

        var unitTable = DataTableManager.UnitTable;
        var normalTable = DataTableManager.NormalEnforceTable;
        var heroTable = DataTableManager.heroEnforceTable;
        var effectTable = DataTableManager.heroEnforceEffectTable;

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

        isInitialized = true;
    }

    public void UpdateButtons(OwnedCharacter character)
    {
        // 초기화되지 않았으면 버튼만 비활성화
        if (!isInitialized)
        {
            if (normalEnforceButton != null)
                normalEnforceButton.interactable = false;

            if (heroEnforceButton != null)
                heroEnforceButton.interactable = false;

            return;
        }

        bool owned = character != null;
        int heroLv = owned ? character.heroEnforceLevel : 0;

        if (normalEnforceButton != null)
            normalEnforceButton.interactable = owned;

        if (heroEnforceButton != null)
            heroEnforceButton.interactable = owned && heroLv < HERO_MAX;
    }

    private void OnNormalEnforceClicked()
    {
        if (mainUI == null)
            mainUI = GetComponentInParent<UnitInfoUI>();

        if (mainUI == null)
        {
            popupManager?.ShowAlert("UI 초기화가 필요합니다.");
            return;
        }

        if (!DataTableManager.IsInitialized)
        {
            popupManager?.ShowAlert("데이터 로딩 중입니다.");
            return;
        }

        if (!isInitialized || normalSystem == null)
        {
            popupManager?.ShowAlert("강화 시스템을 초기화할 수 없습니다.");
            return;
        }

        // 항상 1성(baseUnitId) 기준으로 강화
        int baseUnitId = mainUI.GetBaseUnitIdForEnforce();
        if (baseUnitId < 0)
        {
            popupManager?.ShowAlert("유닛 정보를 불러오지 못했습니다.");
            return;
        }

        var preview = mainUI.GetPreviewUnit();
        if (preview == null || !preview.IsInitialized)
        {
            popupManager?.ShowAlert("유닛 데이터 로딩 중입니다.");
            return;
        }

        if (normalPopup != null)
            normalPopup.Open(baseUnitId, preview);
    }

    private void OnHeroEnforceClicked()
    {
        if (mainUI == null)
            mainUI = GetComponentInParent<UnitInfoUI>();

        if (mainUI == null)
        {
            popupManager?.ShowAlert("UI 초기화가 필요합니다.");
            return;
        }

        if (!DataTableManager.IsInitialized)
        {
            popupManager?.ShowAlert("데이터 로딩 중입니다.");
            return;
        }

        if (!isInitialized || heroSystem == null)
        {
            popupManager?.ShowAlert("강화 시스템을 초기화할 수 없습니다.");
            return;
        }

        // 항상 1성(baseUnitId) 기준으로 강화
        int baseUnitId = mainUI.GetBaseUnitIdForEnforce();
        if (baseUnitId < 0)
        {
            popupManager?.ShowAlert("유닛 정보를 불러오지 못했습니다.");
            return;
        }

        var preview = mainUI.GetPreviewUnit();
        if (preview == null || !preview.IsInitialized)
        {
            popupManager?.ShowAlert("유닛 데이터 로딩 중입니다.");
            return;
        }

        if (heroPopup != null)
            heroPopup.Open(baseUnitId, preview);
    }
}
