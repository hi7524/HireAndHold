using Cysharp.Threading.Tasks;
using GameData;
using TMPro;
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
        // 이미 활성화되어 있으면 끄지 않음 (Open이 먼저 호출된 경우)
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
        // 혹시 Start가 안 불렸을 경우를 대비
        if (closeButton != null && closeButton.onClick.GetPersistentEventCount() == 0)
            SetupPopup();

        if (popupRoot != null)
            popupRoot.SetActive(true);

        currentUnitId = unitId;
        currentPreviewUnit = previewUnit;

        LoadUnitIconAsync().Forget();
        RefreshUI();
    }

    private async UniTaskVoid LoadUnitIconAsync()
    {
        if (unitImage == null || unitTable == null)
            return;

        var data = unitTable.Get(currentUnitId);
        if (data == null)
            return;

        try
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(data.UNIT_ICON).Task;
            if (unitImage != null)
                unitImage.sprite = sprite;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NormalEnforcePopup] Icon load failed: {ex}");
        }
    }

    private void RefreshUI()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null || currentPreviewUnit == null)
            return;

        int lv = character.enforceLevel; // 0부터 시작

        if (levelCurrent != null)
            levelCurrent.text = lv.ToString();

        if (levelNext != null)
            levelNext.text = lv < NORMAL_MAX ? (lv + 1).ToString() : "MAX";

        float currAtk = currentPreviewUnit.GetAttackDamageStat().Value;

        if (powerCurrent != null)
            powerCurrent.text = currAtk.ToString("F1");

        // 다음 공격력 계산
        if (lv < NORMAL_MAX)
        {
            float nextAtk = enforceSystem.GetNextAttack(currentPreviewUnit);
            if (powerNext != null)
                powerNext.text = nextAtk.ToString("F1");
        }
        else
        {
            if (powerNext != null)
                powerNext.text = "-";
        }

        var (goldCost, stoneCost) = enforceSystem.GetNextEnforceCost(currentPreviewUnit);

        if (stoneHave != null)
            stoneHave.text = PlayData.EnhanceStone.ToString();

        if (stoneNeed != null)
            stoneNeed.text = stoneCost.ToString();

        if (goldHave != null)
            goldHave.text = PlayData.Gold.ToString();

        if (goldNeed != null)
            goldNeed.text = goldCost.ToString();
    }

    private void OnConfirmClicked()
    {
        TryEnforce().Forget();
    }

    private async UniTaskVoid TryEnforce()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
        {
            popupManager?.ShowAlert("미보유 유닛입니다.");
            return;
        }

        if (currentPreviewUnit == null)
        {
            popupManager?.ShowAlert("유닛 데이터를 불러올 수 없습니다.");
            return;
        }

        int beforeLv = character.enforceLevel;
        float beforeAtk = currentPreviewUnit.GetAttackDamageStat().Value;

        try
        {
            bool ok = await enforceSystem.TryEnforceAsync(currentPreviewUnit);

            if (!ok)
            {
                popupManager?.ShowAlert("재료가 부족합니다.");
                return;
            }

            int afterLv = character.enforceLevel;
            float afterAtk = currentPreviewUnit.GetAttackDamageStat().Value;

            popupManager?.ShowSuccess(
                "강화 성공",
                $"레벨 {beforeLv} → {afterLv}\n전투력 {beforeAtk} → {afterAtk}"
            );

            mainUI?.RefreshUI();
            RefreshUI();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NormalEnforcePopup] 강화 실패: {ex}");
            popupManager?.ShowAlert("강화 중 오류가 발생했습니다.");
        }
    }

    private void RefreshCostUI()
    {
        if (popupRoot == null || !popupRoot.activeSelf)
            return;

        if (currentPreviewUnit == null)
            return;

        var (goldCost, stoneCost) = enforceSystem.GetNextEnforceCost(currentPreviewUnit);

        if (goldHave != null)
            goldHave.text = PlayData.Gold.ToString();

        if (goldNeed != null)
            goldNeed.text = goldCost.ToString();

        if (stoneHave != null)
            stoneHave.text = PlayData.EnhanceStone.ToString();

        if (stoneNeed != null)
            stoneNeed.text = stoneCost.ToString();
    }

    private void OnEnable()
    {
        PlayData.OnCurrencyChanged += RefreshCostUI;
    }

    private void OnDisable()
    {
        PlayData.OnCurrencyChanged -= RefreshCostUI;
    }
}
