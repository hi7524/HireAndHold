using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DataTable_UnitCatalog : DataTable

{
    public class Data
    {
        public int Catalog_ID { get; set; }
        public int Unit_ID { get; set; }
        public float Probability { get; set; }
        public float Weight { get; set; }
        
    }

    private readonly Dictionary<int, List<Data>> dictionary = new Dictionary<int, List<Data>>();

    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<Data>(textAsset.text);
        
        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.Catalog_ID))
            {
                dictionary.Add(item.Catalog_ID, new List<Data> { item });
            }
            else
            {
                dictionary[item.Catalog_ID].Add(item);
            }
        }
    }

    public List<Data> Get(int key)
    {
        if (!dictionary.ContainsKey(key))
        {
            return null;
        }
        return dictionary[key];
    }
}
