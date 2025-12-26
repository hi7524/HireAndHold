using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;


public class PackageData
{
    public int PACKAGE_ID { get; set; }
    public string PACKAGE_KEY { get; set; }
    public int PACKAGE_NAME { get; set; }  // StringTable ID
    public int ITEM_ID1 { get; set; }
    public int ITEM1_AMOUNT { get; set; }
    public int ITEM_ID2 { get; set; }
    public int ITEM2_AMOUNT { get; set; }
    public int ITEM_ID3 { get; set; }
    public int ITEM3_AMOUNT { get; set; }
}

public class DataTable_Package : DataTable
{
    private readonly Dictionary<int, PackageData> dictionary = new Dictionary<int, PackageData>();


    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<PackageData>(textAsset.text);

        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.PACKAGE_ID))
            {
                dictionary.Add(item.PACKAGE_ID, item);
            }
            else
                Debug.LogError($"중복된 키: {item.PACKAGE_ID}");
        }
    }

    public PackageData Get(int key)
    {
        if (!dictionary.ContainsKey(key))
        {
            return null;
        }
        return dictionary[key];
    }

    public IEnumerable<PackageData> GetAll()
    {
        return dictionary.Values;
    }
}
