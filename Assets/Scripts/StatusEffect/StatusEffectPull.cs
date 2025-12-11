using UnityEngine;

/// <summary>
/// 끌어당기기 상태이상 (부드럽게 끌어당김)
/// amount = 끌어당길 거리
/// centerPosition = 끌어당길 중심점 (스킬 발동 위치)
/// </summary>
public class StatusEffectPull : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.Pull;

    private float pullDistance;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 centerPosition;

    public StatusEffectPull(float amount, Vector3 center, float duration = 0.4f, float tickInterval = 0.02f)
        : base(amount, duration, tickInterval)
    {
        pullDistance = amount;
        centerPosition = center;
    }

    public override void OnStartEffect(GameObject target)
    {
        startTime = Time.time;
        lastTickTime = Time.time;

        startPosition = target.transform.position;

        Vector3 pullOffset = (centerPosition - startPosition).normalized * pullDistance;
        targetPosition = startPosition + pullOffset;

        Debug.Log($"[Pull] {target.name} 끌어당김 시작! {pullDistance}m만큼 {centerPosition} 방향으로 이동!");
        // Todo 벽 공격하고 있으면 해제 하는 코드 추가
    }

    public override void WhileEffect(GameObject target)
    {
        
        float progress = (Time.time - startTime) / effectDuration;
        progress = Mathf.Clamp01(progress);

        
        float easedProgress = Mathf.Pow(progress, 2f);

        
        target.transform.position = Vector3.Lerp(startPosition, targetPosition, easedProgress);
    }

    public override void OnEndEffect(GameObject target)
    {
        
        

        Debug.Log($"[Pull] {target.name} 끌어당김 종료!");
    }
}