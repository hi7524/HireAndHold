using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DataTable_Wave : DataTable
{
    public class Data
    {

        public int WAVE_ID { get; set; }
        public int STAGE_ID { get; set; }
        public int WAVE_NUM { get; set; }
        public int WAVE_TYPE { get; set; }
        public int WAVE_START_T { get; set; }
        public int WAVE_END_T { get; set; }
        public int SPAWN_MON1_ID { get; set; }
        public int MON1_COUNT { get; set; }
        public int SPAWN_MON2_ID { get; set; }
        public int MON2_COUNT { get; set; }
        public float WAVE_SPEED { get; set; }

    }
    private readonly Dictionary<int, Data> dictionary = new Dictionary<int, Data>();
    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<Data>(textAsset.text);
        
        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.WAVE_ID))
            {
                dictionary.Add(item.WAVE_ID, item);
            }
            else
                Debug.LogError($"중복된 키: {item.WAVE_ID}");
        }
    }

    public Data Get(int key)
    {
        if (!dictionary.ContainsKey(key))
        {
            return null;
        }
        return dictionary[key];
    }
}
