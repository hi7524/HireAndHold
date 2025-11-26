using UnityEngine;

public class Enemy : MonoBehaviour, IDamagable
{
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;
    [SerializeField] public float speed;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackCooldown;
    [SerializeField] private float defense;
    public float Defense { get { return defense; } set { defense = value; } }


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

    private void Awake()
    {
        originalScale = transform.localScale;
    }

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
        isStunned = false;
        originalSpeed = speed ;
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
        originalSpeed = speed ;
        defense = data.MON_DEF;
        Debug.Log($"[Monster] {data.MON_NAME} 데이터로 초기화 완료! HP: {maxHp}, ATK: {attackDamage}, DEF: {defense}");
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


        currentHp -= damage ; //- defense;

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
