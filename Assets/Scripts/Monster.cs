using UnityEngine;

public class Monster : MonoBehaviour, IDamagable
{
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;
    [SerializeField] private float speed;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackCooldown;

    private ObjectPoolManager poolManager;
    private string poolKey;
    private float nextAttackTime;
    private Transform targetWall;

    private bool isAttacking = false;
    private bool isDead = false; 

    public float CurrentHp => currentHp;

    void Start()
    {
        currentHp = maxHp;
        nextAttackTime = 0f;
        targetWall = GameObject.FindWithTag("Wall")?.transform;
    }

    public void Initialize(ObjectPoolManager manager, string key)
    {
        poolManager = manager;
        poolKey = key;
        isDead = false;
        currentHp = maxHp;
        isAttacking = false;
    }

    void Update()
    {
        if (isDead || targetWall == null)
        {
            return;
        }

        if (!isAttacking)
        {
            MoveTowardsWall();
        }
        else
        {
            TryAttackWall();
        }
    }

    private void MoveTowardsWall()
    {
        if (targetWall == null)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetWall.position, Time.deltaTime * speed);

        SeparateFromOthers();

        if (Vector3.Distance(transform.position, targetWall.position) <= attackRange)
        {
            isAttacking = true;
            TryAttackWall();
        }
    }

    private void SeparateFromOthers()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.5f);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject)
            {
                continue;
            }


            if (hit.CompareTag("Monster"))
            {
                Vector3 dir = transform.position - hit.transform.position;
                float distance = dir.magnitude;

                if (distance < 0.5f && distance > 0f)
                {
                    transform.position += dir.normalized * (0.5f - distance) * 0.5f;
                }
            }
        }
    }

    private void TryAttackWall()
    {
        if (Time.time >= nextAttackTime)
        {
            AttackWall();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void AttackWall()
    {
        if (isDead)
        {
            return;
        }

        IDamagable wall = targetWall.GetComponent<IDamagable>();
        if (wall != null)
        {
            wall.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            return;
        }


        currentHp -= damage;
        Debug.Log($"Monster 데미지 받음 {damage}, 남은 HP: {currentHp}");

        if (currentHp <= 0)
        {
            Die();
        }
            
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

            isDead = true;

        if (poolManager != null)
        {
            poolManager.Release(poolKey, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
