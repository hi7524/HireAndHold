using CsvHelper.Configuration.Attributes;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DataTable_HeroEnforceEffect : DataTable
{
    public class HeroEnforceEffectData
    {
        public int Hero_Enforce_EffectID { get; set; }

        [Name("SkillID1")]
        public int? SkillID1 { get; set; }

        [Name("SkillID2")]
        public int? SkillID2 { get; set; }

        public float Skill_Damage_Up { get; set; }
        public float Attack_Up { get; set; }
        public float Duration_Up { get; set; }
        public int Projectile { get; set; }

        [Default(0f)]
        public float CoolTime_Down { get; set; }

        [Default(0f)]
        public float Attack_Speed { get; set; }

        [Name("Enforce_Effect_DESCRIPTION")]
        [Index(10)]
        public string Enforce_Effect_DESCRIPTION { get; set; }
    }


    private readonly Dictionary<int, HeroEnforceEffectData> table = new Dictionary<int, HeroEnforceEffectData>();

    public override async UniTask LoadAsync(string filename)
    {
        table.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<HeroEnforceEffectData>(textAsset.text);

        foreach (var item in list)
        {
            if (!table.ContainsKey(item.Hero_Enforce_EffectID))
            {
                table.Add(item.Hero_Enforce_EffectID, item);
            }
            else
            {
                Debug.LogError($"HeroEnforceEffect 중복 id {item.Hero_Enforce_EffectID}");
            }
        }
    }

    public HeroEnforceEffectData Get(int enforceId)
    {
        if (!table.ContainsKey(enforceId))
        {
            Debug.LogError($"HeroEnforceEffect 존재하지 않는 id {enforceId}");
            return null;
        }

        return table[enforceId];
    }

    public bool TryGet(int enforceId, out HeroEnforceEffectData data)
    {
        return table.TryGetValue(enforceId, out data);
    }
}
