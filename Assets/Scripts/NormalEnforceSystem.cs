using Cysharp.Threading.Tasks;
using UnityEngine;

public class NormalEnforceSystem
{
    private const int MAX_ENFORCE_LEVEL = 20;

    private readonly BattleUnitManager battleUnitManager;
    private readonly DataTable_NormalEnforce enforceTable;
    private readonly DataTable_Unit unitTable;
    public static DataTable_NormalEnforce SharedTable;
    //임시 재화
    //public int TempGold = 999999;
    //public int TempMaterial = 9999;

    public NormalEnforceSystem(BattleUnitManager battleUnitManager, DataTable_NormalEnforce enforceTable,DataTable_Unit unitTable)
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
        int nextLevel = currentLevel + 1;

        if (nextLevel > MAX_ENFORCE_LEVEL)
        {
            reason = "최대 레벨 도달";
            return false;
        }

        int rank = GetUnitRank(unit);

        if (!TryGetEnforceData(rank, nextLevel, out var data))
        {
            reason = $"강화 데이터 없음: Rank {rank}, LV {nextLevel}";
            return false;
        }

        // PlayData 캐시에서 즉시 체크 (빠름!)
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
            Debug.Log($"강화 실패: {reason}");
            return false;
        }

        int currentLevel = GetUnitEnforceLevel(unit);
        int nextLevel = currentLevel + 1;
        int rank = GetUnitRank(unit);

        if (!TryGetEnforceData(rank, nextLevel, out var data))
        {
            return false;
        }

        // PlayData를 통한 재화 차감 (내부에서 DB 동기화)
        PlayData.AddGold(-data.Gold_Cost);

        int requiredStone = Mathf.RoundToInt(data.IngredientNum);
        PlayData.AddEnhanceStone(-requiredStone);

        // 유닛에 강화 적용
        ApplyEnforceToUnit(unit, data);
        await SaveUnitEnforceLevel(unit, nextLevel);

        Debug.Log($"강화 성공! Level {nextLevel}, +ATK {data.AttackUp}");
        Debug.Log($"남은 골드: {PlayData.Gold}, 남은 강화석: {PlayData.EnhanceStone}");

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
                Debug.Log($"등급 상승: {currentRank} -> {data.Class}");
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
        if (unit == null) return (0, 0);

        int currentLevel = GetUnitEnforceLevel(unit);
        int nextLevel = currentLevel + 1;

        if (nextLevel > MAX_ENFORCE_LEVEL) return (0, 0);

        int rank = GetUnitRank(unit);

        if (!TryGetEnforceData(rank, nextLevel, out var data))
        {
            return (0, 0);
        }

        return (data.Gold_Cost, Mathf.RoundToInt(data.IngredientNum));
    }

    // 현재 보유 재화 
    public (long gold, int stone) GetCurrentCurrency()
    {
        return (PlayData.Gold, PlayData.EnhanceStone);
    }

    public float GetNextAttack(Unit unit)
    {
        var data = unit.GetUnitData();
        if (data == null) return unit.GetAttackDamageStat().Value;

        int currentLv = DatabaseManager.Instance
            .GetCharacter(unit.UnitID.ToString()).enforceLevel;

        int nextLv = currentLv + 1;
        if (!SharedTable.All.TryGetValue(nextLv, out var enforceData))
            return unit.GetAttackDamageStat().Value;

        return unit.GetAttackDamageStat().Value + enforceData.AttackUp;
    }



}
