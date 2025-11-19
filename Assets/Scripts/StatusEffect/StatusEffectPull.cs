using UnityEngine;

/// <summary>
/// 끌어당기기 상태이상 (부드럽게 끌어당김)
/// amount = 끌어당길 거리
/// </summary>
public class StatusEffectPull : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.Pull;

    private float pullDistance;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    public StatusEffectPull(float amount, float duration = 0.4f, float tickInterval = 0.02f)
        : base(amount, duration, tickInterval)
    {
        pullDistance = amount;
    }

    public override void OnStartEffect(GameObject target)
    {
        startTime = Time.time;
        lastTickTime = Time.time;

        startPosition = target.transform.position;

        Vector3 centerPosition = new Vector3(0f, 3f, 0f);// 하드코딩 박아놨지만 수정해야함


        Vector3 pullOffset = (centerPosition - startPosition).normalized * pullDistance;
        targetPosition = startPosition + pullOffset ;

        Debug.Log($"[Pull] {target.name} 끌어당김 시작! {pullDistance}m만큼 이동!");
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