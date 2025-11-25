using UnityEngine;
using System.Collections.Generic;

public class PassiveSkillManager : MonoBehaviour
{
    
    private Dictionary<PassiveSkillType, PassiveSkillGroup> skillGroups = new Dictionary<PassiveSkillType, PassiveSkillGroup>();
    private PassiveSkillEffects currentEffects = new PassiveSkillEffects();
    
    private void Awake()
    {
        InitializeSkillGroups();
    }
    
    private void InitializeSkillGroups()
    {
        skillGroups.Clear();
        
        skillGroups.Add(PassiveSkillType.Damage, new PassiveSkillGroup(PassiveSkillType.Damage, "피해량 증가"));
        skillGroups.Add(PassiveSkillType.CritRate, new PassiveSkillGroup(PassiveSkillType.CritRate, "치명타 확률 증가"));
        skillGroups.Add(PassiveSkillType.CritDamage, new PassiveSkillGroup(PassiveSkillType.CritDamage, "치명타 피해량 증가"));
        skillGroups.Add(PassiveSkillType.Exp, new PassiveSkillGroup(PassiveSkillType.Exp, "획득 경험치 증가"));
        skillGroups.Add(PassiveSkillType.ShieldRegen, new PassiveSkillGroup(PassiveSkillType.ShieldRegen, "초당 방벽 회복"));
        skillGroups.Add(PassiveSkillType.BossDamage, new PassiveSkillGroup(PassiveSkillType.BossDamage, "보스 피해량 증가"));
        
        Debug.Log($"[PassiveSkillManager] {skillGroups.Count}개의 패시브 스킬 그룹 초기화");
    }
    
    private bool TryGetSkillType(int skillId, out PassiveSkillType skillType)
    {
        skillType = PassiveSkillType.Damage;
        
        if (skillId < 222070 || skillId > 222087)
            return false;
        
        int offset = (skillId - 222070) % 6;
        skillType = (PassiveSkillType)(2206 + offset);
        return true;
    }
    
    public bool AddOrUpgradePassiveSkill(int skillId)
    {
        if (!TryGetSkillType(skillId, out PassiveSkillType skillType))
        {
            Debug.LogError($"[PassiveSkillManager] 유효하지 않은 스킬 ID: {skillId}");
            return false;
        }
        
        if (!skillGroups.ContainsKey(skillType))
        {
            Debug.LogError($"[PassiveSkillManager] 존재하지 않는 스킬 타입: {skillType}");
            return false;
        }
        
        PassiveSkillGroup group = skillGroups[skillType];
        
        if (group.currentStar > 0)
        {
            if (group.AddStar())
            {
                RecalculateEffects();
                Debug.Log($"[PassiveSkillManager] '{group.displayName}' ★{group.currentStar}으로 업그레이드!");
                return true;
            }
            else
            {
                Debug.LogWarning($"[PassiveSkillManager] '{group.displayName}'은(는) 이미 최대 레벨입니다.");
                return false;
            }
        }
        else
        {
            group.currentStar = 1;
            RecalculateEffects();
            Debug.Log($"[PassiveSkillManager] '{group.displayName}' ★1 획득!");
            return true;
        }
    }
    
    public int GetRandomPassiveSkillForReward()
    {
        List<PassiveSkillGroup> availableGroups = new List<PassiveSkillGroup>();
        
        foreach (PassiveSkillGroup group in skillGroups.Values)
        {
            if (group.currentStar < 3)
            {
                availableGroups.Add(group);
            }
        }
        
        if (availableGroups.Count == 0)
        {
            return -1;
        }
        
        PassiveSkillGroup selected = availableGroups[UnityEngine.Random.Range(0, availableGroups.Count)];
        
        if (selected.currentStar == 0)
        {
            return selected.GetSkillIdByStar(1);
        }
        else
        {
            return selected.GetNextSkillId();
        }
    }
    
    
    public List<int> GetRandomPassiveSkillsForReward(int count)
    {
        List<int> skillIds = new List<int>();
        List<PassiveSkillGroup> availableGroups = new List<PassiveSkillGroup>();
        
        
        foreach (PassiveSkillGroup group in skillGroups.Values)
        {
            if (group.currentStar < 3)
            {
                availableGroups.Add(group);
            }
        }
        
        if (availableGroups.Count == 0)
        {
            Debug.LogWarning("[PassiveSkillManager] 획득 가능한 패시브 스킬이 없습니다.");
            return skillIds;
        }
        
        for (int i = 0; i < count && availableGroups.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableGroups.Count);
            PassiveSkillGroup selected = availableGroups[randomIndex];
            
            int skillId = selected.currentStar == 0 
                ? selected.GetSkillIdByStar(1) 
                : selected.GetNextSkillId();
            
            skillIds.Add(skillId);
            
            availableGroups.RemoveAt(randomIndex);
        }
        
        return skillIds;
    }
    
    private void RecalculateEffects()
    {
        currentEffects.Reset();
        
        foreach (PassiveSkillGroup group in skillGroups.Values)
        {
            if (group.currentStar == 0) continue;
            
            int skillId = group.GetCurrentSkillId();
            SkillData skillData = DataTableManager.SkillTable.Get(skillId);
            EffectData data = DataTableManager.EffectTable.Get(skillData.SKILL_EFFECT1_ID);
            
            if (data == null) continue;
            
            ApplySkillEffect(group.skillType, data);
        }
    }
    
    private void ApplySkillEffect(PassiveSkillType type, EffectData data)
    {
        float effectValue = data.EFFECT_VALUE;
        
        switch (type)
        {
            case PassiveSkillType.Damage:
                currentEffects.damageBonus += effectValue;
                break;
            case PassiveSkillType.CritRate:
                currentEffects.critRateBonus += effectValue;
                break;
            case PassiveSkillType.CritDamage:
                currentEffects.critDamageBonus += effectValue;
                break;
            case PassiveSkillType.Exp:
                currentEffects.expBonus += effectValue;
                break;
            case PassiveSkillType.ShieldRegen:
                currentEffects.shieldRegenBonus += effectValue;
                break;
            case PassiveSkillType.BossDamage:
                currentEffects.bossDamageBonus += effectValue;
                break;
        }
    }
    
    public PassiveSkillEffects GetCurrentEffects()
    {
        return currentEffects;
    }
    
    public float GetEffectBonus(PassiveSkillType type)
    {
        switch (type)
        {
            case PassiveSkillType.Damage:
                return currentEffects.damageBonus;
            case PassiveSkillType.CritRate:
                return currentEffects.critRateBonus;
            case PassiveSkillType.CritDamage:
                return currentEffects.critDamageBonus;
            case PassiveSkillType.Exp:
                return currentEffects.expBonus;
            case PassiveSkillType.ShieldRegen:
                return currentEffects.shieldRegenBonus;
            case PassiveSkillType.BossDamage:
                return currentEffects.bossDamageBonus;
            default:
                return 0f;
        }
    }
    
    public List<PassiveSkillGroup> GetOwnedPassiveSkills()
    {
        List<PassiveSkillGroup> owned = new List<PassiveSkillGroup>();
        
        foreach (PassiveSkillGroup group in skillGroups.Values)
        {
            if (group.currentStar > 0)
            {
                owned.Add(group);
            }
        }
        
        return owned;
    }
    public void ResetAllPassiveSkills()
    {
        foreach (PassiveSkillGroup group in skillGroups.Values)
        {
            group.currentStar = 0;
        }
        
        currentEffects.Reset();
    }
    
    public void LogOwnedSkills()
    {
        Debug.Log("=== 보유 패시브 스킬 ===");
        foreach (PassiveSkillGroup group in skillGroups.Values)
        {
            if (group.currentStar > 0)
            {
                Debug.Log($"{group.displayName}: {group.currentStar}");
            }
        }
    }
    
}
