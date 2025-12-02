using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;


public class StageLevelData
{
    public int STAGE_LEV_ID { get; set; }
    public int STAGE_LEV { get; set; }
    public int STAGE_EXP_NEED { get; set; }
    public int STAGE_EXP_POINTS { get; set; }
}

public class DataTable_StageLevel : DataTable
{
    private readonly Dictionary<int, StageLevelData> dictionary = new Dictionary<int, StageLevelData>();


    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<StageLevelData>(textAsset.text);

        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.STAGE_LEV_ID))
            {
                dictionary.Add(item.STAGE_LEV_ID, item);
            }
            else
                Debug.LogError($"중복된 키: {item.STAGE_LEV_ID}");
        }
    }

    public StageLevelData Get(int key)
    {
        if (!dictionary.ContainsKey(key))
        {
            return null;
        }
        return dictionary[key];
    }

    public StageLevelData GetByLevel(int level)
    {
        foreach (var data in dictionary.Values)
        {
            if (data.STAGE_LEV == level)
                return data;
        }
        return null;
    }

    public IEnumerable<StageLevelData> GetAll()
    {
        return dictionary.Values;
    }
}
