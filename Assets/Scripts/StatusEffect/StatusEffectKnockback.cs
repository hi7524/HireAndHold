using UnityEngine;

/// <summary>
/// 넉백 상태이상 (뒤로 밀려남)
/// amount = 넉백 거리
/// </summary>
public class StatusEffectKnockback : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.Knockback;
    
    private Vector3 knockbackDirection;
    private float knockbackForce;
    
    public StatusEffectKnockback(float amount, float duration, float tickInterval = 0.2f) 
        : base(amount, duration, tickInterval)
    {
        knockbackForce = amount;
    }

    public override void OnStartEffect(GameObject target)
    {
        startTime = Time.time;
        
        
            knockbackDirection = Vector3.up;;
            
           
            target.transform.position += knockbackDirection * knockbackForce;
            
            Debug.Log($"[Knockback] {target.name} 넉백! {knockbackForce}m 밀려남!");
        
    }

    public override void WhileEffect(GameObject target)
    {
        
    }

    public override void OnEndEffect(GameObject target)
    {
        
    }
}