using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class TutorialData
{
    public int StringID { get; set; }
    public string TutoText { get; set; }
}

public class DataTable_Tutorial : DataTable
{
    private readonly Dictionary<int, TutorialData> dictionary = new Dictionary<int, TutorialData>();

    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<TutorialData>(textAsset.text);

        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.StringID))
            {
                dictionary.Add(item.StringID, item);
            }
            else
            {
                Debug.LogWarning($"[TutorialTable] 중복된 키: {item.StringID}");
            }
        }

        Debug.Log($"[TutorialTable] 로드 완료: {dictionary.Count}개");
    }

    /// <summary>
    /// ID로 튜토리얼 데이터 가져오기
    /// </summary>
    public TutorialData Get(int stringId)
    {
        if (dictionary.TryGetValue(stringId, out var data))
        {
            return data;
        }
        return null;
    }

    /// <summary>
    /// ID로 튜토리얼 텍스트만 가져오기
    /// </summary>
    public string GetText(int stringId)
    {
        if (dictionary.TryGetValue(stringId, out var data))
        {
            return data.TutoText;
        }
        return $"[{stringId}]";
    }

    /// <summary>
    /// 모든 데이터 가져오기
    /// </summary>
    public IEnumerable<TutorialData> GetAll()
    {
        return dictionary.Values;
    }
}
