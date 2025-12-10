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
    }

    private readonly Dictionary<int, HeroEnforceData> table = new Dictionary<int, HeroEnforceData>();

    public IReadOnlyDictionary<int, HeroEnforceData> Table => table;

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<HeroEnforceData>(textAsset.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.Hero_EnforceID))
            {
                table.Add(item.Hero_EnforceID, item);
            }
            else
            {
                Debug.LogError($"HeroEnforce double id {item.Hero_EnforceID}");
            }
        }

        Debug.Log($"HeroEnforce 테이블 로드");
    }

    public HeroEnforceData Get(int enforceId)
    {
        table.TryGetValue(enforceId, out var data);
        return data;
    }

    //임시 기획 테이블 완성 아직 안됨
    public HeroEnforceData FindByLevelAndIndex(int level, int heroIndex)
    {
        foreach (var kvp in table)
        {
            var data = kvp.Value;


            if (data.Hero_Enforce_LV != level)
            {
                continue;
            }

            int csvIndex = data.Hero_EnforceID % 100;

            if (csvIndex == heroIndex)
            {
                return data;
            }
        }
        return null;
    }

}
