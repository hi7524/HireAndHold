using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Enemy : MonoBehaviour, IDamagable
{
    public event Action<Enemy> OnDeath;
    public int MonsterId { get; private set; }
    
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;
    [SerializeField] public float speed;
    [SerializeField] private float defaultSpeed;
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

    // === 비주얼 관련 필드 ===
    [SerializeField] private Transform visualRoot; // 비주얼이 들어갈 부모 Transform
    private GameObject visualObject;
    private Animator visualAnimator;
    private AsyncOperationHandle<GameObject> visualHandle;
    private CancellationTokenSource cts;
    private string currentModelKey;

    private void Awake()
    {
        originalScale = transform.localScale;
        myCollider = GetComponent<Collider2D>();
        
        // visualRoot가 없으면 자신을 사용
        if (visualRoot == null)
        {
            visualRoot = transform;
        }
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
            speed *= 1.5f;
            transform.localScale = originalScale * 3f;
        }
        else
        {
            transform.localScale = originalScale * 1;
        }
        
        // CancellationTokenSource 초기화
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
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
        
        // CancellationTokenSource 초기화
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Addressable을 사용하여 비주얼 모델을 비동기로 로드합니다.
    /// </summary>
    /// <param name="modelKey">MonsterTable의 MON_MODEL 값 (Addressable 키)</param>
    public async UniTask LoadVisualAsync(string modelKey)
    {
        if (string.IsNullOrEmpty(modelKey))
        {
            Debug.LogWarning($"[Enemy] MON_MODEL is null or empty! MonsterId: {MonsterId}");
            return;
        }

        // 같은 모델이 이미 로드되어 있으면 스킵
        if (currentModelKey == modelKey && visualObject != null)
        {
            return;
        }

        try
        {
            // 이전 비주얼 정리
            ClearVisualChildren();
            ReleaseVisualHandle();

            currentModelKey = modelKey;
            
            // Addressable로 프리팹 로드
            visualHandle = Addressables.LoadAssetAsync<GameObject>(modelKey);
            var visualPrefab = await visualHandle.ToUniTask(cancellationToken: cts.Token);

            if (visualHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[Enemy] Failed to load visual prefab: {modelKey}");
                return;
            }

            // 프리팹 인스턴스화
            visualObject = Instantiate(visualPrefab, visualRoot);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = Vector3.one;

            // Animator 캐싱
            visualAnimator = visualObject.GetComponentInChildren<Animator>();
            if (visualAnimator == null)
            {
                Debug.LogWarning($"[Enemy] Animator not found in visual prefab: {modelKey}");
            }
        }
        catch (OperationCanceledException)
        {
            // 취소됨 - 정상적인 상황
        }
        catch (Exception e)
        {
            Debug.LogError($"[Enemy] Error loading visual {modelKey}: {e.Message}");
        }
    }

    /// <summary>
    /// 비주얼 자식 오브젝트를 제거합니다.
    /// </summary>
    private void ClearVisualChildren()
    {
        if (visualObject != null)
        {
            Destroy(visualObject);
            visualObject = null;
            visualAnimator = null;
        }
    }

    /// <summary>
    /// Addressable 핸들을 릴리즈합니다.
    /// </summary>
    private void ReleaseVisualHandle()
    {
        if (visualHandle.IsValid())
        {
            Addressables.Release(visualHandle);
        }
        currentModelKey = null;
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

        transform.Translate(Vector3.down * speed * Time.deltaTime * defaultSpeed);

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

        // 비주얼 정리
        ClearVisualChildren();
        ReleaseVisualHandle();
        
        // CancellationToken 취소
        cts?.Cancel();

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

    private void OnDisable()
    {
        // 풀로 반환될 때 정리
        ClearVisualChildren();
        ReleaseVisualHandle();
        cts?.Cancel();
    }

    private void OnDestroy()
    {
        ClearVisualChildren();
        ReleaseVisualHandle();
        cts?.Cancel();
        cts?.Dispose();
    }
}
