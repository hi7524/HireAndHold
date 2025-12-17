using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class OreData
{
    public int OresID { get; set; }
    public int Ores_HP { get; set; }
    public int Damage_Per_Touch_Of_Ore { get; set; }
    public int Base_Number_Of_Successful_Drops { get; set; }
    public int Jackpot_Percent { get; set; }
    public int Jackpot_Percent_Minimum_Number_Of_Drops { get; set; }
    public int Jackpot_Percent_Maximum_Number_Of_Drops { get; set; }
    public int Fiasco { get; set; }
    public int Fiasco_Drops { get; set; }
}

public class DataTable_Ore : DataTable
{
    private readonly Dictionary<int, OreData> table = new Dictionary<int, OreData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<OreData>(textAsset.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.OresID))
                table.Add(item.OresID, item);
            else
                Debug.LogError("키 중복");
        }
    }

    public OreData Get(int ID)
    {
        if (!table.ContainsKey(ID))
        {
            return null;
        }
        return table[ID];
    }

    public IEnumerable<OreData> GetAll()
    {
        return table.Values;
    }
}