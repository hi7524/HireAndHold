using System.Collections.Generic;
using UnityEngine;

public class UnitManager
{
    private readonly DataTable_Unit unitTable;
    private readonly DataTable_NormalEnforce enforceTable;
    private readonly Dictionary<int, PlayerUnit> ownedUnits = new Dictionary<int, PlayerUnit>();

    public UnitManager(DataTable_Unit unitTable, DataTable_NormalEnforce enforceTable)
    {
        this.unitTable = unitTable;
        this.enforceTable = enforceTable;
    }

    public PlayerUnit AddUnit(int unitId)
    {
        if (ownedUnits.ContainsKey(unitId))
        {
            return ownedUnits[unitId];
        }

        var baseData = unitTable.Get(unitId);
        if (baseData == null)
        {
            Debug.LogError($"ID 없는 유닛: {unitId}");
            return null;
        }

        // enforceTable 전달
        PlayerUnit newUnit = new PlayerUnit(baseData, enforceTable);
        ownedUnits.Add(unitId, newUnit);
        return newUnit;
    }

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
