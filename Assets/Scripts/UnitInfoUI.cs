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

    private DataTable_Unit unitTable;
    private DataTable_NormalEnforce normalEnforceTable;

    private NormalEnforceSystem normalEnforceSystem;

    private int currentUnitId = -1;
    private Unit previewUnit;  // 전투 유닛 객체

    private async void Start()
    {
        unitTable = new DataTable_Unit();
        normalEnforceTable = new DataTable_NormalEnforce();

        await unitTable.LoadAsync("UnitTable");
        await normalEnforceTable.LoadAsync("NormalEnforceTable");
    }

    public void SetUnitManager( BattleUnitManager battleManager)
    {

        normalEnforceSystem = new NormalEnforceSystem(
            battleManager,
            normalEnforceTable,
            unitTable
        );

        normalEnforceButton.onClick.AddListener(OnClick_NormalEnforce);
    }

    public void SetUnit(int unitId)
    {
        currentUnitId = unitId;
        CreatePreviewUnit();
        UpdateUI();
    }

    private async void CreatePreviewUnit()
    {
        var go = new GameObject("PreviewUnit");
        previewUnit = go.AddComponent<Unit>();
        previewUnit.SetUnitID(currentUnitId);

        await UniTask.DelayFrame(1); 
    }

    private async void UpdateUI()
    {
        if (currentUnitId < 0 || previewUnit == null) return;

        // 유닛 스탯 로드 기다리기
        await UniTask.DelayFrame(2);

        OwnedCharacter character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        UnitData data = unitTable.Get(currentUnitId);

        if (data == null)
        {
            Debug.LogError("UnitData 없음: " + currentUnitId);
            return;
        }

        // 보유 재화 표시
        playerGoldText.text = $"보유 골드: {PlayData.Gold}";
        playerStoneText.text = $"보유 강화석: {PlayData.EnhanceStone}";

        // 기본 정보
        unitName.text = data.StringName;
        classText.text = $"등급: {data.RANK}";

        attackText.text = $"공격력: {previewUnit.GetAttackDamageStat().Value}";
        currentAttack.text = previewUnit.GetAttackDamageStat().Value.ToString();

        LoadUnitImage(currentUnitId).Forget();

        // 강화 레벨
        int enforceLv = character.enforceLevel;
        currentLevel.text = enforceLv.ToString();
        normalLevelText.text = $"레벨: {enforceLv} / 20";

        int nextLv = enforceLv + 1;
        nextLevel.text = nextLv > 20 ? "MAX" : nextLv.ToString();

        // 강화 비용
        var (gold, stone) = normalEnforceSystem.GetNextEnforceCost(previewUnit);

        if (nextLv > 20 || gold == 0)
        {
            normalCostText.text = "최대 레벨 도달";
            goldCost.text = "-";
            //stoneCost.text = "-";
        }
        else
        {
            normalCostText.text = $"다음 강화 비용\n골드 {gold} / 강화석 {stone}";
            goldCost.text = gold.ToString();
            //stoneCost.text = stone.ToString();
        }

        // nextAttack 계산
        float nextAtk = previewUnit.GetAttackDamageStat().Value;
        int rank = data.RANK;

        if (NormalEnforceSystem.SharedTable != null)
        {
            foreach (var kv in NormalEnforceSystem.SharedTable.All)
            {
                var e = kv.Value;
                if (e.Class == rank && e.Normal_Enforce_LV == nextLv)
                {
                    nextAtk += e.AttackUp;
                    break;
                }
            }
        }

        nextAttack.text = nextAtk.ToString();

        // ▼ 버튼 활성화 여부
        normalEnforceButton.interactable = normalEnforceSystem.CanEnforce(previewUnit, out _);
    }



    public async void OnClick_NormalEnforce()
    {
        bool result = await normalEnforceSystem.TryEnforceAsync(previewUnit);

        if (result)
        {
            Debug.Log("강화 성공");
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
            Debug.LogError($"유닛 이미지 로드 실패: {data.UNIT_ICON}");
        }
    }
}
