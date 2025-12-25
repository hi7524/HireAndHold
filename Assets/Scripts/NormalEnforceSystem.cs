using Cysharp.Threading.Tasks;
using UnityEngine;

public class NormalEnforceSystem
{
    private const int MAX_ENFORCE_LEVEL = 20;

    private readonly BattleUnitManager battleUnitManager;
    private readonly DataTable_NormalEnforce enforceTable;
    private readonly DataTable_Unit unitTable;
    public static DataTable_NormalEnforce SharedTable;

    public NormalEnforceSystem(BattleUnitManager battleUnitManager, DataTable_NormalEnforce enforceTable, DataTable_Unit unitTable)
    {
        this.battleUnitManager = battleUnitManager;
        this.enforceTable = enforceTable;
        this.unitTable = unitTable;
        SharedTable = enforceTable;
    }

    private bool TryGetEnforceData(int classNum, int level, out DataTable_NormalEnforce.NormalEnforceData result)
    {
        foreach (var kv in enforceTable.All)
        {
            var data = kv.Value;
            if (data.Class == classNum && data.Normal_Enforce_LV == level)
            {
                result = data;
                return true;
            }
        }
        result = null;
        return false;
    }

    private int GetUnitEnforceLevel(Unit unit)
    {
        string id = unit.UnitID.ToString();
        var character = DatabaseManager.Instance.GetCharacter(id);
        return character != null ? character.enforceLevel : 0;
    }

    private int GetUnitRank(Unit unit)
    {
        UnitData data = unitTable.Get(unit.UnitID);
        return data != null ? data.RANK : 1;
    }

    public bool CanEnforce(Unit unit, out string reason)
    {
        if (unit == null)
        {
            reason = "유닛 없음";
            return false;
        }

        int currentLevel = GetUnitEnforceLevel(unit);

        if (currentLevel >= MAX_ENFORCE_LEVEL)
        {
            reason = "최대 레벨 도달";
            return false;
        }

        int nextLevel = currentLevel + 1;
        int rank = GetUnitRank(unit);

        // 다음 레벨의 강화 데이터를 찾음 
        if (!TryGetEnforceData(rank, nextLevel, out var data))
        {
            reason = $"강화 데이터 없음: Rank {rank}, LV {nextLevel}";
            return false;
        }

        if (!PlayData.HasEnoughGold(data.Gold_Cost))
        {
            reason = $"골드 부족 (필요: {data.Gold_Cost}, 보유: {PlayData.Gold})";
            return false;
        }

        int requiredStone = Mathf.RoundToInt(data.IngredientNum);
        if (!PlayData.HasEnoughEnhanceStone(requiredStone))
        {
            reason = $"강화석 부족 (필요: {requiredStone}, 보유: {PlayData.EnhanceStone})";
            return false;
        }

        reason = "";
        return true;
    }

    public async UniTask<bool> TryEnforceAsync(Unit unit)
    {
        if (!CanEnforce(unit, out string reason))
        {
            Debug.LogWarning($"[NormalEnforceSystem] TryEnforceAsync failed: {reason}");
            return false;
        }

        int currentLevel = GetUnitEnforceLevel(unit);
        int nextLevel = currentLevel + 1;
        int rank = GetUnitRank(unit);

        if (!TryGetEnforceData(rank, nextLevel, out var data))
        {
            Debug.LogError($"[NormalEnforceSystem] TryEnforceAsync: No data found");
            return false;
        }

        Debug.Log($"[NormalEnforceSystem] 강화 시도: Level {currentLevel} → {nextLevel}, 골드 차감: {data.Gold_Cost}, 강화석 차감: {data.IngredientNum}");

        // PlayData를 통한 재화 차감 (내부에서 DB 동기화)
        await DatabaseManager.Instance.AddGoldAsync(-data.Gold_Cost);
        await DatabaseManager.Instance.AddEnhanceStoneAsync(-data.IngredientNum);

        Debug.Log($"[NormalEnforceSystem] 재화 차감 완료 - 남은 골드: {PlayData.Gold}, 남은 강화석: {PlayData.EnhanceStone}");

        // 유닛에 강화 적용
        ApplyEnforceToUnit(unit, data);
        await SaveUnitEnforceLevel(unit, nextLevel);

        Debug.Log($"[NormalEnforceSystem] 강화 완료: {unit.UnitID} Level {nextLevel}");

        // 업적 연동: 일반 강화 성공
        await AchievementManager.AddNormalUpgradeSuccessAsync(1);

        // 최대 레벨 달성 시 업적
        if (nextLevel >= MAX_ENFORCE_LEVEL)
            await AchievementManager.CompleteNormalUpgradeMaxAsync();

        return true;
    }

    private void ApplyEnforceToUnit(Unit unit, DataTable_NormalEnforce.NormalEnforceData data)
    {
        Stat attackStat = unit.GetAttackDamageStat();
        if (attackStat != null)
        {
            StatModifier modifier = new StatModifier(data.AttackUp, ModifierType.Flat);
            attackStat.AddModifier(modifier);
        }

        if (data.Class > 0)
        {
            int currentRank = GetUnitRank(unit);
            if (data.Class != currentRank)
            {
            }
        }
    }

    private async UniTask SaveUnitEnforceLevel(Unit unit, int newLevel)
    {
        string characterId = unit.UnitID.ToString();
        var character = DatabaseManager.Instance.GetCharacter(characterId);

        if (character != null)
        {
            character.enforceLevel = newLevel;
            await DatabaseManager.Instance.SaveCharacterAsync(characterId);
        }
    }

    public (long gold, int stone) GetNextEnforceCost(Unit unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("[NormalEnforceSystem] GetNextEnforceCost: unit is null");
            return (0, 0);
        }

        int currentLevel = GetUnitEnforceLevel(unit);

        if (currentLevel >= MAX_ENFORCE_LEVEL)
        {
            Debug.Log($"[NormalEnforceSystem] GetNextEnforceCost: Max level reached ({currentLevel})");
            return (0, 0);
        }

        int nextLevel = currentLevel + 1;
        int rank = GetUnitRank(unit);

        if (!TryGetEnforceData(rank, nextLevel, out var data))
        {
            Debug.LogError($"[NormalEnforceSystem] GetNextEnforceCost: No data found for rank {rank}, level {nextLevel}");
            return (0, 0);
        }

        Debug.Log($"[NormalEnforceSystem] GetNextEnforceCost: rank={rank}, currentLv={currentLevel}, nextLv={nextLevel}, gold={data.Gold_Cost}, stone={data.IngredientNum}");
        return (data.Gold_Cost, Mathf.RoundToInt(data.IngredientNum));
    }

    // 현재 보유 재화 
    public (long gold, int stone) GetCurrentCurrency()
    {
        return (PlayData.Gold, PlayData.EnhanceStone);
    }

    public float GetNextAttack(Unit unit)
    {
        // Unit null 체크
        if (unit == null)
        {
            Debug.LogWarning("[NormalEnforceSystem] GetNextAttack: unit is null");
            return 0;
        }

        var unitData = unit.GetUnitData();
        if (unitData == null)
        {
            Debug.LogWarning($"[NormalEnforceSystem] GetNextAttack: unitData is null for unit {unit.UnitID}");
            return 0;
        }

        // DatabaseManager null 체크
        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("[NormalEnforceSystem] GetNextAttack: DatabaseManager.Instance is null");
            return unitData.ATTACK;
        }

        // 기본 공격력
        float baseAtk = unitData.ATTACK;

        // 현재 강화 레벨
        var character = DatabaseManager.Instance.GetCharacter(unit.UnitID.ToString());

        // Character null 체크
        if (character == null)
        {
            Debug.LogWarning($"[NormalEnforceSystem] GetNextAttack: character is null for unit {unit.UnitID}");
            return baseAtk;
        }

        int currLv = character.enforceLevel;
        int rank = unitData.RANK;

        // SharedTable null 체크
        if (SharedTable == null)
        {
            Debug.LogError("[NormalEnforceSystem] GetNextAttack: SharedTable is null");
            return baseAtk;
        }

        if (SharedTable.All == null)
        {
            Debug.LogError("[NormalEnforceSystem] GetNextAttack: SharedTable.All is null");
            return baseAtk;
        }

        float totalAtkUp = 0f;

        // 1 ~ 현재 강화 레벨까지 누적 공격력
        try
        {
            foreach (var kv in SharedTable.All)
            {
                if (kv.Value == null) continue;

                var d = kv.Value;
                if (d.Class == rank && d.Normal_Enforce_LV <= currLv)
                    totalAtkUp += d.AttackUp;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NormalEnforceSystem] GetNextAttack: Error calculating totalAtkUp - {ex.Message}");
            return baseAtk;
        }

        float currAtk = baseAtk + totalAtkUp;

        // 다음 레벨 강화 데이터 찾기
        float nextAtkUp = 0f;
        try
        {
            foreach (var kv in SharedTable.All)
            {
                if (kv.Value == null) continue;

                var d = kv.Value;
                if (d.Class == rank && d.Normal_Enforce_LV == currLv + 1)
                {
                    nextAtkUp = d.AttackUp;
                    break;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NormalEnforceSystem] GetNextAttack: Error calculating nextAtkUp - {ex.Message}");
        }

        // 현재 공격력 + 다음 강화 효과
        return currAtk + nextAtkUp;
    }
}
