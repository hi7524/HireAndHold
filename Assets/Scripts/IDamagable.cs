using UnityEngine;

public interface IDamagable
{
    float CurrentHp { get; }
    void TakeDamage(float damage);

    void Die();
    
}
