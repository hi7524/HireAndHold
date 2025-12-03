using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Enemy : MonoBehaviour, IDamagable
{
    // === 애니메이션 상태 ===
    public enum AnimState
    {
        Idle,
        Move,
        Attack,
        Hit,
        Death
    }

    // 애니메이션 파라미터 이름 (Animator에서 동일하게 설정 필요)
    private static readonly int ANIM_IDLE = Animator.StringToHash("Idle");
    private static readonly int ANIM_ATTACK = Animator.StringToHash("Attack");
    private static readonly int ANIM_HIT = Animator.StringToHash("Hit");
    private static readonly int ANIM_DEATH = Animator.StringToHash("Death");

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
    private const float SEPARATE_INTERVAL = 0.1f;

    // === 비주얼 관련 필드 ===
    [SerializeField] private Transform visualRoot;
    private GameObject visualObject;
    private Animator visualAnimator;
    private AsyncOperationHandle<GameObject> visualHandle;
    private CancellationTokenSource cts;
    private string currentModelKey;

    // === 애니메이션 상태 필드 ===
    private AnimState currentAnimState = AnimState.Idle;
    private bool isPlayingHitAnim = false;
    private float hitAnimDuration = 0.2f;

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

        // 애니메이션 상태 초기화
        currentAnimState = AnimState.Idle;
        isPlayingHitAnim = false;
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

        // 애니메이션 상태 초기화
        currentAnimState = AnimState.Idle;
        isPlayingHitAnim = false;
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

    #region Animation Methods

    /// <summary>
    /// 애니메이션 상태를 변경합니다.
    /// </summary>
    /// <param name="state">변경할 애니메이션 상태</param>
    public void PlayAnimation(AnimState state)
    {
        if (visualAnimator == null) return;
        if (currentAnimState == state) return;

        // 죽음 상태에서는 다른 애니메이션으로 전환 불가
        if (currentAnimState == AnimState.Death && state != AnimState.Death) return;

        currentAnimState = state;

        // 모든 트리거 리셋
        ResetAllAnimTriggers();

        switch (state)
        {
            case AnimState.Idle:
                visualAnimator.SetTrigger(ANIM_IDLE);
                break;
            case AnimState.Attack:
                visualAnimator.SetTrigger(ANIM_ATTACK);
                break;
            case AnimState.Hit:
                visualAnimator.SetTrigger(ANIM_HIT);
                break;
            case AnimState.Death:
                visualAnimator.SetTrigger(ANIM_DEATH);
                break;
        }
    }

    /// <summary>
    /// 모든 애니메이션 트리거를 리셋합니다.
    /// </summary>
    private void ResetAllAnimTriggers()
    {
        if (visualAnimator == null) return;

        visualAnimator.ResetTrigger(ANIM_IDLE);
        visualAnimator.ResetTrigger(ANIM_ATTACK);
        visualAnimator.ResetTrigger(ANIM_HIT);
        visualAnimator.ResetTrigger(ANIM_DEATH);
    }

    /// <summary>
    /// 피격 애니메이션을 재생하고 일정 시간 후 이전 상태로 복귀합니다.
    /// </summary>
    private async UniTaskVoid PlayHitAnimationAsync()
    {
        if (isPlayingHitAnim || isDead) return;

        isPlayingHitAnim = true;
        AnimState previousState = currentAnimState;

        PlayAnimation(AnimState.Hit);

        await UniTask.Delay(TimeSpan.FromSeconds(hitAnimDuration), cancellationToken: cts.Token);

        isPlayingHitAnim = false;

        // 죽지 않았으면 이전 상태로 복귀
        if (!isDead)
        {
            if (isAttacking)
            {
                PlayAnimation(AnimState.Attack);
            }
            else
            {
                PlayAnimation(AnimState.Move);
            }
        }
    }

    /// <summary>
    /// 현재 애니메이션 상태를 반환합니다.
    /// </summary>
    public AnimState GetCurrentAnimState()
    {
        return currentAnimState;
    }

    #endregion

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

        // 이동 애니메이션 재생
        if (!isPlayingHitAnim && currentAnimState != AnimState.Move)
        {
            PlayAnimation(AnimState.Move);
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

        // 공격 애니메이션 재생
        if (!isPlayingHitAnim)
        {
            PlayAnimation(AnimState.Attack);
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

        if (damage < 999999f && currentHp > 0)
        {
            PlayHitAnimationAsync().Forget();
        }

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
        DieAsync().Forget();
    }

    private async UniTaskVoid DieAsync()
    {
        PlayAnimation(AnimState.Death);
        OnDeath?.Invoke(this);
        float deathAnimDuration = 0.5f;
        if (visualAnimator != null)
        {

            AnimatorClipInfo[] clipInfo = visualAnimator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                deathAnimDuration = clipInfo[0].clip.length;
            }
        }

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(deathAnimDuration), cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        ExpItemSpawned();

        

        ClearVisualChildren();
        ReleaseVisualHandle();

        SafeCancelCts();

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

    private void SafeCancelCts()
    {
        try
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        }
        catch (ObjectDisposedException)
        {
            // 이미 dispose된 경우 무시
        }
    }

    private void SafeDisposeCts()
    {
        try
        {
            cts?.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
        cts = null;
    }

    private void OnDisable()
    {
        ClearVisualChildren();
        ReleaseVisualHandle();
        SafeCancelCts();
    }

    private void OnDestroy()
    {
        ClearVisualChildren();
        ReleaseVisualHandle();
        SafeCancelCts();
        SafeDisposeCts();
    }
}
