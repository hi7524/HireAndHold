using UnityEngine;
using System.Collections.Generic;

public class PassiveSkillManager : MonoBehaviour
{
    public event System.Action OnPassiveSkillChanged;

    [SerializeField] private Wall wall;

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
    }
    
    private bool TryGetSkillType(int skillId, out PassiveSkillType skillType)
    {
        skillType = PassiveSkillType.Damage;
        
        if (skillId < 22070 || skillId > 22087)
            return false;
        
        int offset = (skillId - 22070) % 6;
        skillType = (PassiveSkillType)(2206 + offset);
        return true;
    }
    
    public bool AddOrUpgradePassiveSkill(int skillId)
    {
        if (!TryGetSkillType(skillId, out PassiveSkillType skillType))
        {
            return false;
        }
        
        if (!skillGroups.ContainsKey(skillType))
        {
            return false;
        }
        
        PassiveSkillGroup group = skillGroups[skillType];
        
        if (group.currentStar > 0)
        {
            if (group.AddStar())
            {
                RecalculateEffects();
                OnPassiveSkillChanged?.Invoke();
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            group.currentStar = 1;
            RecalculateEffects();
            OnPassiveSkillChanged?.Invoke();
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
        PassiveSkillGroup shieldRegenGroup = null;

        bool shouldIncludeShieldRegen = ShouldIncludeShieldRegen();

        foreach (PassiveSkillGroup group in skillGroups.Values)
        {
            if (group.currentStar >= 3)
                continue;

            // ShieldRegen은 별도로 저장 (우선순위 처리용)
            if (group.skillType == PassiveSkillType.ShieldRegen)
            {
                if (shouldIncludeShieldRegen)
                    shieldRegenGroup = group;
                continue;
            }

            availableGroups.Add(group);
        }

        if (availableGroups.Count == 0 && shieldRegenGroup == null)
        {
            Debug.LogWarning("[PassiveSkillManager] 획득 가능한 패시브 스킬이 없습니다.");
            return skillIds;
        }

        // 일반 스킬 랜덤 선택 (ShieldRegen 슬롯 확보)
        int normalSkillCount = shieldRegenGroup != null ? count - 1 : count;

        for (int i = 0; i < normalSkillCount && availableGroups.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableGroups.Count);
            PassiveSkillGroup selected = availableGroups[randomIndex];

            int skillId = selected.currentStar == 0
                ? selected.GetSkillIdByStar(1)
                : selected.GetNextSkillId();

            skillIds.Add(skillId);

            availableGroups.RemoveAt(randomIndex);
        }

        // ShieldRegen을 마지막에 추가 (골드 카드 자리를 대체)
        if (shieldRegenGroup != null)
        {
            int shieldSkillId = shieldRegenGroup.currentStar == 0
                ? shieldRegenGroup.GetSkillIdByStar(1)
                : shieldRegenGroup.GetNextSkillId();

            skillIds.Add(shieldSkillId);
        }

        return skillIds;
    }

    /// <summary>
    /// ShieldRegen 스킬이 선택지에 포함되어야 하는지 확인
    /// 조건: 1) 이미 배운 경우, 2) 방벽이 피해를 받은 적 있는 경우, 3) 다른 5종류가 모두 MAX인 경우, 4) 획득 가능한 일반 스킬이 2개 이하인 경우
    /// </summary>
    private bool ShouldIncludeShieldRegen()
    {
        // 조건 1: ShieldRegen을 이미 배운 경우
        if (skillGroups[PassiveSkillType.ShieldRegen].currentStar > 0)
            return true;

        // 조건 2: 방벽이 피해를 받은 적 있는 경우
        if (wall != null && wall.HasEverTakenDamage)
            return true;

        // 조건 3, 4: ShieldRegen 제외 획득 가능한 스킬 개수 확인
        int availableCount = 0;
        foreach (var group in skillGroups.Values)
        {
            if (group.skillType != PassiveSkillType.ShieldRegen && group.currentStar < 3)
                availableCount++;
        }

        // 다른 5종류가 모두 MAX(3성)인 경우
        if (availableCount == 0)
            return true;

        // 획득 가능한 일반 스킬이 2개 이하면 해금 (골드 카드 대신 방벽회복)
        if (availableCount <= 2)
            return true;

        return false;
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
        LogOwnedSkills();
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
        OnPassiveSkillChanged?.Invoke();
    }
    
    public void LogOwnedSkills()
    {
        foreach (PassiveSkillGroup group in skillGroups.Values)
        {
            if (group.currentStar > 0)
            {
            }
        }
    }
    
}
