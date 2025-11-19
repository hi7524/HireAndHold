using UnityEngine;

public class StatusEffectSlow : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.Slow;

    private float originalSpeed;

    public StatusEffectSlow(float amount, float duration, float tickInterval = 0.2f)
        : base(amount, duration, tickInterval)
    {
    }

    public override void OnStartEffect(GameObject target)
    {
        var monster = target.GetComponent<Monster>();
        if (monster != null)
        {
            originalSpeed = monster.speed;

            monster.speed = originalSpeed * (amount/100f);

            startTime = Time.time;

            Debug.Log($"[StatusEffectSlow] {target.name}에게 {effectDuration}초 슬로우 효과 시작! " +
                      $"속도 {originalSpeed:F2} -> {monster.speed:F2} ({amount * 100}% 감소)");
        }
    }

    public override void WhileEffect(GameObject target)
    {
        
    }

    public override void OnEndEffect(GameObject target)
    {
        var monster = target.GetComponent<Monster>();
        if (monster != null)
        {
            monster.speed = originalSpeed;

            Debug.Log($"[StatusEffectSlow] {target.name}의 슬로우 효과 종료! " +
                      $"속도 복구: {monster.speed:F2}");
        }
    }
}
