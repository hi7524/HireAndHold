using UnityEngine;

public class PassiveSkillEffects
{
    public float damageBonus = 0f;           // 피해량 증가
    public float critRateBonus = 0f;         // 치명타 확률 증가
    public float critDamageBonus = 0f;       // 치명타 피해량 증가
    public float expBonus = 0f;              // 획득 경험치 증가
    public float shieldRegenBonus = 0f;      // 초당 방벽 회복
    public float bossDamageBonus = 0f;       // 보스 피해량 증가
    
    public void Reset()
    {
        damageBonus = 0f;
        critRateBonus = 0f;
        critDamageBonus = 0f;
        expBonus = 0f;
        shieldRegenBonus = 0f;
        bossDamageBonus = 0f;
    }
}