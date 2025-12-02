using System;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamagable
{
    public event Action<Enemy> OnDeath;
    
    public int MonsterId { get; private set; }
    
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;
    [SerializeField] public float speed;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackCooldown;
    [SerializeField] private float defense;

    public float Defense { get { return defense; } set { defense = value; } }

    private FloatingTextSpawner floatingTextSpawner;


    private ObjectPoolManager poolManager;
    private string poolKey;
    private float nextAttackTime;
    private Transform targetWall;

    [SerializeField] private string expKey = "Exp";

    private bool isAttacking = false;
    private bool isDead = false;
    private bool isStunned = false;
    private float originalSpeed;


    public bool IsDead => isDead;
    public bool IsStunned => isStunned;

    //boss
    private bool isBoss = false;
    public bool IsBoss => isBoss;
    private Vector3 originalScale;
    public float CurrentHp => currentHp;
    private Collider2D currentWallCollider;


    private Collider2D myCollider;
    private IDamagable wallDamagable;
    private ExperienceCollector expCollector;

    private float lastSeparateTime;
    private const float SEPARATE_INTERVAL = 0.1f; // 0.1초마다만 실행


    private void Awake()
    {
        originalScale = transform.localScale;
        myCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        currentHp = maxHp;
        nextAttackTime = 0f;
        targetWall = GameObject.FindWithTag("Wall")?.transform;
        if (targetWall != null)
        {
            wallDamagable = targetWall.GetComponent<IDamagable>();
        }
        expCollector = GameObject.FindWithTag("Collector")?.GetComponent<ExperienceCollector>();
    }

    public void Initialize(ObjectPoolManager manager, string key, bool boss = false)
    {
        poolManager = manager;
        poolKey = key;
        isDead = false;
        currentHp = maxHp;
        isAttacking = false;
        isStunned = false;
        originalSpeed = speed;
        originalScale = transform.localScale;

        defense = 5;

        isBoss = boss;

        if (isBoss)
        {
            currentHp = maxHp;
            speed *= 0.7f;
            transform.localScale = originalScale * 3f;
        }
        else
        {
            transform.localScale = originalScale * 1;
        }
    }
    public void InitializeWithData(ObjectPoolManager manager, string key, MonsterData data, bool boss = false)
    {
        poolManager = manager;
        poolKey = key;
        isDead = false;
        maxHp = data.MON_HP;
        currentHp = maxHp;
        speed = data.MON_SPEED;
        attackDamage = data.MON_ATK;
        isAttacking = false;
        isStunned = false;
        originalSpeed = speed;
        defense = data.MON_DEF;
        MonsterId = data.MON_ID;

        isBoss = boss;

        if (isBoss)
        {
            currentHp = maxHp;
            speed *= 0.7f;
            attackDamage *= 2f;
            transform.localScale = originalScale * 3f;
        }
        else
        {
            transform.localScale = originalScale * 1;
        }
    }

    void Update()
    {
        if (isDead || targetWall == null)
        {
            return;
        }

       
        if (isStunned)
        {
            return;
        }

        if (isAttacking && currentWallCollider != null)
        {
            CheckWallProximity();
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
    private void CheckWallProximity()
    {
        if (currentWallCollider != null && myCollider != null)
        {
            bool isOverlapping = currentWallCollider.IsTouching(myCollider);

            if (!isOverlapping && isAttacking)
            {
                // 넉백으로 밀려났을 때 자동 해제
                isAttacking = false;
            }
        }
    }
    private void MoveTowardsWall()
    {
        if (targetWall == null)
        {
            return;
        }

        transform.Translate(Vector3.down * speed * Time.deltaTime);

        if (Time.time >= lastSeparateTime + SEPARATE_INTERVAL)
        {
            lastSeparateTime = Time.time;
            SeparateFromOthers();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            Wall wall = other.GetComponent<Wall>();

            if (wall != null)
            {
                currentWallCollider = other;
                wall.TakeDamage(attackDamage);
                isAttacking = true;
                TryAttackWall();
            }
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            if (isAttacking)
            {
                isAttacking = false;
                currentWallCollider = null;

            }
        }
    }

    private void SeparateFromOthers()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.5f);

        foreach (var hit in hits)
        {
            if (hit == myCollider)
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

       if (wallDamagable != null)
        {
            wallDamagable.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            return;
        }

        if (floatingTextSpawner != null && damage < 999999f)
        {
            Vector3 pos = transform.position + Vector3.up * 0.3f;
            floatingTextSpawner.SpawnText(pos, damage.ToString());
        }

        currentHp -= damage;

        if (currentHp <= 0)
        {
            isDead = true;
            Die();
        }
    }



    public void Die()
    {
        if (!isDead)
        {
            Debug.LogWarning("[Enemy] Die() called but isDead was false - this shouldn't happen!");
            return;
        }

        ExpItemSpawned();
        
        // 사망 이벤트 발생
        OnDeath?.Invoke(this);

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
            if (exp != null && expCollector != null)
            {
                exp.SetExpCollecter(expCollector);
            }
        }
    }


    public void SetStunned(bool stunned) 
    {
        isStunned = stunned;
    }


    public void RestoreOriginalSpeed()
    {
        speed = originalSpeed;
    }

    public void SetFloatingTextSpawner(FloatingTextSpawner spawner)
    {
        floatingTextSpawner = spawner;
    }

}
