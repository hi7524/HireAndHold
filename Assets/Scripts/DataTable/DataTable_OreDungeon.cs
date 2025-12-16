using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class OreDungeonData
{
    public int Dungeon_SettingID { get; set; }
    public int DungeonID { get; set; }
    public int Stage { get; set; }
    public int Touch_Attrition_Battle { get; set; }
    public int Reward_ItemID { get; set; }
    public int OresID { get; set; }
    public int Number_Of_Ores { get; set; }
    public int OresID2 { get; set; }
    public int Number_Of_Ores2 { get; set; }
}

public class DataTable_OreDungeon : DataTable
{
    private readonly Dictionary<int, OreDungeonData> table = new Dictionary<int, OreDungeonData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<OreDungeonData>(textAsset.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.DungeonID))
                table.Add(item.DungeonID, item);
            else
                Debug.LogError("키 중복");
        }
    }

    public OreDungeonData Get(int ID)
    {
        if (!table.ContainsKey(ID))
        {
            return null;
        }
        return table[ID];
    }

    public IEnumerable<OreDungeonData> GetAll()
    {
        return table.Values;
    }
}