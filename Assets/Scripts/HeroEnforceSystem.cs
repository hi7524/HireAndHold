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
        if (unit == null)
        {
            Debug.LogError("[HeroEnforceSystem] GetCharacter: unit is null");
            return null;
        }

        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("[HeroEnforceSystem] GetCharacter: DatabaseManager.Instance is null");
            return null;
        }

        return DatabaseManager.Instance.GetCharacter(unit.BaseCharacterID.ToString());
    }

    public bool CanEnforce(Unit unit, out string reason)
    {
        // Unit null 체크
        if (unit == null)
        {
            reason = "유닛이 null입니다";
            Debug.LogError("[HeroEnforceSystem] CanEnforce: unit is null");
            return false;
        }

        var ch = GetCharacter(unit);

        // Character null 체크
        if (ch == null)
        {
            reason = "캐릭터 데이터 없음";
            Debug.LogWarning($"[HeroEnforceSystem] CanEnforce: character is null for unit {unit.BaseCharacterID}");
            return false;
        }

        int next = ch.heroEnforceLevel + 1;

        if (next > MAX_LEVEL)
        {
            reason = "최대 레벨";
            return false;
        }

        // table null 체크
        if (table == null)
        {
            reason = "강화 테이블이 null입니다";
            Debug.LogError("[HeroEnforceSystem] CanEnforce: table is null");
            return false;
        }

        var row = table.Get(unit.BaseCharacterID, next);
        if (row == null)
        {
            reason = "데이터 없음";
            Debug.LogWarning($"[HeroEnforceSystem] CanEnforce: row is null for BaseCharacterID {unit.BaseCharacterID}, level {next}");
            return false;
        }

        if (!PlayData.HasEnoughGold(row.Gold_Cost))
        {
            reason = "골드 부족";
            return false;
        }

        var unitData = unit.GetUnitData();

        // unitData null 체크
        if (unitData == null)
        {
            reason = "유닛 데이터가 null입니다";
            Debug.LogError($"[HeroEnforceSystem] CanEnforce: unitData is null for unit {unit.BaseCharacterID}");
            return false;
        }

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
            Debug.LogWarning($"[HeroEnforceSystem] TryEnforceAsync failed: {reason}");
            return false;
        }

        var ch = GetCharacter(unit);
        if (ch == null)
        {
            Debug.LogError("[HeroEnforceSystem] TryEnforceAsync: character is null");
            return false;
        }

        int nextLv = ch.heroEnforceLevel + 1;

        if (table == null)
        {
            Debug.LogError("[HeroEnforceSystem] TryEnforceAsync: table is null");
            return false;
        }

        var row = table.Get(unit.BaseCharacterID, nextLv);
        if (row == null)
        {
            Debug.LogError($"[HeroEnforceSystem] TryEnforceAsync: row is null for BaseCharacterID {unit.BaseCharacterID}, level {nextLv}");
            return false;
        }

        var unitData = unit.GetUnitData();
        if (unitData == null)
        {
            Debug.LogError($"[HeroEnforceSystem] TryEnforceAsync: unitData is null");
            return false;
        }

        int fragmentItemId = unitData.FRAGMENT_ITEM_ID;


        await UniTask.WhenAll(
            DatabaseManager.Instance.AddGoldAsync(-row.Gold_Cost),
            DatabaseManager.Instance.AddItemAsync(fragmentItemId, -row.IngredientNum)
        );

        ch.heroEnforceLevel = nextLv;
        await DatabaseManager.Instance.SaveCharacterAsync(ch.id);

        if (effectTable != null)
        {
            var effect = effectTable.Get(row.Hero_Enforce_EffectID);
            ApplyEffectToUnit(unit, effect, nextLv);
        }
        else
        {
            Debug.LogWarning("[HeroEnforceSystem] TryEnforceAsync: effectTable is null");
        }

        AchievementManager.AddHeroUpgradeSuccessAsync(1).Forget();

        if (nextLv >= MAX_LEVEL)
            AchievementManager.CompleteHeroUpgradeMaxAsync().Forget();

        Debug.Log($"[HeroEnforceSystem] 영웅 강화 성공: {unit.BaseCharacterID} LV {nextLv}");
        return true;
    }

    private void ApplyEffectToUnit(Unit unit, DataTable_HeroEnforceEffect.HeroEnforceEffectData effect, int level)
    {
        if (unit == null)
        {
            Debug.LogError("[HeroEnforceSystem] ApplyEffectToUnit: unit is null");
            return;
        }

        if (effect == null)
        {
            Debug.LogError($"[HeroEnforceSystem] ApplyEffectToUnit: effect is null");
            return;
        }

        var unitData = unit.GetUnitData();
        if (unitData == null)
        {
            Debug.LogError($"[HeroEnforceSystem] ApplyEffectToUnit: unitData is null");
            return;
        }

        // 스킬 매칭 검사
        int skill1 = unitData.UNIT_SKILL1;
        int skill2 = unitData.UNIT_SKILL2;

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
