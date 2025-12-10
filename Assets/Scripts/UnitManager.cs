using System.Collections.Generic;
using UnityEngine;

public class UnitManager
{
    //임시 스크립트 (데이터 옮겨야함)
    private readonly DataTable_Unit unitTable;
    private readonly DataTable_NormalEnforce normalEnforceTable;
    private readonly DataTable_HeroEnforce heroEnforceTable;
    private readonly DataTable_HeroEnforceEffect heroEnforceEffectTable;
    private readonly Dictionary<int, PlayerUnit> ownedUnits = new Dictionary<int, PlayerUnit>();

    public UnitManager(DataTable_Unit unitTable, DataTable_NormalEnforce normalEnforceTable, DataTable_HeroEnforce heroEnforceTable, DataTable_HeroEnforceEffect heroEnforceEffectTable)
    {
        this.unitTable = unitTable;
        this.normalEnforceTable = normalEnforceTable;
        this.heroEnforceTable = heroEnforceTable;
        this.heroEnforceEffectTable = heroEnforceEffectTable;
    }

    //public PlayerUnit AddUnit(int unitId)
    //{
    //    if (ownedUnits.ContainsKey(unitId))
    //    {
    //        return ownedUnits[unitId];
    //    }

    //    var baseData = unitTable.Get(unitId);
    //    if (baseData == null)
    //    {
    //        Debug.LogError($"ID 없는 유닛: {unitId}");
    //        return null;
    //    }

    //    // enforceTable 전달
    //    PlayerUnit newUnit = new PlayerUnit(baseData, normalEnforceTable, heroEnforceTable, heroEnforceEffectTable);
    //    ownedUnits.Add(unitId, newUnit);
    //    return newUnit;
    //}

    public PlayerUnit GetPlayerUnit(int unitId)
    {
        if (!ownedUnits.ContainsKey(unitId))
        {
            return null;
        }

        return ownedUnits[unitId];
    }

    public int GetUnitTotalAttack(int unitId)
    {
        if (!ownedUnits.ContainsKey(unitId))
        {
            return 0;
        }

        return ownedUnits[unitId].CurrentAttack;
    }

    public Dictionary<int, PlayerUnit> GetAllUnits()
    {
        return ownedUnits;
    }
}
