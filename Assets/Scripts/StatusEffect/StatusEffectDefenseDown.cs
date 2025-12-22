using UnityEngine;


public class StatusEffectDefenseDown : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.DefenseDown;
    
    private float originalDefense;
    
    public StatusEffectDefenseDown(float amount, float duration, float tickInterval = 0.2f) 
        : base(amount, duration, tickInterval)
    {
        
    }

    public override void OnStartEffect(GameObject target)
    {
        startTime = Time.time;
        Enemy monster = target.GetComponent<Enemy>();
        if (monster != null)
        {
            originalDefense = monster.Defense;
            monster.Defense = originalDefense * (1 - amount/100f);
        }
    }

    public override void WhileEffect(GameObject target)
    {
        
    }

    public override void OnEndEffect(GameObject target)
    {
        Enemy monster = target.GetComponent<Enemy>();
        if (monster != null)
        {
            monster.Defense = originalDefense;
        }
    }
}