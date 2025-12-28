using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;


public class ItemData
{
    public int ITEM_ID { get; set; }
    public int ITEM_NAME { get; set; }  // StringTable ID
    public int ITEM_TYPE { get; set; }
    public int ITEM_STACK { get; set; }
    public int PACKAGE_ID { get; set; }
    public string ITEM_ICON { get; set; }
    public int ITEM_DESCRIPTION { get; set; }  // StringTable ID
}

public class DataTable_Item : DataTable
{
    private readonly Dictionary<int, ItemData> dictionary = new Dictionary<int, ItemData>();


    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<ItemData>(textAsset.text);

        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.ITEM_ID))
            {
                dictionary.Add(item.ITEM_ID, item);
            }
            else
                Debug.LogError($"중복된 키: {item.ITEM_ID}");
        }
    }

    public ItemData Get(int key)
    {
        if (!dictionary.ContainsKey(key))
        {
            return null;
        }
        return dictionary[key];
    }

    public IEnumerable<ItemData> GetAll()
    {
        return dictionary.Values;
    }
}
