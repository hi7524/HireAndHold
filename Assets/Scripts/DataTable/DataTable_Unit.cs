using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DataTable_Unit : DataTable
{
    public class UnitData
    {
        public int UNIT_ID { get; set; }
        public string UNIT_NAME { get; set; } // String table과 연결 필요 **
        public int UNIT_RANK { get; set; }
        public int UNIT_LEVEL { get; set; }
        public int UNIT_GET { get; set; }
        public int UNIT_ATK { get; set; }
        public int UNIT_ATKRANGE { get; set; }
        public float UNIT_ATKCOOLTIME { get; set; }
        public int UNIT_BOLTNUM { get; set; }
        public int UNIT_BOLTSPEED { get; set; }
        public int UNIT_ATK_CRT { get; set; }
        public float UNIT_CRT_DMG { get; set; }
        public int UNIT_SKILL1 { get; set; }
        public int UNIT_SKILL2 { get; set; }
        public int NORMAL_ENFORCEID { get; set; }
        public int HERO_ENFORCEID { get; set; }
        public string UNIT_PREFAB { get; set; }
        public string UNIT_DESCRIPTION { get; set; } // String table과 연결 필요 **
        public int ORDER_1 { get; set; }
        public int ORDER_2 { get; set; }
    }

     private readonly Dictionary<int, UnitData> table = new Dictionary<int, UnitData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<UnitData>(textAsset.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.UNIT_ID))
            {
                table.Add(item.UNIT_ID, item);
            }
            else
            {
                Debug.LogError("키 중복");
            }
        }
    }

    public UnitData Get(int unitId)
    {
        if (!table.ContainsKey(unitId))
        {
            return null;
        }
        return table[unitId];
    }
}