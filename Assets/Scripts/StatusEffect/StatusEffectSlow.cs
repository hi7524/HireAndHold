using UnityEngine;

public class StatusEffectSlow : StatusEffect
{
    public override StatusEffectType Type => throw new System.NotImplementedException();
    public StatusEffectSlow(float amount, float duration, float tickInterval = 0.2f) : base(amount, duration,tickInterval)
    {
        
    }

    public override void OnStartEffect(GameObject target)
    {
        Debug.Log($"[StatusEffectSlow] {target.name}에게 {effectDuration}초 슬로우 효과 시작! 이동속도 {amount}% 감소.");
    }

    public override void WhileEffect(GameObject target)
    {
        
    }
    
    public override void OnEndEffect(GameObject target)
    {
        Debug.Log($"[StatusEffectSlow] {target.name}의 슬로우 효과 종료! 이동속도 원래대로 복구.");
    }

}
