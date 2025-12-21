using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DailyRewardData
{
    public int ATTENDANCE_ID { get; set; }
    public int ATTENDANCE_MONTH { get; set; }
    public int ATTENDANCE_DAY { get; set; }
    public int REWARD_TYPE { get; set; }
    public int REWARD_ID { get; set; }
    public int REWARD_NUM { get; set; }
}

public class DataTable_DailyReward : DataTable
{
    private readonly Dictionary<int, DailyRewardData> dictionary = new Dictionary<int, DailyRewardData>();

    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<DailyRewardData>(textAsset.text);

        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.ATTENDANCE_ID))
            {
                dictionary.Add(item.ATTENDANCE_ID, item);
            }
            else
                Debug.LogError($"중복된 키: {item.ATTENDANCE_ID}");
        }
    }

    public DailyRewardData Get(int key)
    {
        if (!dictionary.ContainsKey(key))
        {
            return null;
        }
        return dictionary[key];
    }

    public IEnumerable<DailyRewardData> GetAll()
    {
        return dictionary.Values;
    }
}
