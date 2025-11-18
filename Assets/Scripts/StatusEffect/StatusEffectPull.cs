using UnityEngine;

/// <summary>
/// 끌어당기기 상태이상 (플레이어 쪽으로 당겨짐)
/// amount = 끌어당길 거리
/// </summary>
public class StatusEffectPull : StatusEffect
{
    public override StatusEffectType Type => StatusEffectType.Pull;
    
    private float pullForce;
    
    public StatusEffectPull(float amount, float duration, float tickInterval = 0.2f) 
        : base(amount, duration, tickInterval)
    {
        pullForce = amount;
    }

    public override void OnStartEffect(GameObject target)
    {
        startTime = Time.time;
        
        // 플레이어 방향으로 끌어당김
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector3 pullDirection = (player.transform.position - target.transform.position).normalized;
            
            // 즉시 끌어당김 적용
            target.transform.position += pullDirection * pullForce;
            
            Debug.Log($"[Pull] {target.name} 끌어당김! {pullForce}m 당겨짐!");
        }
    }

    public override void WhileEffect(GameObject target)
    {
        // 끌어당기기는 즉시 효과
    }

    public override void OnEndEffect(GameObject target)
    {
        // 끌어당기기 종료
    }
}