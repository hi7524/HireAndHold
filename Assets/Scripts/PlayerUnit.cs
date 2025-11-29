using System.Collections.Generic;
using UnityEngine;

public class PlayerUnit
{
    public int UnitID { get; private set; }
    public int Level { get; private set; }
    public int NormalEnforceLevel { get; private set; } = 0;
    public int UnitRank { get; private set; }
    public int HeroEnforceLevel { get; private set; } = 0;
    public float AttackMultiplier { get; private set; } = 0f;

    public int HeroEnforceGroupID;  // UnitTable에서 임시로 take 

    public int HeroIndex => HeroEnforceGroupID;

    private UnitData baseData;
    private DataTable_NormalEnforce normalEnforceTable;
    private DataTable_HeroEnforce heroEnforceTable;
    private DataTable_HeroEnforceEffect heroEffectTable;

    private HeroSkillEffects skillEffects = new HeroSkillEffects();

    // 임시 스킬들
    private Dictionary<int, TempSkill> skills = new Dictionary<int, TempSkill>();

    public PlayerUnit(
        UnitData baseData,
        DataTable_NormalEnforce normalEnforceTable,
        DataTable_HeroEnforce heroEnforceTable,
        DataTable_HeroEnforceEffect heroEffectTable)
    {
        this.baseData = baseData;
        this.normalEnforceTable = normalEnforceTable;
        this.heroEnforceTable = heroEnforceTable;
        this.heroEffectTable = heroEffectTable;

        this.UnitID = baseData.UNIT_ID;
        this.Level = baseData.LEVEL;
        this.UnitRank = baseData.RANK;

        this.HeroEnforceGroupID = baseData.UNIT_DESCRIPTION;

        InitializeTempSkills();
    }

    private void InitializeTempSkills()
    {
        skills[1] = new TempSkill(1);
        skills[2] = new TempSkill(2);
    }

    public TempSkill GetSkill(int skillId)
    {
        if (skills.TryGetValue(skillId, out var skill))
        {
            return skill;
        }

        Debug.LogWarning($"스킬 ID {skillId} 없음.");
        return null;
    }

    public void AddNormalEnforceLevel()
    {
        NormalEnforceLevel++;
    }

    public void AddHeroEnforceLevel()
    {
        HeroEnforceLevel++;
    }

    public int CurrentAttack
    {
        get
        {
            int baseAttack = baseData.ATTACK;

            int normalEnforceBonus = 0;
            for (int lv = 1; lv <= NormalEnforceLevel; lv++)
            {
                int enforceId = CalculateNormalEnforceID(UnitRank, lv);
                if (normalEnforceTable.TryGet(enforceId, out var enforceData))
                {
                    normalEnforceBonus += Mathf.RoundToInt(enforceData.AttackUp);
                }
            }

            float heroAttackPercent = GetSkillEffects().TotalAttackUp / 100f;

            float finalAttack = (baseAttack + normalEnforceBonus) * (1 + heroAttackPercent);
            return Mathf.RoundToInt(finalAttack);
        }
    }


    public HeroSkillEffects GetSkillEffects()
    {
        return skillEffects;
    }

    private static int CalculateNormalEnforceID(int rank, int level)
    {
        int baseOffset = (rank - 1) * 20;
        int suffix = baseOffset + level;
        string idString = $"11{level}{rank}8{suffix:D2}";
        return int.Parse(idString);
    }

    public void SetRank(int newRank)
    {
        UnitRank = newRank;
    }

}

// 임시 테스트용
public class TempSkill
{
    public int SkillID { get; private set; }
    public float DamageMultiplier { get; set; } = 1f;
    public float Duration { get; set; } = 5f;
    public int ProjectileCount { get; set; } = 1;
    public float Cooldown { get; set; } = 10f;

    public TempSkill(int skillId)
    {
        this.SkillID = skillId;
    }
}

// 임시 테스트용
public class HeroSkillEffects
{
    public float TotalSkillDamageUp { get; private set; } = 0f;
    public float TotalDurationUp { get; private set; } = 0f;
    public int TotalProjectileUp { get; private set; } = 0;
    public float TotalCoolTimeDown { get; private set; } = 0f;

    public float TotalAttackUp { get; private set; } = 0f;

    public void AddEffect(DataTable_HeroEnforceEffect.HeroEnforceEffectData effectData)
    {
        if (effectData == null)
        {
            return;
        }

        TotalAttackUp += effectData.Attack_Up;
        TotalSkillDamageUp += effectData.Skill_Damage_Up;
        TotalDurationUp += effectData.Duration_Up;
        TotalProjectileUp += effectData.Projectile;
        TotalCoolTimeDown += effectData.CoolTime_Down;
    }

    public float GetFinalSkillDamage(float baseDamage)
    {
        return baseDamage * (1 + TotalSkillDamageUp / 100f);
    }

    public float GetFinalCooldown(float baseCooldown)
    {
        return baseCooldown * (1 - TotalCoolTimeDown / 100f);
    }

    public float GetFinalDuration(float baseDuration)
    {
        return baseDuration + TotalDurationUp;
    }

    public int GetFinalProjectileCount(int baseCount)
    {
        return baseCount + TotalProjectileUp;
    }
}
