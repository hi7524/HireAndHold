using Cysharp.Threading.Tasks;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class HeroEnforcePopup : MonoBehaviour
{
    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Unit Display")]
    [SerializeField] private Image unitImage;

    [Header("Level Info")]
    [SerializeField] private TextMeshProUGUI levelCurrent;
    [SerializeField] private TextMeshProUGUI levelNext;

    [Header("Cost Info")]
    [SerializeField] private TextMeshProUGUI pieceHave;
    [SerializeField] private TextMeshProUGUI pieceNeed;
    [SerializeField] private TextMeshProUGUI goldHave;
    [SerializeField] private TextMeshProUGUI goldNeed;

    [Header("Effect List")]
    [SerializeField] private Transform effectListParent;
    [SerializeField] private GameObject effectItemPrefab;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    private HeroEnforceSystem enforceSystem;
    private DataTable_Unit unitTable;
    private DataTable_HeroEnforce heroTable;
    private DataTable_HeroEnforceEffect effectTable;
    private UnitInfoUI mainUI;
    private UIPopupManager popupManager;

    private int currentUnitId;
    private Unit currentPreviewUnit;

    private const int HERO_MAX = 4;

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
        HeroEnforceSystem system,
        DataTable_Unit uTable,
        DataTable_HeroEnforce hTable,
        DataTable_HeroEnforceEffect eTable,
        UnitInfoUI ui)
    {
        enforceSystem = system;
        unitTable = uTable;
        heroTable = hTable;
        effectTable = eTable;
        mainUI = ui;
    }
    public void Open(int unitId, Unit previewUnit)
    {
        popupRoot.SetActive(true);


        currentUnitId = unitId;
        currentPreviewUnit = previewUnit;

        if (currentPreviewUnit == null)
        {
            Debug.LogWarning("[HeroEnforcePopup] previewUnit is null");
            return;
        }

        var character = DatabaseManager.Instance.GetCharacter(unitId.ToString());
        if (character == null)
        {
            popupManager?.ShowAlert("미보유 유닛은 영웅강화가 불가합니다.");
            return;
        }

        int heroLv = character.heroEnforceLevel;
        if (heroLv >= HERO_MAX)
        {
            popupManager?.ShowAlert("최대 영웅강화입니다.");
            return;
        }

        RefreshUI();
        LoadUnitIcon().Forget();
    }


    private void RefreshUI()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null) return;

        int heroLv = character.heroEnforceLevel;
        int nextLv = heroLv + 1;
        var next = nextLv <= HERO_MAX ? heroTable.Get(currentUnitId, nextLv) : null;

        levelCurrent.text = $"★{heroLv}";
        levelNext.text = heroLv < HERO_MAX ? $"★{nextLv}" : "MAX";

        var unitData = unitTable.Get(currentUnitId);
        int fragmentItemId = unitData.FRAGMENT_ITEM_ID;

        int havePieces = PlayData.GetItemCount(fragmentItemId);
        int needPieces = (int)(next?.IngredientNum ?? 0);

        pieceHave.text = havePieces.ToString();
        pieceNeed.text = needPieces.ToString();

        goldHave.text = PlayData.Gold.ToString();
        goldNeed.text = (next?.Gold_Cost ?? 0).ToString();

        RefreshEffectList(heroLv);
    }

    private void RefreshEffectList(int heroLv)
    {
        foreach (Transform t in effectListParent)
            Destroy(t.gameObject);

        for (int lv = 1; lv <= HERO_MAX; lv++)
        {
            var e = heroTable.Get(currentUnitId, lv);
            if (e == null) continue;

            var eff = effectTable.Get(e.Hero_Enforce_EffectID);
            if (eff == null) continue;

            var go = Instantiate(effectItemPrefab, effectListParent);
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            string desc = effectTable.FormatEffect(eff);

            string status = lv <= heroLv ? "[활성]" : "[잠김]";
            txt.text = $"{status} LV {lv}: {desc}";
        }
    }

    private async UniTaskVoid OnConfirmClicked()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
        {
            popupManager?.ShowAlert("미보유 유닛입니다.");
            return;
        }

        int beforeLv = character.heroEnforceLevel;

        bool ok = await enforceSystem.TryEnforceAsync(currentPreviewUnit);
        if (!ok)
        {
            popupManager?.ShowAlert("재료 부족!");
            return;
        }

        await DatabaseManager.Instance.LoadUserDataAsync();
        PlayData.SyncFromDatabase();

        int afterLv = character.heroEnforceLevel;

        var ef = heroTable.Get(currentUnitId, afterLv);
        var effData = effectTable.Get(ef.Hero_Enforce_EffectID);
        var desc = effectTable.FormatEffect(effData);

        popupManager?.ShowSuccess(
            "영웅 강화 성공!",
            $"★{beforeLv} → ★{afterLv}\n효과: {desc}"
        );

        mainUI?.RefreshUI();
        RefreshUI();
    }

    private void RefreshCostUI()
    {
        if (!popupRoot.activeSelf) return;
        if (currentPreviewUnit == null) return;

        goldHave.text = PlayData.Gold.ToString();

        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null) return;

        int heroLv = character.heroEnforceLevel;
        var next = heroTable.Get(currentUnitId, heroLv + 1);

        goldNeed.text = (next?.Gold_Cost ?? 0).ToString();

        var unitData = unitTable.Get(currentUnitId);
        int fragmentItemId = unitData.FRAGMENT_ITEM_ID;

        pieceHave.text = PlayData.GetItemCount(fragmentItemId).ToString();
        pieceNeed.text = ((int)(next?.IngredientNum ?? 0)).ToString();
    }

    private void OnEnable()
    {
        PlayData.OnCurrencyChanged += RefreshCostUI;
    }

    private void OnDisable()
    {
        PlayData.OnCurrencyChanged -= RefreshCostUI;
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
            Debug.LogError($"[HeroEnforcePopup] Icon load failed: {e}");
        }
    }

}
