using UnityEngine;

/// <summary>
/// 지속 데미지 상태이상 (DoT)
/// amount = 틱당 데미지
/// </summary>
public class StatusEffectDamageOverTime : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.DamageOverTime;
    
    public StatusEffectDamageOverTime(float amount, float duration, float tickInterval = 0.5f) 
        : base(amount, duration, tickInterval)
    {
    }

    public override void OnStartEffect(GameObject target)
    {
        startTime = Time.time;
        lastTickTime = Time.time;
    }

    public override void WhileEffect(GameObject target)
    {
        // 틱마다 데미지 적용
        if (TryTick())
        {
            var damageable = target.GetComponent<IDamagable>();
            if (damageable != null)
            {
                damageable.TakeDamage(amount);
            }
            UpdateTickTime();
        }
    }

    public override void OnEndEffect(GameObject target)
    {
    }
}