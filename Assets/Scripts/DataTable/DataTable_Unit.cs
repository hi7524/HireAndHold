using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class UnitData
{
    public int UNIT_ID { get; set; }             // 유닛 ID
    public int NAME { get; set; }             // 유닛 이름 키 (StringTable)
    public int RANK { get; set; }                // 유닛 등급
    public int LEVEL { get; set; }               // 유닛 성급
    public int ATTACK { get; set; }              // 유닛 공격력
    public float ATTACK_RANGE { get; set; }      // 유닛 공격 사거리
    public float ATTACK_COOLTIME { get; set; }   // 기본 공격 쿨타임
    public int BOLT_NUM { get; set; }            // 기본 투사체 개수
    public float ATTACK_CRITICAL { get; set; }   // 공격 치명타 확률 (%)
    public float CRITICAL_DAMAGE { get; set; }   // 공격 치명타 데미지
    public int UNIT_SKILL1 { get; set; }         // 스킬 ID 1
    public int UNIT_SKILL2 { get; set; }         // 스킬 ID 2
    public int NORMAL_ENFORCEID { get; set; }    // 일반 강화 ID
    public string UNIT_ICON { get; set; }        // 유닛 아이콘
    public int UNIT_DESCRIPTION { get; set; }    // 유닛 설명 키 (StringTable)
    public string PREFAB_NAME { get; set; }
    public string GRID_DATA { get; set; }        // 그리드 데이터 ScriptableObject 이름
    public string PROJECTILE { get; set; }

    // StringTable 연동
    public string StringName => DataTableManager.StringTable?.Get(NAME);
    public string StringDescription => DataTableManager.StringTable?.Get(UNIT_DESCRIPTION);
}

public class DataTable_Unit : DataTable
{
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

    public Dictionary<int, UnitData> RawTable => table;

    public IEnumerable<UnitData> GetAll()
    {
        return table.Values;
    }
} 
