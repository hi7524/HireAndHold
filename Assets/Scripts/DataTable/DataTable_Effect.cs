using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class EffectData 
{
    public int EFFECT_ID { get; set; }
    public string EFFECT_NAME { get; set; }
    public string EFFECT_NAME_KR { get; set; }           
    public int EFFECT_TYPE { get; set; }
    public float EFFECT_VALUE { get; set; }
    public int EFFECT_STACK { get; set; }
    public int EFFECT_STACKNUM { get; set; }
    public int EFFECT_CLEAR { get; set; }
    public string EFFECT_DESCRIPTION { get; set; }
    public string EFFECT_DESC { get; set; }             
}
public class DataTable_Effect : DataTable
{
    private readonly Dictionary<int, EffectData> dictionary = new Dictionary<int, EffectData>();

    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<EffectData>(textAsset.text);
        
        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.EFFECT_ID))
            {
                dictionary.Add(item.EFFECT_ID, item);
            }
            else
            {
                Debug.LogError($"[DataTable_Effect] 중복된 키: {item.EFFECT_ID}");
            }
        }
    }

    public EffectData Get(int key)
    {
        if (!dictionary.ContainsKey(key))
        {
            Debug.LogWarning($"[DataTable_Effect] 존재하지 않는 이펙트 ID: {key}");
            return null;
        }
        return dictionary[key];
    }
}
