using Cysharp.Threading.Tasks;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class UnitInfoUI : MonoBehaviour
{
    [Header("Unit Info Text")]
    public Image unitImage;
    public TextMeshProUGUI attackText;

    public TextMeshProUGUI normalLevelText;
    public TextMeshProUGUI normalCostText;
    public TextMeshProUGUI classText;

    public Button normalEnforceButton;

    [Header("Normal Enforce Detail")]
    public TextMeshProUGUI currentLevel;
    public TextMeshProUGUI nextLevel;
    public TextMeshProUGUI goldCost;
    public TextMeshProUGUI unitName;
    public TextMeshProUGUI currentAttack;
    public TextMeshProUGUI nextAttack;

    public TextMeshProUGUI playerGoldText;
    public TextMeshProUGUI playerStoneText;

    public Image enforceUnitImage;

    // 영웅 강화 UI

    [Header("Hero Enforce UI")]
    public TextMeshProUGUI heroLevelText;
    public TextMeshProUGUI heroNextLevelText;
    public TextMeshProUGUI heroCostText;
    public TextMeshProUGUI heroEffectText;
    public Button heroEnforceButton;

    private DataTable_Unit unitTable;
    private DataTable_NormalEnforce normalEnforceTable;
    private DataTable_HeroEnforce heroEnforceTable;
    private DataTable_HeroEnforceEffect heroEffectTable;

    private NormalEnforceSystem normalEnforceSystem;
    private HeroEnforceSystem heroEnforceSystem;

    [Header("Hero Enforce Stage List")]
    public Transform heroEffectListParent;
    public GameObject heroEffectItemPrefab;

    private BattleUnitManager battleUnitManager;

    [Header("Hero Progress Text")]
    public TextMeshProUGUI heroProgressText;

    public DeckControl deckControl;

    private int currentUnitId = -1;
    private Unit previewUnit;

    private async void Start()
    {
        unitTable = new DataTable_Unit();
        normalEnforceTable = new DataTable_NormalEnforce();
        heroEnforceTable = new DataTable_HeroEnforce();
        heroEffectTable = new DataTable_HeroEnforceEffect();

        await unitTable.LoadAsync("UnitTable");
        await normalEnforceTable.LoadAsync("NormalEnforceTable");
        await heroEnforceTable.LoadAsync("HeroEnforceTable");
        await heroEffectTable.LoadAsync("HeroEnforceEffectTable");
    }

    // BattleUnitManager 등록 (전투 중 강화 적용 가능)
 
    public void SetUnitManager(BattleUnitManager battleManager)
    {
        battleUnitManager = battleManager;

        normalEnforceSystem = new NormalEnforceSystem(
            battleManager,
            normalEnforceTable,
            unitTable
        );

        heroEnforceSystem = new HeroEnforceSystem(
            battleManager,
            heroEnforceTable,
            heroEffectTable
        );

        normalEnforceButton.onClick.AddListener(OnClick_NormalEnforce);
        heroEnforceButton.onClick.AddListener(() => OnClick_HeroEnforce().Forget());
    }


    // 유닛 변경

    public void SetUnit(int unitId)
    {
        currentUnitId = unitId;
        CreatePreviewUnit();
        UpdateUI();
    }

    private async void CreatePreviewUnit()
    {
        if (previewUnit != null)
            Destroy(previewUnit.gameObject);

        var go = new GameObject("PreviewUnit");
        previewUnit = go.AddComponent<Unit>();
        previewUnit.SetUnitID(currentUnitId);

        await UniTask.DelayFrame(1);
    }

    // UI 업데이트

    private async void UpdateUI()
    {
        if (currentUnitId < 0 || previewUnit == null) return;

        await UniTask.DelayFrame(1);

        OwnedCharacter character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        UnitData data = unitTable.Get(currentUnitId);

        if (data == null)
        {
            Debug.LogError("UnitData 없음: " + currentUnitId);
            return;
        }

        // 공통 UI
        playerGoldText.text = $"골드: {PlayData.Gold}";
        playerStoneText.text = $"강화석: {PlayData.EnhanceStone}";

        unitName.text = data.StringName;
        classText.text = $"등급: {data.RANK}";

        attackText.text = $"공격력: {previewUnit.GetAttackDamageStat().Value}";
        currentAttack.text = previewUnit.GetAttackDamageStat().Value.ToString();

        LoadUnitImage(currentUnitId).Forget();


        // NORMAL 강화 UI

        int enforceLv = character.enforceLevel;
        currentLevel.text = enforceLv.ToString();
        normalLevelText.text = $"레벨: {enforceLv} / 20";

        int nextLv = enforceLv + 1;
        nextLevel.text = nextLv > 20 ? "MAX" : nextLv.ToString();

        var (gold, stone) = normalEnforceSystem.GetNextEnforceCost(previewUnit);

        if (nextLv > 20)
        {
            normalCostText.text = "최대 레벨 도달";
            goldCost.text = "-";
        }
        else
        {
            normalCostText.text = $"다음 강화 비용\n골드 {gold} / 강화석 {stone}";
            goldCost.text = gold.ToString();
        }

        nextAttack.text = normalEnforceSystem.GetNextAttack(previewUnit).ToString();

        normalEnforceButton.interactable =
            normalEnforceSystem.CanEnforce(previewUnit, out _);


        // HERO 강화 UI

        int heroLv = character.heroEnforceLevel;
        int maxLv = 4;

        heroLevelText.text = $"★{heroLv}";
        heroNextLevelText.text = heroLv < maxLv ? $"★{heroLv + 1}" : "MAX";

        heroProgressText.text = $"{heroLv}/{maxLv}";

        foreach (Transform c in heroEffectListParent)
            Destroy(c.gameObject);

        for (int lv = 1; lv <= maxLv; lv++)
        {
            var enforceData = heroEnforceTable.Get(currentUnitId, lv);
            if (enforceData == null) continue;

            var effect = heroEffectTable.Get(enforceData.Hero_Enforce_EffectID);
            if (effect == null) continue;

            //var item = Instantiate(heroEffectItemPrefab, heroEffectListParent);
            //item.GetComponent<TextMeshProUGUI>().text =
            //    $"LV {lv}: {effect.Enforce_Effect_DESCRIPTION}";
        }
    }


    // 버튼 이벤트

    public async void OnClick_NormalEnforce()
    {
        bool result = await normalEnforceSystem.TryEnforceAsync(previewUnit);

        if (result)
            Debug.Log("일반 강화 성공");

        UpdateUI();
    }

    private async UniTaskVoid OnClick_HeroEnforce()
    {
        bool result = await heroEnforceSystem.TryEnforceAsync(previewUnit);

        if (result)
        {
            Debug.Log("영웅 강화 성공");
            await DatabaseManager.Instance.LoadUserDataAsync();
            PlayData.SyncFromDatabase();
            
        }

        UpdateUI();
    }

    private async UniTaskVoid LoadUnitImage(int unitId)
    {
        UnitData data = unitTable.Get(unitId);

        try
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(data.UNIT_ICON).Task;
            unitImage.sprite = sprite;
            enforceUnitImage.sprite = sprite;
        }
        catch
        {
            Debug.LogError("유닛 아이콘 로드 실패: " + data.UNIT_ICON);
        }
    }
}
