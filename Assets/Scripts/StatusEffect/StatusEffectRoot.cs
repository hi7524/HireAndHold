using UnityEngine;

/// <summary>
/// 속박 상태이상 (이동 불가, 공격은 가능)
/// Stun과 유사하지만 공격은 가능
/// </summary>
public class StatusEffectRoot : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.Root;
    
    private float originalSpeed;
    
    public StatusEffectRoot(float amount, float duration, float tickInterval = 0.2f) 
        : base(amount, duration, tickInterval)
    {
    }

    public override void OnStartEffect(GameObject target)
    {
        var monster = target.GetComponent<Monster>();
        if (monster != null)
        {
            originalSpeed = monster.speed;
            monster.speed = 0f; // 이속을 0으로
            
            startTime = Time.time;
            
            Debug.Log($"[Root] {target.name} 속박됨! {effectDuration}초간 이동 불가!");
        }
    }

    public override void WhileEffect(GameObject target)
    {
        // 속박 중에는 특별한 처리 불필요
    }

    public override void OnEndEffect(GameObject target)
    {
        var monster = target.GetComponent<Monster>();
        if (monster != null)
        {
            monster.speed = originalSpeed;
            
            Debug.Log($"[Root] {target.name} 속박 해제! 이속 복구!");
        }
    }
}