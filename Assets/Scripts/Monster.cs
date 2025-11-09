using UnityEngine;

public class Monster : MonoBehaviour, IDamagable
{
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;
    [SerializeField] private float speed;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackCooldown;
    private float nextAttackTIime;
    private Transform targetWall;
    public float CurrentHp => currentHp;

    private bool isAttacking = false;

    // void Start()
    // {
    //     currentHp = maxHp;
    //     nextAttackTIime = 0f;
    //     targetWall = GameObject.FindWithTag("Wall").transform;
    // }

    // void Update()
    // {
    //     if (targetWall == null) return;

    //     if (!isAttacking)
    //     {
    //         MoveTowardsWall();
    //     }
    //     else
    //     {
    //         TryAttackWall();
    //     }
    // }

    // private void MoveTowardsWall()
    // {
      
    //     if (targetWall == null)
    //     {
    //         return;
    //     }

    //     transform.position = Vector3.MoveTowards(transform.position, targetWall.position, Time.deltaTime * speed);

    //     if (Vector3.Distance(transform.position, targetWall.position) <= attackRange)
    //     {
    //         isAttacking = true;
    //         TryAttackWall();
    //     }
      
    // }

    // private void TryAttackWall()
    // {
    //     if(Time.time >= nextAttackTIime)
    //     {
    //         AttackWall();
    //         nextAttackTIime = Time.time + attackCooldown;
    //     }
    // }

    // private void AttackWall()
    // {
    //     IDamagable wall = targetWall.GetComponent<IDamagable>();
    //     if(wall != null)
    //     {
    //         wall.TakeDamage(attackDamage);
    //     }
    // }

    public void Die()
    {
        Destroy(gameObject);
        Debug.Log("Monster 사망");
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        Debug.Log($"Monster 데미지 받음{damage}, 남은 HP: {currentHp}");
        ;
        if(currentHp <= 0)
        {
            Die();
        }
    }
}
