using UnityEngine;

/// <summary>
/// 버프 전용 상태 효과 (시각 효과만 표시, 실제 버프는 별도 로직에서 처리)
/// </summary>
public class StatusEffectBuff : StatusEffect
{
    private StatusEffectType buffType;

    public StatusEffectBuff(StatusEffectType type, float duration) : base(0f, duration)
    {
        buffType = type;
    }

    public override StatusEffectType Type => buffType;

    public override void OnStartEffect(GameObject target)
    {
        // 버프 시작 시 특별한 로직 없음 (시각 효과는 StatusEffectManager에서 처리)
    }

    public override void WhileEffect(GameObject target)
    {
        // 버프 지속 중 특별한 로직 없음
    }

    public override void OnEndEffect(GameObject target)
    {
        // 버프 종료 시 특별한 로직 없음 (실제 버프 제거는 스킬에서 처리)
    }
}
