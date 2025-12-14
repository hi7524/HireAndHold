using UnityEngine;

public class PlayerUnitProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float launchSpeed = 5f;
    [SerializeField] private ParticleSystem mainProjectile;
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private float rotationOffset = 0f; // 프리팹마다 기본 회전이 다를 경우 조정

    private ObjectPoolManager poolManager;
    private string poolKey;

    private float damage;
    private bool isCritical;
    private float spawnTime;
    private bool hasHit = false;
    private Vector2 direction;
    private Transform target;
    private Rigidbody2D rb;

    public void Initialize(ObjectPoolManager manager, string key)
    {
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

    public void Launch()
    {
        spawnTime = Time.time;
        hasHit = false;

        if (mainProjectile != null)
            mainProjectile.gameObject.SetActive(true);

        if (hitEffect != null)
            hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (target != null)
        {
            direction = (target.transform.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
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

            // 정지
            rb.linearVelocity = Vector2.zero;
            launchSpeed = 0f;
        }
    }

    private void OnParticleSystemStopped()
    {
        poolManager.Release(poolKey, gameObject);
    }
}
