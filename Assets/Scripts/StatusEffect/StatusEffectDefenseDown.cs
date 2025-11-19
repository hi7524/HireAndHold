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
        Monster monster = target.GetComponent<Monster>();
        if (monster != null)
        {
            originalDefense = monster.Defense;
            monster.Defense = originalDefense * (1 - amount/100f);
        }
        
        Debug.Log($"[DefenseDown] {target.name}의 방어력 감소! 적 방어력 {monster.Defense} ({effectDuration}초)");
    }

    public override void WhileEffect(GameObject target)
    {
        
    }

    public override void OnEndEffect(GameObject target)
    {
        Monster monster = target.GetComponent<Monster>();
        if (monster != null)
        {
            monster.Defense = originalDefense;
        }
        
        Debug.Log($"[DefenseDown] {target.name}의 방어력 복구!");
    }
}