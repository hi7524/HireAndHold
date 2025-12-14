using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class UnitGachaData
{
    public int Gacha_ID { get; set; }
    public int Catalog_ID { get; set; }
    public int Gacha_Type { get; set; }
    public int ItemID { get; set; }
    public int ItemNum { get; set; }
    public int Draw10_ItemID { get; set; }
    public int Draw10_ItemNum { get; set; }
    public int FreeDraw { get; set; }
    public int FreeDraw_CoolTime { get; set; }
    public int Confirmed_Draw_CatalogID { get; set; }
}

public class DataTable_UnitGacha : DataTable
{
    private readonly Dictionary<int, UnitGachaData> dictionary = new Dictionary<int, UnitGachaData>();

    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<UnitGachaData>(textAsset.text);

        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.Gacha_ID))
            {
                dictionary.Add(item.Gacha_ID, item);
            }
            else
            {
                Debug.LogError($"중복된 키: {item.Gacha_ID}");
            }
        }
    }

    public UnitGachaData Get(int gachaId)
    {
        if (!dictionary.ContainsKey(gachaId))
        {
            return null;
        }
        return dictionary[gachaId];
    }

    public UnitGachaData GetByType(int gachaType)
    {
        foreach (var data in dictionary.Values)
        {
            if (data.Gacha_Type == gachaType)
                return data;
        }
        return null;
    }

    public IEnumerable<UnitGachaData> GetAll()
    {
        return dictionary.Values;
    }
}
