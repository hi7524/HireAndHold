using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DungeonSettingData
{
    public int Dungeon_SettingID { get; set; }
    public int Dungeon_Name { get; set; }
    public int Dungeon_Description { get; set; }
    public int Dungeon_Type { get; set; }
    public int Total_Stage { get; set; }
    public int Conditions_Of_Entry_ItemID { get; set; }
    public int Basic_Num_Of_Entries { get; set; }

    // StringTable 연동
    public string StringName => DataTableManager.StringTable?.Get(Dungeon_Name);
    public string StringDescription => DataTableManager.StringTable?.Get(Dungeon_Description);
}

public class DataTable_DungeonSetting : DataTable
{
    private readonly Dictionary<int, DungeonSettingData> table = new Dictionary<int, DungeonSettingData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<DungeonSettingData>(textAsset.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.Dungeon_SettingID))
                table.Add(item.Dungeon_SettingID, item);
            else
                Debug.LogError("키 중복");
        }
    }

    public DungeonSettingData Get(int ID)
    {
        if (!table.ContainsKey(ID))
        {
            return null;
        }
        return table[ID];
    }

    public IEnumerable<DungeonSettingData> GetAll()
    {
        return table.Values;
    }
}