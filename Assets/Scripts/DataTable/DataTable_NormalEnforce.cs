using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine;

public class DataTable_NormalEnforce : DataTable
{
    public class NormalEnforceData
    {
        public int Normal_EnforceID { get; set; }
        public int Normal_Enforce_LV { get; set; }
        public float AttackUp { get; set; }
        public int IngredientID { get; set; }
        public float IngredientNum { get; set; }
        public int Gold_Cost { get; set; }
        public int Class { get; set; }
    }

    private readonly Dictionary<int, NormalEnforceData> table = new Dictionary<int, NormalEnforceData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<NormalEnforceData>(textAsset.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.Normal_EnforceID))
            {
                table.Add(item.Normal_EnforceID, item);
            }
            else
            {
                Debug.LogError($"NormalEnforce 중복 id {item.Normal_EnforceID}");
            }
        }
    }

    public NormalEnforceData Get(int enforceId)
    {
        if (!table.ContainsKey(enforceId))
        {
            Debug.LogError($"NormalEnforce 존재하지 않는 id {enforceId}");
            return null;
        }

        return table[enforceId];
    }

    public bool TryGet(int enforceId, out NormalEnforceData data)
    {
        return table.TryGetValue(enforceId, out data);
    }


}
