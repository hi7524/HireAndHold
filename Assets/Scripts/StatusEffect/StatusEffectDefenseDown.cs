using UnityEngine;

/// <summary>
/// 방어력 감소 상태이상
/// amount = 받는 데미지 증가 비율 (0.3 = 30% 더 받음)
/// </summary>
public class StatusEffectDefenseDown : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.DefenseDown;
    
    private float damageMultiplier; // 데미지 배율 저장
    
    public StatusEffectDefenseDown(float amount, float duration, float tickInterval = 0.2f) 
        : base(amount, duration, tickInterval)
    {
        damageMultiplier = 1f + amount; // 0.3이면 1.3배 데미지
    }

    public override void OnStartEffect(GameObject target)
    {
        startTime = Time.time;
        // TODO: Monster에 damageMultiplier 적용하는 메서드 추가 필요
        // monster.SetDamageMultiplier(damageMultiplier);
        
        Debug.Log($"[DefenseDown] {target.name}의 방어력 감소! 받는 데미지 {amount * 100}% 증가 ({effectDuration}초)");
    }

    public override void WhileEffect(GameObject target)
    {
        // 방어력 감소는 지속 효과
    }

    public override void OnEndEffect(GameObject target)
    {
        // TODO: Monster의 damageMultiplier 복구
        // monster.SetDamageMultiplier(1f);
        
        Debug.Log($"[DefenseDown] {target.name}의 방어력 복구!");
    }
}