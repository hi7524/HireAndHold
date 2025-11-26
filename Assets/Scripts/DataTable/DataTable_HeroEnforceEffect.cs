using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DataTable_HeroEnforceEffect : DataTable
{
    public class HeroEnforceEffectData
    {
        public int Hero_Enforce_EffectID { get; set; }
        public int SkillID1 { get; set; }
        public int SkillID2 { get; set; }
        public float Skill_Damage_Up { get; set; }
        public float Attack_Up { get; set; }
        public float Duration_Up { get; set; }
        public int Projectile { get; set; }
        public float CoolTime_Down { get; set; }
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
                Debug.LogError($"NormalEnforce 중복 id {item.Hero_Enforce_EffectID}");
            }
        }
    }

    public HeroEnforceEffectData Get(int enforceId)
    {
        if (!table.ContainsKey(enforceId))
        {
            Debug.LogError($"HeroEnforce 존재하지 않는 id {enforceId}");
            return null;
        }

        return table[enforceId];
    }

    public bool TryGet(int enforceId, out HeroEnforceEffectData data)
    {
        return table.TryGetValue(enforceId, out data);
    }
}
