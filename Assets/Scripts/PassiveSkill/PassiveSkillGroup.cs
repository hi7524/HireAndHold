using System;
using UnityEngine;

public enum PassiveSkillType
{
    Damage = 2206,          // 피해량 증가
    CritRate = 2207,        // 치명타 확률 증가
    CritDamage = 2208,      // 치명타 피해량 증가
    Exp = 2209,             // 획득 경험치 증가
    ShieldRegen = 2210,     // 초당 방벽 회복
    BossDamage = 2211       // 보스 피해량 증가
}

[Serializable]
public class PassiveSkillGroup
{
    public PassiveSkillType skillType;
    public string displayName;
    public int currentStar;  // 0~3
    
    // 스킬 ID 상수
    private const int STAR1_BASE = 22070;
    private const int STAR2_BASE = 22076;
    private const int STAR3_BASE = 22082;
    
    public PassiveSkillGroup(PassiveSkillType type, string name)
    {
        skillType = type;
        displayName = name;
        currentStar = 0;
    }
    
    public bool AddStar()
    {
        if (currentStar >= 3) return false;
        currentStar++;
        return true;
    }
    
    public int GetCurrentSkillId()
    {
        if (currentStar <= 0) return -1;
        return GetSkillIdByStar(currentStar);
    }
    
    public int GetNextSkillId()
    {
        if (currentStar >= 3) return -1;
        return GetSkillIdByStar(currentStar + 1);
    }
    
    /// <summary>
    /// 특정 성급의 스킬 ID 계산
    /// </summary>
    public int GetSkillIdByStar(int star)
    {
        if (star < 1 || star > 3) return -1;
        
        int typeOffset = (int)skillType - 2206;
        
        int baseId;
        if (star == 1)
            baseId = STAR1_BASE;
        else if (star == 2)
            baseId = STAR2_BASE;
        else
            baseId = STAR3_BASE;
        
        return baseId + typeOffset;
    }
    
}
