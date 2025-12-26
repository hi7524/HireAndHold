using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class QuestData
{
    public int Quest_ID { get; set; }
    public int Quest_Type { get; set; }           // 1=일일, 2=주간
    public int Category { get; set; }
    public string Condition_Key { get; set; }
    public int Condition_Value { get; set; }
    public int Cumulative_Or_Not { get; set; }
    public int Reward_Type { get; set; }
    public int Reward_ID { get; set; }
    public int Reward_Value { get; set; }
    public int Reset_Cycle_Type { get; set; }     // 1=일일, 2=주간
    public int Exposure_Or_Not { get; set; }
    public int Sort_Order { get; set; }
    public int Quest_Name { get; set; }
    public int Quest_Desc { get; set; }

    // 누적형 퀘스트인지 여부
    public bool IsCumulative => Cumulative_Or_Not == 1;

    // UI 노출 여부
    public bool IsExposed => Exposure_Or_Not == 1;

    // 일일 퀘스트 여부
    public bool IsDaily => Quest_Type == 1;

    // 주간 퀘스트 여부
    public bool IsWeekly => Quest_Type == 2;
}

public class DataTable_Quest : DataTable
{
    private readonly Dictionary<int, QuestData> dictionary = new Dictionary<int, QuestData>();

    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<QuestData>(textAsset.text);

        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.Quest_ID))
            {
                dictionary.Add(item.Quest_ID, item);
            }
            else
            {
                Debug.LogError($"[Quest] 중복된 키: {item.Quest_ID}");
            }
        }
    }

    public QuestData Get(int key)
    {
        return dictionary.TryGetValue(key, out var data) ? data : null;
    }

    public IEnumerable<QuestData> GetAll()
    {
        return dictionary.Values;
    }

    /// <summary>
    /// 특정 타입의 퀘스트 목록 조회 (1=일일, 2=주간)
    /// </summary>
    public IEnumerable<QuestData> GetByType(int type)
    {
        return dictionary.Values.Where(q => q.Quest_Type == type);
    }

    /// <summary>
    /// 일일 퀘스트 목록 조회
    /// </summary>
    public IEnumerable<QuestData> GetDailyQuests()
    {
        return dictionary.Values.Where(q => q.IsDaily && q.IsExposed).OrderBy(q => q.Sort_Order);
    }

    /// <summary>
    /// 주간 퀘스트 목록 조회
    /// </summary>
    public IEnumerable<QuestData> GetWeeklyQuests()
    {
        return dictionary.Values.Where(q => q.IsWeekly && q.IsExposed).OrderBy(q => q.Sort_Order);
    }

    /// <summary>
    /// 특정 조건 키의 퀘스트 목록 조회
    /// </summary>
    public IEnumerable<QuestData> GetByConditionKey(string conditionKey)
    {
        return dictionary.Values.Where(q => q.Condition_Key == conditionKey);
    }

    /// <summary>
    /// UI에 노출되는 퀘스트만 조회 (정렬 순서대로)
    /// </summary>
    public IEnumerable<QuestData> GetExposedQuests()
    {
        return dictionary.Values
            .Where(q => q.IsExposed)
            .OrderBy(q => q.Sort_Order);
    }
}
