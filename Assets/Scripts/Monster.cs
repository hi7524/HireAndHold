using UnityEditor;
using UnityEngine;

public class Monster : MonoBehaviour, IDamagable
{
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;
    [SerializeField] public float speed;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackCooldown;
    [SerializeField] private float defense;
    public float Defense{ get { return defense; } set { defense = value; } }

    private ObjectPoolManager poolManager;
    private string poolKey;
    private float nextAttackTime;
    private Transform targetWall;

    [SerializeField] private string expKey = "Exp";

    private bool isAttacking = false;
    private bool isDead = false;
    private bool isStunned = false; // 스턴 상태
    private float originalSpeed; // 원래 속도 저장

    public bool IsDead => isDead;
    public bool IsStunned => isStunned;

    //boss
    private bool isBoss = false;
    public float CurrentHp => currentHp;

    void Start()
    {
        currentHp = maxHp;
        nextAttackTime = 0f;
        targetWall = GameObject.FindWithTag("Wall")?.transform;
    }

    public void Initialize(ObjectPoolManager manager, string key, bool boss = false)
    {
        poolManager = manager;
        poolKey = key;
        isDead = false;
        currentHp = maxHp;
        isAttacking = false;
        isStunned = false; // 장철희
        originalSpeed = speed; // 장철희
        defense = 5; // 장철희

        isBoss = boss;

        if (isBoss)
        {
            maxHp *= 3f;
            currentHp = maxHp;
            speed *= 0.7f;
            attackDamage *= 2f;
            transform.localScale = transform.localScale * 3f;
        }
        else
        {
            transform.localScale = transform.localScale * 1;
        }
    }

    void Update()
    {
        if (isDead || targetWall == null)
        {
            return;
        }

        // 스턴 상태일 때는 움직임과 공격을 모두 정지 // 장철희
        if (isStunned)
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

        transform.Translate(Vector3.down * speed * Time.deltaTime);
        

        SeparateFromOthers();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            Wall wall = other.GetComponent<Wall>();

            if (wall != null)
            {
                wall.TakeDamage(attackDamage);
                isAttacking = true;
                TryAttackWall();
            }
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

                if (distance < 0.4f && distance > 0.1f)
                {
                    transform.position += dir.normalized * (0.4f - distance) * 0.25f;
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


        currentHp -= damage - defense;

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

        ExpItemSpawned();

        if (poolManager != null)
        {
            poolManager.Release(poolKey, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ExpItemSpawned()
    {
        if (poolManager == null)
        {
            return;
        }


        GameObject expObj = poolManager.Get("Exp");

        if (expObj != null)
        {
            expObj.transform.position = transform.position;

            Experience exp = expObj.GetComponent<Experience>();
            if (exp != null)
            {
                ExperienceCollector collector = GameObject.FindWithTag("Collector")?.GetComponent<ExperienceCollector>();
                if (collector != null)
                {
                    exp.SetExpCollecter(collector);
                }
            }
        }
    }

    
    public void SetStunned(bool stunned) // 장철희
    {
        isStunned = stunned;
        
        if (stunned)
        {
            Debug.Log($"[Monster] {gameObject.name} 스턴 적용!");
        }
        else
        {
            Debug.Log($"[Monster] {gameObject.name} 스턴 해제!");
        }
    }

    
    public void RestoreOriginalSpeed() // 장철희
    {
        speed = originalSpeed;
    }

}
