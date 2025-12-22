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
        if (currentHp <= 0)
        {
            currentHp = maxHp; // 테스트용: 죽으면 바로 부활
        }
    }
    
    public void SetInvincible(bool value)
    {
        isInvincible = value;
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
    }
}
