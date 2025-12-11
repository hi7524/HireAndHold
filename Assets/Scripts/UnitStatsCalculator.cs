//using UnityEngine;

//public static class UnitStatsCalculator
//{
//    public static PlayerUnit GetFinalStats(int unitId, UnitManager unitManager)
//    {
//        return unitManager.GetPlayerUnit(unitId);
//    }

//    public static PlayerUnit GetNextLevelStats(int unitId, UnitManager unitManager)
//    {
//        var u = unitManager.GetPlayerUnit(unitId);
//        if (u == null)
//        {
//            return null;
//        }

//        var clone = new PlayerUnit(
//            u.BaseData,
//            u.NormalTable,
//            u.HeroTable,
//            u.HeroEffectTable
//        );

//        for (int i = 0; i < u.NormalEnforceLevel; i++)
//        {
//            clone.AddNormalEnforceLevel();
//        }

//        for (int i = 0; i < u.HeroEnforceLevel; i++)
//        {
//            clone.AddHeroEnforceLevel();
//        }

//        clone.AddNormalEnforceLevel();

//        return clone;
//    }

//}
