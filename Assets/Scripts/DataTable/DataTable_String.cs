using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DataTable_String : DataTable
{
    public class Data
    {
        public string ID { get; set; }
        public string String { get; set; }
    }

    private readonly Dictionary<string, string> dictionary = new Dictionary<string, string>();

    public override void Load(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = Addressables.LoadAssetAsync<TextAsset>(path).WaitForCompletion();
        var list = LoadCSV<Data>(textAsset.text);
        
        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.ID))
            {
                dictionary.Add(item.ID, item.String);
            }
            else
            {
                Debug.LogError($"중복된 키: {item.ID}");
            }
        }
    }

    public string Get(string key)
    {
        if (!dictionary.ContainsKey(key))
        {
            return "존재하지 않는 키";
        }
        return dictionary[key];
    }
}
