using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class MonsterData
    {
        public int MON_ID { get; set; }
        public string MON_NAME { get; set; }
        public int MON_TYPE { get; set; }
        public int MON_ATK { get; set; }
        public int MON_HP { get; set; }
        public int MON_DEF { get; set; }
        public int MON_RANGE { get; set; }
        public float MON_HIT_BOX_SCA{ get; set; }
        public int MON_SPEED { get; set; }
        public int MON_STAGE_EXP { get; set; }
        public float MON_DROP_GOLD { get; set; }
        public int DROP_ITEM1_ID { get; set; }
        public int DROP_ITEM1_COUNT { get; set; }
        public int DROP_ITEM1_RATE { get; set; }
        public int DROP_ITEM2_ID { get; set; }
        public int DROP_ITEM2_COUNT { get; set; }
        public int DROP_ITEM2_RATE { get; set; }
        public string MON_MODEL { get; set; }



    }
public class DataTable_Monster : DataTable
{
    
    private readonly Dictionary<int, MonsterData> dictionary = new Dictionary<int, MonsterData>();

    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<MonsterData>(textAsset.text);
        
        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.MON_ID))
            {
                dictionary.Add(item.MON_ID, item);
            }
            else
                Debug.LogError($"중복된 키: {item.MON_ID}");
        }
    }

    public MonsterData Get(int key)
    {
        if (!dictionary.ContainsKey(key))
        {
            return null;
        }
        return dictionary[key];
    }
}
