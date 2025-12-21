using Cysharp.Threading.Tasks;
using GameData;
using UnityEngine;

public class HeroEnforceSystem
{
    private const int MAX_LEVEL = 4;
    private readonly BattleUnitManager battleUnitManager;
    private readonly DataTable_HeroEnforce table;
    private readonly DataTable_HeroEnforceEffect effectTable;

    public HeroEnforceSystem(
        BattleUnitManager battleUnitManager,
        DataTable_HeroEnforce table,
        DataTable_HeroEnforceEffect effectTable)
    {
        this.battleUnitManager = battleUnitManager;
        this.table = table;
        this.effectTable = effectTable;
    }

    private OwnedCharacter GetCharacter(Unit unit)
    {
        return DatabaseManager.Instance.GetCharacter(unit.BaseCharacterID.ToString());
    }

    public bool CanEnforce(Unit unit, out string reason)
    {
        var ch = GetCharacter(unit);
        int next = ch.heroEnforceLevel + 1;

        if (next > MAX_LEVEL)
        {
            reason = "최대 레벨";
            return false;
        }

        var row = table.Get(unit.BaseCharacterID, next);
        if (row == null)
        {
            reason = "데이터 없음";
            return false;
        }

        if (!PlayData.HasEnoughGold(row.Gold_Cost))
        {
            reason = "골드 부족";
            return false;
        }

        var unitData = unit.GetUnitData();
        int fragmentItemId = unitData.FRAGMENT_ITEM_ID;

        if (fragmentItemId <= 0)
        {
            reason = "조각 아이템 없음";
            return false;
        }

        if (!PlayData.HasEnoughItem(fragmentItemId, row.IngredientNum))
        {
            reason = "조각 부족";
            return false;
        }


        reason = "";
        return true;
    }

    public async UniTask<bool> TryEnforceAsync(Unit unit)
    {
        if (!CanEnforce(unit, out string reason))
        {
            return false;
        }

        var ch = GetCharacter(unit);
        int nextLv = ch.heroEnforceLevel + 1;
        var row = table.Get(unit.BaseCharacterID, nextLv);

        // 재화 차감
        await DatabaseManager.Instance.AddGoldAsync(-row.Gold_Cost);

        var unitData = unit.GetUnitData();
        int fragmentItemId = unitData.FRAGMENT_ITEM_ID;

        await DatabaseManager.Instance.AddItemAsync(
            fragmentItemId,
            -row.IngredientNum
        );

        // DB 저장
        ch.heroEnforceLevel = nextLv;
        await DatabaseManager.Instance.SaveCharacterAsync(ch.id);

        // 인게임 유닛에 적용 단일 레벨만 추가 적용
        var effect = effectTable.Get(row.Hero_Enforce_EffectID);
        ApplyEffectToUnit(unit, effect, nextLv);

        // 업적 연동: 영웅 강화 성공
        await AchievementManager.AddHeroUpgradeSuccessAsync(1);

        // 최대 레벨 달성 시 업적
        if (nextLv >= MAX_LEVEL)
            await AchievementManager.CompleteHeroUpgradeMaxAsync();

        return true;
    }

    private void ApplyEffectToUnit(Unit unit, DataTable_HeroEnforceEffect.HeroEnforceEffectData effect, int level)
    {
        if (effect == null)
        {
            Debug.LogError($"[HeroEnforce] effect is null");
            return;
        }

        // 스킬 매칭 검사
        int skill1 = unit.GetUnitData().UNIT_SKILL1;
        int skill2 = unit.GetUnitData().UNIT_SKILL2;

        bool skillMatched = false;

        if (effect.Attack_Up > 1f &&
            effect.SkillID1.GetValueOrDefault(0) == 0 &&
            effect.SkillID2.GetValueOrDefault(0) == 0)
        {
            skillMatched = true;
        }
        else if (effect.SkillID1.GetValueOrDefault(0) == 0 &&
                 effect.SkillID2.GetValueOrDefault(0) == 0)
        {
            skillMatched = true;
        }
        else
        {
            int effectSkill1 = effect.SkillID1.GetValueOrDefault(0);
            int effectSkill2 = effect.SkillID2.GetValueOrDefault(0);

            if ((effectSkill1 > 0 && (effectSkill1 == skill1 || effectSkill1 == skill2)) ||
                (effectSkill2 > 0 && (effectSkill2 == skill1 || effectSkill2 == skill2)))
            {
                skillMatched = true;
            }
        }

        if (!skillMatched)
        {
            return;
        }

        // 효과 적용
        if (effect.Attack_Up > 1f)
        {
            unit.AddHeroAttackMultiplier(effect.Attack_Up);
        }

        if (effect.Skill_Damage_Up > 1f)
        {
            unit.AddHeroSkillDamageMultiplier(effect.Skill_Damage_Up);
        }

        if (effect.Duration_Up > 0f)
        {
            unit.AddHeroSkillDuration(effect.Duration_Up);
        }

        if (effect.Projectile > 0)
        {
            unit.AddHeroProjectileBonus(effect.Projectile);
        }

        if (effect.CoolTime_Down > 0f && effect.CoolTime_Down < 1f)
        {
            unit.AddHeroSkillCooltimeMultiplier(effect.CoolTime_Down);
        }
    }
}
