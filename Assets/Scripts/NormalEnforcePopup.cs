using Cysharp.Threading.Tasks;
using GameData;
using TMPro;
using Tutorial;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class NormalEnforcePopup : MonoBehaviour
{
    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Unit Display")]
    [SerializeField] private Image unitImage;

    [Header("Level Info")]
    [SerializeField] private TextMeshProUGUI levelCurrent;
    [SerializeField] private TextMeshProUGUI levelNext;
    [SerializeField] private TextMeshProUGUI powerCurrent;
    [SerializeField] private TextMeshProUGUI powerNext;

    [Header("Cost Info")]
    [SerializeField] private TextMeshProUGUI stoneHave;
    [SerializeField] private TextMeshProUGUI stoneNeed;
    [SerializeField] private TextMeshProUGUI goldHave;
    [SerializeField] private TextMeshProUGUI goldNeed;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("Success Effect")]
    [SerializeField] private EnforceSuccessEffect successEffect; // ⭐ 추가

    private NormalEnforceSystem enforceSystem;
    private UnitInfoUI mainUI;
    private UIPopupManager popupManager;
    private DataTable_Unit unitTable;

    private int currentUnitId;
    private Unit currentPreviewUnit;

    private const int NORMAL_MAX = 20;

    private void Start()
    {
        SetupPopup();
    }

    private void SetupPopup()
    {
        if (popupRoot != null && !popupRoot.activeSelf)
            popupRoot.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => popupRoot.SetActive(false));
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
    }

    public void SetPopupManager(UIPopupManager manager)
    {
        popupManager = manager;
    }

    public void SetEnforceSystem(
        NormalEnforceSystem system,
        UnitInfoUI ui,
        DataTable_Unit uTable)
    {
        enforceSystem = system;
        mainUI = ui;
        unitTable = uTable;
    }

    public void Open(int unitId, Unit previewUnit)
    {
        if (closeButton != null && closeButton.onClick.GetPersistentEventCount() == 0)
            SetupPopup();

        if (previewUnit == null || !previewUnit.IsInitialized)
        {
            Debug.LogError("[NormalEnforcePopup] previewUnit not ready");
            return;
        }

        if (popupRoot != null)
            popupRoot.SetActive(true);

        currentUnitId = unitId;
        currentPreviewUnit = previewUnit;

        RefreshUI();
    }


    private void RefreshUI()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null || currentPreviewUnit == null)
            return;

        var data = unitTable.Get(currentUnitId);
        if (data != null && unitImage != null)
        {
            var sp = SpriteCache.Instance.GetCachedSpriteOrNull(data.UNIT_ICON);
            if (sp != null) unitImage.sprite = sp;
        }

        int lv = character.enforceLevel;

        levelCurrent.text = lv.ToString();
        levelNext.text = lv < NORMAL_MAX ? (lv + 1).ToString() : "MAX";

        float currAtk = currentPreviewUnit.GetAttackDamageStat().Value;
        powerCurrent.text = currAtk.ToString();

        powerNext.text = lv < NORMAL_MAX
            ? enforceSystem.GetNextAttack(currentPreviewUnit).ToString()
            : "-";

        var (goldCost, stoneCost) = enforceSystem.GetNextEnforceCost(currentPreviewUnit);

        goldHave.text = PlayData.Gold.ToString();
        goldNeed.text = goldCost.ToString();
        stoneHave.text = PlayData.EnhanceStone.ToString();
        stoneNeed.text = stoneCost.ToString();
    }


    private void OnConfirmClicked()
    {
        // 튜토리얼에 버튼 클릭 알림 (TutorialTarget 리스너가 RemoveAllListeners로 삭제되므로 여기서 직접 호출)
        TutorialManager.Instance?.NotifyButtonTouched("EnhanceButton");

        _ = TryEnforceAsync();
    }

    private async UniTask TryEnforceAsync()
    {
        if (currentPreviewUnit == null || !currentPreviewUnit.IsInitialized)
        {
            popupManager?.ShowAlert("유닛 데이터 준비 중입니다.");
            return;
        }

        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
        {
            popupManager?.ShowAlert("미보유 유닛입니다.");
            return;
        }

        int beforeLv = character.enforceLevel;
        int beforeAtk = Mathf.RoundToInt(currentPreviewUnit.GetAttackDamageStat().Value);

        bool ok = await enforceSystem.TryEnforceAsync(currentPreviewUnit);
        if (!ok)
        {
            popupManager?.ShowAlert("재료가 부족합니다.");
            return;
        }

        PlayData.SyncCharactersFromDatabase();
        PlayData.NotifyCharacterUpdated(currentUnitId.ToString());

        character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());

        int afterLv = character.enforceLevel;
        int afterAtk = Mathf.RoundToInt(currentPreviewUnit.GetAttackDamageStat().Value);

        successEffect?.PlayEffect().Forget();

        popupManager?.ShowSuccess(
            "강화 성공",
            $"레벨 {beforeLv} → {afterLv}\n전투력 {beforeAtk} → {afterAtk}"
        );

        mainUI?.RefreshUI();
        RefreshUI();
    }


    private void RefreshCostUI()
    {
        if (popupRoot == null || !popupRoot.activeSelf)
            return;

        if (currentPreviewUnit == null)
            return;

        var (goldCost, stoneCost) = enforceSystem.GetNextEnforceCost(currentPreviewUnit);

        long currentGold = PlayData.Gold;
        int currentStone = PlayData.EnhanceStone;

        Debug.Log($"[NormalEnforcePopup] RefreshCostUI: 보유 골드={currentGold}, 필요 골드={goldCost}, 보유 강화석={currentStone}, 필요 강화석={stoneCost}");

        if (goldHave != null)
            goldHave.text = currentGold.ToString();

        if (goldNeed != null)
            goldNeed.text = goldCost.ToString();

        if (stoneHave != null)
            stoneHave.text = currentStone.ToString();

        if (stoneNeed != null)
            stoneNeed.text = stoneCost.ToString();
    }

    private void OnEnable()
    {
        PlayData.OnCurrencyChanged += RefreshCostUI;
        Debug.Log("[NormalEnforcePopup] OnEnable: 재화 변경 이벤트 구독");
    }

    private void OnDisable()
    {
        PlayData.OnCurrencyChanged -= RefreshCostUI;
        Debug.Log("[NormalEnforcePopup] OnDisable: 재화 변경 이벤트 구독 해제");
    }
}
