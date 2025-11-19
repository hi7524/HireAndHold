using UnityEngine;

/// <summary>
/// 넉백 상태이상 (부드럽게 밀려남)
/// amount = 넉백 거리
/// </summary>
public class StatusEffectKnockback : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.Knockback;

    private Vector3 knockbackDirection;
    private float knockbackDistance;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    public StatusEffectKnockback(float amount, float duration = 0.3f, float tickInterval = 0.02f)
        : base(amount, duration, tickInterval)
    {
        knockbackDistance = amount / 10f;
    }

    public override void OnStartEffect(GameObject target)
    {
        startTime = Time.time;
        lastTickTime = Time.time;

        startPosition = target.transform.position;

        knockbackDirection = Vector3.up;

        targetPosition = startPosition + knockbackDirection * knockbackDistance;

        Debug.Log($"[Knockback] {target.name} 넉백 시작! {knockbackDistance}m 밀려남!");
        // Todo 벽 공격하고 있으면 해제 하는 코드 추가

    }

    public override void WhileEffect(GameObject target)
    {
        
        float progress = (Time.time - startTime) / effectDuration;
        progress = Mathf.Clamp01(progress);

        
        float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

        
        target.transform.position = Vector3.Lerp(startPosition, targetPosition, easedProgress);
    }

    public override void OnEndEffect(GameObject target)
    {
        
        target.transform.position = targetPosition;

        Debug.Log($"[Knockback] {target.name} 넉백 종료!");
    }
}