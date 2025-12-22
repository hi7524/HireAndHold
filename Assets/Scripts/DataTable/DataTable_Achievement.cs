using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AchievementData
{
    public int Achievements_ID { get; set; }
    public int Achievements_Type { get; set; }
    public string Condition_Key { get; set; }
    public int Condition_Value { get; set; }
    public int Cumulative_Or_Not { get; set; }
    public int Reward_Type { get; set; }
    public int Reward_ID { get; set; }
    public int Reward_Value { get; set; }
    public int Exposure_Or_Not { get; set; }
    public int Sort_Order { get; set; }
    public int Achievement_Name { get; set; }
    public int Achievement_Desc { get; set; }

    // 누적형 업적인지 여부
    public bool IsCumulative => Cumulative_Or_Not == 1;

    // UI 노출 여부
    public bool IsExposed => Exposure_Or_Not == 1;
}

public class DataTable_Achievement : DataTable
{
    private readonly Dictionary<int, AchievementData> dictionary = new Dictionary<int, AchievementData>();

    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<AchievementData>(textAsset.text);

        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.Achievements_ID))
            {
                dictionary.Add(item.Achievements_ID, item);
            }
            else
            {
                Debug.LogError($"[Achievement] 중복된 키: {item.Achievements_ID}");
            }
        }
    }

    public AchievementData Get(int key)
    {
        return dictionary.TryGetValue(key, out var data) ? data : null;
    }

    public IEnumerable<AchievementData> GetAll()
    {
        return dictionary.Values;
    }

    /// <summary>
    /// 특정 타입의 업적 목록 조회
    /// </summary>
    public IEnumerable<AchievementData> GetByType(int type)
    {
        return dictionary.Values.Where(a => a.Achievements_Type == type);
    }

    /// <summary>
    /// 특정 조건 키의 업적 목록 조회
    /// </summary>
    public IEnumerable<AchievementData> GetByConditionKey(string conditionKey)
    {
        return dictionary.Values.Where(a => a.Condition_Key == conditionKey);
    }

    /// <summary>
    /// UI에 노출되는 업적만 조회 (정렬 순서대로)
    /// </summary>
    public IEnumerable<AchievementData> GetExposedAchievements()
    {
        return dictionary.Values
            .Where(a => a.IsExposed)
            .OrderBy(a => a.Sort_Order);
    }
}
