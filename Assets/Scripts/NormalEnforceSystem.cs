using UnityEngine;

public class NormalEnforceSystem
{
    private const int MAX_ENFORCE_LEVEL = 20;

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

        if (!TryGetEnforceData(unit.UnitRank, nextLevel, out var data))
        {
            reason = $"CSV 데이터 x Class {unit.UnitRank}, LV {nextLevel}";
            return false;
        }

        if (TempGold < data.Gold_Cost)
        {
            reason = "골드 부족";
            return false;
        }

        if (TempMaterial < Mathf.RoundToInt(data.IngredientNum))
        {
            reason = "재료 부족";
            return false;
        }

        reason = "";
        return true;
    }

    public bool TryEnforce(int unitId)
    {
        if (!CanEnforce(unitId, out string reason))
        {
            Debug.Log("강화 실패: " + reason);
            return false;
        }

        var unit = unitManager.GetPlayerUnit(unitId);
        int nextLevel = unit.NormalEnforceLevel + 1;

        TryGetEnforceData(unit.UnitRank, nextLevel, out var data);


        TempGold -= data.Gold_Cost;
        TempMaterial -= Mathf.RoundToInt(data.IngredientNum); 


        unit.AddNormalEnforceLevel();

        if (data.Class > 0 && data.Class != unit.UnitRank)
        {
            unit.SetRank(data.Class);
            Debug.Log($"등급 상승 {unit.UnitRank}");
        }

        //Debug.Log($"강화  Level {unit.NormalEnforceLevel} / +ATK {data.AttackUp}");
        return true;
    }

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

        if (!TryGetEnforceData(unit.UnitRank, nextLevel, out var data))
        {
            return (0, 0);
        }

        return (data.Gold_Cost, Mathf.RoundToInt(data.IngredientNum)); 
    }
}
