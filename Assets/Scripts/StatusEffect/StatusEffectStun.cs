using TMPro;
using UnityEngine;

public class StatusEffectStun : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.Stun;

    public StatusEffectStun(float amount, float duration, float tickInterval = 0.2f)
        : base(amount, duration, tickInterval)
    {

    }
    
    public override void OnStartEffect(GameObject target)
    {
        var monster = target.GetComponent<Enemy>();
        if (monster != null)
        {
            // 스턴 상태 활성화
            monster.SetStunned(true);
            startTime = Time.time;
        }
    }

    public override void WhileEffect(GameObject target)
    {
        // 스턴 중에는 특별한 처리가 필요하지 않음
        // Monster의 Update에서 이미 움직임을 차단하고 있음
    }

    public override void OnEndEffect(GameObject target)
    {
        var monster = target.GetComponent<Enemy>();
        if (monster != null)
        {
            // 스턴 상태 해제
            monster.SetStunned(false);
            monster.RestoreOriginalSpeed();
        }
    }
}