using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;


public class SkillData 
{
    						
    public int SKILL_ID{get; set;}
    public string SKILL_NAME{get; set;}
    public int SKILL_OBJECT{get; set;}
    public int SKILL_ACTIVATE{get; set;}
    public int SKILL_EFFECT1{get; set;}
    public int SKILL_TYPE1{get; set;}
    public int SKILL_TARGET1{get; set;}
    public float EFFECT_TIME1{get; set;}
    public int SKILL_EFFECT2{get; set;}
    public int SKILL_TYPE2{get; set;}
    public int SKILL_TARGET2{get; set;}
    public float EFFECT_TIME2{get; set;}
    public float SKILL_COOLTIME{get; set;}
    public float SKILL_CRT{get; set;}
    public float SKILL_CRT_DMG{get; set;}
    public int SKILL_BOLT{get; set;}
    public float SKILL_BOLTSPEED{get; set;}
    public float SKILL_ATKRANGE{get; set;}
    public float SKILL_RANGE{get; set;}
    public string SKILL_DESCRIPTION{get; set;}

}
public class DataTable_Skill : DataTable
{
    private readonly Dictionary<int, SkillData> dictionary = new Dictionary<int, SkillData>();
    

    public override async UniTask LoadAsync(string filename)
    {
        dictionary.Clear();

        var path = string.Format(FormatPath, filename);
        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(path).ToUniTask();

        var list = LoadCSV<SkillData>(textAsset.text);
        
        foreach (var item in list)
        {
            if (!dictionary.ContainsKey(item.SKILL_ID))
            {
                dictionary.Add(item.SKILL_ID, item);
            }
            else
                Debug.LogError($"중복된 키: {item.SKILL_ID}");
        }
    }

    public SkillData Get(int key)
    {
        if (!dictionary.ContainsKey(key))
        {
            return null;
        }
        return dictionary[key];
    }

    
}
