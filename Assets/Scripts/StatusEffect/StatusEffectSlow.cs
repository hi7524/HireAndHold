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
        var monster = target.GetComponent<Enemy>();
        if (monster != null)
        {
            originalSpeed = monster.speed;

            monster.speed = originalSpeed * (amount/100f);

            startTime = Time.time;
        }
    }

    public override void WhileEffect(GameObject target)
    {
        
    }

    public override void OnEndEffect(GameObject target)
    {
        var monster = target.GetComponent<Enemy>();
        if (monster != null)
        {
            monster.speed = originalSpeed;
        }
    }
}
