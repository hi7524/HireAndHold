using UnityEngine;

public class PlayerUnit
{
    public int UnitID { get; private set; }
    public int Level { get; private set; }
    public int NormalEnforceLevel { get; private set; } = 0;
    public int UnitRank { get; private set; }

    private DataTable_Unit.UnitData baseData;
    private DataTable_NormalEnforce enforceTable;

    public PlayerUnit(DataTable_Unit.UnitData baseData, DataTable_NormalEnforce enforceTable)
    {
        this.baseData = baseData;
        this.enforceTable = enforceTable;
        this.UnitID = baseData.UNIT_ID;
        this.Level = baseData.UNIT_LEVEL;
        this.UnitRank = baseData.UNIT_RANK;
    }

    public void AddNormalEnforceLevel()
    {
        NormalEnforceLevel++;
    }

    public int CurrentAttack
    {
        get
        {
            int totalAttack = baseData.UNIT_ATK;

            // 각 레벨의 강화 데이터 Count
            for (int lv = 1; lv <= NormalEnforceLevel; lv++)
            {
                int enforceId = CalculateEnforceID(UnitRank, lv);
                if (enforceTable.TryGet(enforceId, out var enforceData))
                {
                    totalAttack += Mathf.RoundToInt(enforceData.AttackUp);
                }
            }

            return totalAttack;
        }
    }

    // id 계산 로직 
    private int CalculateEnforceID(int rank, int level)
    {
        int baseOffset = (rank - 1) * 20;
        int suffix = baseOffset + level;
        string idString = $"11{level}{rank}8{suffix:D2}"; 
        return int.Parse(idString);
    }
}
