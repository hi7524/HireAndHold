using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;


public class SellingData
{
    public int SELLING_ID { get; set; }
    public string SELLING_NAME { get; set; }
    public int SELLING_ITEM { get; set; }
    public int SELLING_AMOUNT { get; set; }
    public int SELLING_MONEY { get; set; }
    public int SELLING_PRICE { get; set; }
    public string SELLING_START { get; set; }
    public string SELLING_END { get; set; }
    public int SELLING_LIMIT { get; set; }
    public int SELLING_NUM { get; set; }
    public int SELLING_NOW { get; set; }
}

public class DataTable_Selling : DataTable
{
    private readonly Dictionary<int, SellingData> dictionary = new Dictionary<int, SellingData>();


    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<SellingData>(textAsset.text);

        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.SELLING_ID))
            {
                dictionary.Add(item.SELLING_ID, item);
            }
            else
                Debug.LogError($"중복된 키: {item.SELLING_ID}");
        }
    }

    public SellingData Get(int key)
    {
        if (!dictionary.ContainsKey(key))
        {
            return null;
        }
        return dictionary[key];
    }

    public IEnumerable<SellingData> GetAll()
    {
        return dictionary.Values;
    }
}
