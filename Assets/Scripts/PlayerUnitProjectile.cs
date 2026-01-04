using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PlayerUnitProjectile : MonoBehaviour, ISkillProjectile
{
    private const int EFFECT_SORTING_ORDER = 100;

    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float launchSpeed = 5f;
    [SerializeField] private ParticleSystem mainProjectile;
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private float rotationOffset = -90f; // 스프라이트가 위쪽을 향하는 경우 -90 (기본값)

    private ObjectPoolManager poolManager;
    private string poolKey;
    private bool sortingOrderSet = false;

    private float damage;
    private bool isCritical;
    private float spawnTime;
    private bool hasHit = false;
    private Vector2 direction;
    private Transform target;
    private Rigidbody2D rb;
    private string hitAudioClipName;
    private bool useLegacyMode = true;

    // ISkillProjectile 구현
    public void Initialize(ref SkillProjectileData data)
    {
        useLegacyMode = false;
        poolManager = data.poolManager;
        poolKey = data.poolKey;
        damage = data.damage;
        isCritical = data.isCritical;
        target = data.target;
        transform.position = data.spawnPosition;
        hitAudioClipName = data.hitAudioClipName;

        // customDirection이 있으면 사용, 없으면 targetPosition에서 방향 계산
        if (data.customDirection != Vector3.zero)
        {
            direction = data.customDirection.normalized;
        }
        else
        {
            direction = (data.targetPosition - data.spawnPosition).normalized;
        }
    }

    // 레거시 호환용
    public void Initialize(ObjectPoolManager manager, string key)
    {
        useLegacyMode = true;
        poolManager = manager;
        poolKey = key;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
        isCritical = false;
    }

    public void SetDamage(float dmg, bool critical)
    {
        damage = dmg;
        isCritical = critical;
    }

    public void SetTarget(Transform target)
    {
        this.target = target;

        if (target != null)
        {
            direction = (target.position - transform.position).normalized;
        }
        else
        {
            direction = transform.up;
        }
    }

    public void SetHitAudioClip(string clipName)
    {
        hitAudioClipName = clipName;
    }

    public void Launch()
    {
        spawnTime = Time.time;
        hasHit = false;

        // Sorting Order 설정 (최초 1회)
        if (!sortingOrderSet)
        {
            SetEffectSortingOrder();
            sortingOrderSet = true;
        }

        if (mainProjectile != null)
            mainProjectile.gameObject.SetActive(true);

        if (hitEffect != null)
            hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
    }

    private void SetEffectSortingOrder()
    {
        var renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (var renderer in renderers)
        {
            renderer.sortingOrder = EFFECT_SORTING_ORDER;
        }
    }

    private void FixedUpdate()
    {
        if (Time.time >= spawnTime + lifeTime)
        {
            gameObject.SetActive(false);
            return;
        }

        rb.MovePosition(rb.position + direction * launchSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit)
            return;

        Enemy m = collision.GetComponent<Enemy>();
        if (m != null && !m.IsDead)
        {
            hasHit = true;

            m.TakeDamage(damage, isCritical);

            if (mainProjectile != null)
                mainProjectile.gameObject.SetActive(false);

            if (hitEffect != null)
                hitEffect.Play();

            // Hit 사운드 재생
            PlayHitSound();

            // 정지
            rb.linearVelocity = Vector2.zero;
            launchSpeed = 0f;
        }
    }

    private async void PlayHitSound()
    {
        if (string.IsNullOrEmpty(hitAudioClipName) || SoundManager.Instance == null)
            return;

        AudioClip clip = null;
        
        if (AddressablePreloader.Instance != null && AddressablePreloader.Instance.HasCachedAudioClip(hitAudioClipName))
        {
            clip = AddressablePreloader.Instance.GetCachedAudioClip(hitAudioClipName);

            if (clip != null)
            {
                // 유닛 공격 소리는 4배 볼륨으로 재생
                SoundManager.Instance.PlaySFX(clip, 4f);
            }
        }
    }

    private void OnParticleSystemStopped()
    {
        poolManager.Release(poolKey, gameObject);
    }
}
