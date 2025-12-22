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

    private int currentUnitId;
    private Unit currentPreviewUnit;

    private const int NORMAL_MAX = 20;
    private DataTable_Unit unitTable;


    private void Start()
    {
        popupRoot.SetActive(false);
        closeButton.onClick.AddListener(() => popupRoot.SetActive(false));
        confirmButton.onClick.AddListener(() => OnConfirmClicked().Forget());
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
        popupRoot.SetActive(true);

        currentUnitId = unitId;
        currentPreviewUnit = previewUnit;

        LoadUnitIcon().Forget();
        RefreshUI();
    }


    private async UniTaskVoid LoadUnitIcon()
    {
        if (unitImage == null || unitTable == null)
            return;

        var data = unitTable.Get(currentUnitId);
        if (data == null)
            return;

        try
        {
            var sprite = await Addressables
                .LoadAssetAsync<Sprite>(data.UNIT_ICON)
                .Task;

            unitImage.sprite = sprite;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NormalEnforcePopup] Icon load failed: {e}");
        }
    }


    private void RefreshUI()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null) return;

        int lv = character.enforceLevel;

        levelCurrent.text = lv.ToString();
        levelNext.text = lv < NORMAL_MAX ? (lv + 1).ToString() : "MAX";

        float currAtk = currentPreviewUnit.GetAttackDamageStat().Value;
        float nextAtk = enforceSystem.GetNextAttack(currentPreviewUnit);

        var (goldCost, stoneCost) = enforceSystem.GetNextEnforceCost(currentPreviewUnit);

        powerCurrent.text = currAtk.ToString();
        powerNext.text = lv < NORMAL_MAX ? nextAtk.ToString() : "-";

        stoneHave.text = PlayData.EnhanceStone.ToString();
        stoneNeed.text = stoneCost.ToString();

        goldHave.text = PlayData.Gold.ToString();
        goldNeed.text = goldCost.ToString();
    }

    private async UniTaskVoid OnConfirmClicked()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
        {
            popupManager?.ShowAlert("미보유 유닛입니다.");
            return;
        }

        int beforeLv = character.enforceLevel;
        float beforeAtk = currentPreviewUnit.GetAttackDamageStat().Value;

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

    private void RefreshCostUI()
    {
        if (!popupRoot.activeSelf) return;
        if (currentPreviewUnit == null) return;

        var (goldCost, stoneCost) = enforceSystem.GetNextEnforceCost(currentPreviewUnit);

        goldHave.text = PlayData.Gold.ToString();
        goldNeed.text = goldCost.ToString();

        stoneHave.text = PlayData.EnhanceStone.ToString();
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
