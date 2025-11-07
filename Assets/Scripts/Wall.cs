using UnityEngine;

public class Wall : MonoBehaviour, IDamagable
{
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;
    public float CurrentHp => currentHp;

    void Start()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        Debug.Log($"벽 데미지 {damage} 받음");

        if(currentHp <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject);
        Debug.Log("벽 파괴됨");
    }    
}
