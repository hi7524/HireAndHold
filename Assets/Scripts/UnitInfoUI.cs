using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInfoUI : MonoBehaviour
{
    [Header("Unit Info Text")]
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI unitInfoText;
    public TextMeshProUGUI resourceText;

    public TextMeshProUGUI normalLevelText;
    public TextMeshProUGUI normalCostText;
    public TextMeshProUGUI classText;
    public Button normalEnforceButton;

    public TextMeshProUGUI heroLevelText;
    public TextMeshProUGUI heroCostText;
    public TextMeshProUGUI heroEffectsText;
    public Button heroEnforceButton;

    [Header("Normal Enforce UI")]

    public TextMeshProUGUI currentLevel;
    public TextMeshProUGUI nextLevel;
    public TextMeshProUGUI stonecost;
    public TextMeshProUGUI goldCost;
    public TextMeshProUGUI unitName;
    public TextMeshProUGUI currentAttack;
    public TextMeshProUGUI nextAttack;

    // 데이터 로드

    private DataTable_Unit unitTable;
    private DataTable_NormalEnforce normalEnforceTable;
    private DataTable_HeroEnforce heroEnforceTable;
    private DataTable_HeroEnforceEffect heroEffectTable;

    private UnitManager unitManager;
    private NormalEnforceSystem normalEnforceSystem;
    private HeroEnforceSystem heroEnforceSystem;

    private int currentUnitId = -1;

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

        normalEnforceButton.onClick.AddListener(OnClick_NormalEnforce);
        heroEnforceButton.onClick.AddListener(OnClick_HeroEnforce);
    }

    public void SetUnitManager(UnitManager manager)
    {
        this.unitManager = manager;
        this.normalEnforceSystem = new NormalEnforceSystem(manager, normalEnforceTable);
        this.heroEnforceSystem = new HeroEnforceSystem(manager, heroEnforceTable, heroEffectTable);

        normalEnforceSystem.TempGold = 999999;
        normalEnforceSystem.TempMaterial = 9999;
        heroEnforceSystem.TempGold = 999999;
    }

    public void SetUnit(int unitId)
    {
        currentUnitId = unitId;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (currentUnitId < 0 || unitManager == null)
        {
            return;
        }

        PlayerUnit unit = unitManager.GetPlayerUnit(currentUnitId);

        if (unit == null)
        {
            return;
        }


        attackText.text = $"공격력: {unit.CurrentAttack}";
        currentAttack.text = $"{unit.CurrentAttack}";
        int nextA = unit.CurrentAttack + 1;

        nextAttack.text = $"{nextA}";
        unitInfoText.text = $"유닛 ID: {unit.UnitID}\n랭크: {unit.UnitRank}";
        classText.text = $"등급: {unit.UnitRank}";
        unitName.text = $"{unit.UnitID}";

        UpdateNormalEnforceUI(unit);
        UpdateHeroEnforceUI(unit);
    }

    private void UpdateNormalEnforceUI(PlayerUnit unit)
    {
        normalLevelText.text = $"레벨:\n {unit.NormalEnforceLevel} / 20";
        currentLevel.text = $"{unit.NormalEnforceLevel}";

        int nextLv = unit.NormalEnforceLevel + 1;
        if (nextLv > 20)
        {
            nextLevel.text = "MAX";
        }
        else
        {
            nextLevel.text = $"{nextLv}";
        }

        var (gold, mat) = normalEnforceSystem.GetNextEnforceCost(currentUnitId);

        if (gold > 0)
        {
            normalCostText.text = $"다음 강화 비용\n골드: {gold} / 재료: {mat}";
            goldCost.text = $"{gold}/{normalEnforceSystem.TempGold}";
            stonecost.text = $"{mat}/{normalEnforceSystem.TempMaterial}";
        }
        else
        {
            normalCostText.text = "최대 레벨 도달";
        }

        normalEnforceButton.interactable = normalEnforceSystem.CanEnforce(currentUnitId, out _);
    }


    private void UpdateHeroEnforceUI(PlayerUnit unit)
    {
        heroLevelText.text = $"영웅 강화 레벨: {unit.HeroEnforceLevel} / 4";

        var (gold, frag) = heroEnforceSystem.GetNextEnforceCost(currentUnitId);

        if (gold > 0)
        {
            heroCostText.text = $"다음 강화 비용\n골드: {gold} / 조각: {frag}";
        }
        else
        {
            heroCostText.text = "최대 레벨 도달";
        }

        var effects = heroEnforceSystem.GetCurrentEffects(currentUnitId);
        if (effects.Count > 0)
        {
            string txt = "적용된 효과:\n";
            foreach (var e in effects)
            {
                txt += $"• {e.Enforce_Effect_DESCRIPTION}\n";
            }
            heroEffectsText.text = txt;
        }
        else
        {
            heroEffectsText.text = "적용된 효과 없음";
        }

        heroEnforceButton.interactable = heroEnforceSystem.CanEnforce(currentUnitId, out _);
    }

    public void OnClick_NormalEnforce()
    {
        normalEnforceSystem.TryEnforce(currentUnitId);
        UpdateUI();
    }

    public void OnClick_HeroEnforce()
    {
        heroEnforceSystem.TryEnforce(currentUnitId);
        UpdateUI();
    }

    public void OnClick_AddMaterial()
    {
        normalEnforceSystem.TempMaterial += 50;
        UpdateUI();
    }

    public void OnClick_AddGold()
    {
        normalEnforceSystem.TempGold += 10000;
        heroEnforceSystem.TempGold += 10000;
        UpdateUI();
    }

    public void OnClick_AddFragment()
    {
        if (!heroEnforceSystem.TempUnitFragments.ContainsKey(currentUnitId))
        {
            heroEnforceSystem.TempUnitFragments[currentUnitId] = 0;
        }

        heroEnforceSystem.TempUnitFragments[currentUnitId] += 10;
        UpdateUI();
    }
}
