using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class Wall : MonoBehaviour, IDamagable
{
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;
    // [SerializeField] private float maxExp = 100;
    [SerializeField] private float currentExp;

    public event Action<float, float> OnHpChanged;
    public event Action<float, float> OnExpChanged;

    public float CurrentHp => currentHp;
    public float MaxHp() => maxHp;
    // public float MaxExp () => maxExp;
    void Start()
    {
        currentHp = maxHp;
        OnHpChanged?.Invoke(currentHp, maxHp);
        // OnExpChanged?.Invoke(currentExp, maxExp);
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        Debug.Log($"벽 데미지 {damage} 받음");

        if(currentHp <= 0)
        {
            Die();
        }

        OnHpChanged?.Invoke(currentHp, maxHp);
    }

    public void TakeExp(float exp)
    {
        currentExp += exp;
        Debug.Log($"경험치 {exp} 만큼 증가");
        // OnExpChanged?.Invoke(currentExp, maxExp);
    }

    public void Die()
    {
        Destroy(gameObject);
        Debug.Log("벽 파괴됨");
    }
}
