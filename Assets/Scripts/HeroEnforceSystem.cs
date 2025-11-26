using System.Collections.Generic;
using UnityEngine;
using static DataTable_HeroEnforce;
using static DataTable_HeroEnforceEffect;

public class HeroEnforceSystem
{
    private const int MAX_HERO_LEVEL = 4;

    private readonly UnitManager unitManager;
    private readonly DataTable_HeroEnforce heroEnforceTable;
    private readonly DataTable_HeroEnforceEffect effectTable;

    public int TempGold = 9999999;
    public Dictionary<int, float> TempUnitFragments = new Dictionary<int, float>();

    public HeroEnforceSystem(UnitManager unitManager, DataTable_HeroEnforce heroTable, DataTable_HeroEnforceEffect effectTable)
    {
        this.unitManager = unitManager;
        this.heroEnforceTable = heroTable;
        this.effectTable = effectTable;
    }

    public bool CanEnforce(int unitId, out string reason)
    {
        var unit = unitManager.GetPlayerUnit(unitId);

        if (unit == null)
        {
            reason = "유닛 없음";
            return false;
        }

        int nextLevel = unit.HeroEnforceLevel + 1;

        if (nextLevel > MAX_HERO_LEVEL)
        {
            reason = "최대 레벨 도달";
            return false;
        }

        reason = "";
        return true;
    }

    public bool TryEnforce(int unitId)
    {
        var unit = unitManager.GetPlayerUnit(unitId);
        int nextLevel = unit.HeroEnforceLevel + 1;

        var enforceData =
            heroEnforceTable.FindByLevelAndIndex(nextLevel, unit.HeroIndex);

        if (enforceData == null)
        {
            Debug.LogError($"[HeroEnforce] 데이터를 찾을 수 없음 → HeroIndex={unit.HeroIndex}, Level={nextLevel}");
            return false;
        }

        if (!HasEnoughResources(enforceData, unitId, out string reason))
        {
            Debug.Log($"영웅 강화 실패: {reason}");
            return false;
        }

        ConsumeResources(enforceData, unitId);
        unit.AddHeroEnforceLevel();

        var effectData = effectTable.Get(enforceData.Hero_Enforce_EffectID);
        ApplyHeroEffect(unit, effectData);

        return true;
    }

    private bool HasEnoughResources(HeroEnforceData data, int unitId, out string reason)
    {
        if (TempGold < data.Gold_Cost)
        {
            reason = "골드 부족";
            return false;
        }

        if (!TempUnitFragments.ContainsKey(unitId) ||
            TempUnitFragments[unitId] < data.IngredientNum)
        {
            reason = "유닛 조각 부족";
            return false;
        }

        reason = "";
        return true;
    }

    private void ConsumeResources(HeroEnforceData data, int unitId)
    {
        TempGold -= data.Gold_Cost;
        TempUnitFragments[unitId] -= data.IngredientNum;
    }

    private void ApplyHeroEffect(PlayerUnit unit, HeroEnforceEffectData effectData)
    {
        var effects = unit.GetSkillEffects();
        effects.AddEffect(effectData);

        if (effectData.SkillID1 > 0)
        {
            ApplySkillEffect(unit, effectData.SkillID1, effectData);
        }

        if (effectData.SkillID2 > 0)
        {
            ApplySkillEffect(unit, effectData.SkillID2, effectData);
        }
    }

    private void ApplySkillEffect(PlayerUnit unit, int skillId, HeroEnforceEffectData effectData)
    {
        var skill = unit.GetSkill(skillId);

        if (skill == null)
        {
            return;
        }


        skill.DamageMultiplier += effectData.Skill_Damage_Up / 100f;
        skill.Duration += effectData.Duration_Up;
        skill.ProjectileCount += effectData.Projectile;
        skill.Cooldown *= (1 - effectData.CoolTime_Down / 100f);

        unit.GetSkillEffects().AddEffect(effectData);
    }

    public (int gold, float fragments) GetNextEnforceCost(int unitId)
    {
        var unit = unitManager.GetPlayerUnit(unitId);
        int nextLevel = unit.HeroEnforceLevel + 1;

        var data = heroEnforceTable.FindByLevelAndIndex(nextLevel, unit.HeroIndex);
        if (data == null)
        {
            return (0, 0);
        }

        return (data.Gold_Cost, data.IngredientNum);
    }

    public List<HeroEnforceEffectData> GetCurrentEffects(int unitId)
    {
        var unit = unitManager.GetPlayerUnit(unitId);

        var list = new List<HeroEnforceEffectData>();

        for (int lv = 1; lv <= unit.HeroEnforceLevel; lv++)
        {
            var enforceData =
                heroEnforceTable.FindByLevelAndIndex(lv, unit.HeroIndex);

            if (enforceData != null)
            {
                list.Add(effectTable.Get(enforceData.Hero_Enforce_EffectID));
            }
        }

        return list;
    }
}
