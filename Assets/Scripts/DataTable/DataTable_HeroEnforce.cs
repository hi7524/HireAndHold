using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine;

public class DataTable_HeroEnforce : DataTable
{
    public class HeroEnforceData
    {
        public int Hero_EnforceID { get; set; }
        public int Hero_Enforce_LV { get; set; }
        public int Hero_Enforce_EffectID { get; set; }
        public int IngredientID { get; set; }
        public float IngredientNum { get; set; }
        public int Gold_Cost { get; set; }

        public int Apply_Unit_ID_1 { get; set; }
        public int Apply_Unit_ID_2 { get; set; }
        public int Apply_Unit_ID_3 { get; set; }
    }

    private readonly Dictionary<int, Dictionary<int, HeroEnforceData>> map =
        new Dictionary<int, Dictionary<int, HeroEnforceData>>();

    public override async UniTask LoadAsync(string filename)
    {
        map.Clear();

        string path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<HeroEnforceData>(textAsset.text);

        foreach (var row in list)
        {
            foreach (var unitId in new[] { row.Apply_Unit_ID_1, row.Apply_Unit_ID_2, row.Apply_Unit_ID_3 })
            {
                if (unitId <= 0) continue;

                if (!map.ContainsKey(unitId))
                    map[unitId] = new Dictionary<int, HeroEnforceData>();

                map[unitId][row.Hero_Enforce_LV] = row;
            }
        }
    }

    public HeroEnforceData Get(int unitId, int level)
    {
        if (map.TryGetValue(unitId, out var levelDict))
        {
            levelDict.TryGetValue(level, out var data);
            return data;
        }

        return null;
    }
}
