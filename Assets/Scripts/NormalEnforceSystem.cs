using UnityEngine;

public class NormalEnforceSystem
{
    private const int MAX_ENFORCE_LEVEL = 20;
    private const int BASE_OFFSET_PER_RANK = 20;

    private readonly UnitManager unitManager;
    private readonly DataTable_NormalEnforce enforceTable;

    //임시 재화 
    public int TempGold = 999999;
    public int TempMaterial = 9999;

    public NormalEnforceSystem(UnitManager unitManager, DataTable_NormalEnforce enforceTable)
    {
        this.unitManager = unitManager;
        this.enforceTable = enforceTable;
    }

    /// id 계산 
    /// csv id 패턴: "11" + Level + Rank + "8" + BaseOffset + Level을 2자리로
    public static int CalculateEnforceID(int unitRank, int level)
    {
        int baseOffset = (unitRank - 1) * BASE_OFFSET_PER_RANK;
        int suffix = baseOffset + level;

        string idString = $"11{level}{unitRank}8{suffix:D2}";
        int result = int.Parse(idString);

        return result;
    }

    //여부 확인
    public bool CanEnforce(int unitId, out string reason)
    {
        var unit = unitManager.GetPlayerUnit(unitId);
        if (unit == null)
        {
            reason = "유닛 없음";
            return false;
        }

        int nextLevel = unit.NormalEnforceLevel + 1;

        if (nextLevel > MAX_ENFORCE_LEVEL)
        {
            reason = "최대 레벨 도달";
            return false;
        }

        int enforceId = CalculateEnforceID(unit.UnitRank, nextLevel);
        if (!enforceTable.TryGet(enforceId, out var enforceData))
        {
            reason = $"강화 데이터 없음 (ID: {enforceId})";
            Debug.LogError(reason);
            return false;
        }

        if (!HasEnoughResources(enforceData, out reason))
        {
            return false;
        }

        reason = string.Empty;
        return true;
    }

    /// try enforce
    public bool TryEnforce(int unitId)
    {
        if (!CanEnforce(unitId, out string reason))
        {
            Debug.Log($"강화 실패: {reason}");
            return false;
        }

        var unit = unitManager.GetPlayerUnit(unitId);
        int nextLevel = unit.NormalEnforceLevel + 1;
        int enforceId = CalculateEnforceID(unit.UnitRank, nextLevel);
        var enforceData = enforceTable.Get(enforceId);

        ConsumeResources(enforceData);
        unit.AddNormalEnforceLevel();

        Debug.Log($"강화 sucess Lv {unit.NormalEnforceLevel} / 공격력 + {enforceData.AttackUp}");
        return true;
    }

    /// 다음 재화 정보 update
    public (int gold, int material) GetNextEnforceCost(int unitId)
    {
        var unit = unitManager.GetPlayerUnit(unitId);
        if (unit == null)
        {
            return (0, 0);
        }

        int nextLevel = unit.NormalEnforceLevel + 1;
        if (nextLevel > MAX_ENFORCE_LEVEL)
        {
            return (0, 0);
        }

        int enforceId = CalculateEnforceID(unit.UnitRank, nextLevel);
        if (!enforceTable.TryGet(enforceId, out var data))
        {
            return (0, 0);
        }

        return (data.Gold_Cost, Mathf.RoundToInt(data.IngredientNum));
    }

    /// 재화 lacking information 
    private bool HasEnoughResources(DataTable_NormalEnforce.NormalEnforceData enforceData, out string reason)
    {
        if (TempGold < enforceData.Gold_Cost)
        {
            reason = "골드 부족";
            return false;
        }

        if (TempMaterial < enforceData.IngredientNum)
        {
            reason = "재료 부족";
            return false;
        }

        reason = string.Empty;

        return true;
    }

    // how much user spent? 
    private void ConsumeResources(DataTable_NormalEnforce.NormalEnforceData enforceData)
    {
        TempGold -= enforceData.Gold_Cost;
        TempMaterial -= Mathf.RoundToInt(enforceData.IngredientNum);
    }
}
