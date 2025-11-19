using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class StageData
    {
        public int  STAGE_ID { get; set; }
        public string STAGE_NAME { get; set; }
        public int STAGE_TIME_SEC { get; set; }
        public int TOTAL_WAVE { get; set; }
        public int STAGE_C_EXP { get; set; }
        public int STAGE_C_GOLD { get; set; }
        public int STAGE_C_RE1_ID { get; set; }
        public int STAGE_C_RE1_CO { get; set; }
        public int STAGE_C_RE2_ID { get; set; }
        public int STAGE_C_RE2_CO { get; set; }
        public string STAGE_MAP { get; set; }
        
    }
public class DataTable_Stage : DataTable
{
    

    private readonly Dictionary<int, StageData> dictionary = new Dictionary<int, StageData>();

    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<StageData>(textAsset.text);
        
        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.STAGE_ID))
            {
                dictionary.Add(item.STAGE_ID, item);
            }
            else
                Debug.LogError($"중복된 키: {item.STAGE_ID}");
        }
    }

    public StageData Get(int key)
    {
        if (!dictionary.ContainsKey(key))
        {
            return null;
        }
        return dictionary[key];
    }
}
