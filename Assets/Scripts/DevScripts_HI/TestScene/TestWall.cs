using UnityEngine;

/// <summary>
/// 테스트 씬용 간단한 Wall
/// 기존 Wall 클래스와 동일한 인터페이스를 제공하지만 의존성 최소화
/// </summary>
public class TestWall : MonoBehaviour, IDamagable
{
    [SerializeField] private float maxHp = 10000f;
    [SerializeField] private float currentHp;
    [SerializeField] private bool isInvincible = true; // 테스트용 무적 모드
    
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    
    private void Awake()
    {
        currentHp = maxHp;
    }
    
    public void TakeDamage(float damage)
    {
        if (isInvincible) return;
        
        currentHp -= damage;
        Debug.Log($"[TestWall] 데미지: {damage}, 현재 HP: {currentHp}/{maxHp}");
        
        if (currentHp <= 0)
        {
            currentHp = maxHp; // 테스트용: 죽으면 바로 부활
            Debug.Log("[TestWall] HP가 0이 되어 자동 회복됨");
        }
    }
    
    public void SetInvincible(bool value)
    {
        isInvincible = value;
        Debug.Log($"[TestWall] 무적 모드: {isInvincible}");
    }
    
    public void SetMaxHp(float value)
    {
        maxHp = value;
        currentHp = maxHp;
    }
    
    public void Heal(float amount)
    {
        currentHp = Mathf.Min(currentHp + amount, maxHp);
    }

    public void Die()
    {
        // 테스트용: 죽어도 바로 부활
        currentHp = maxHp;
        Debug.Log("[TestWall] Die() 호출됨 - 자동 부활");
    }
}
