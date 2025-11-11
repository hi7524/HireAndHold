using UnityEngine;

public class UnitProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float launchSpeed = 5f;

    private ObjectPoolManager poolManager;
    private string poolKey;
    private float damage;
    private Vector2 direction;
    private float spawnTime;
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
    }

    public void SetTarget(Transform target)
    {
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

        if (target != null)
        {
            Vector2 direction = (target.transform.position - transform.position).normalized;
            rb.AddForce(direction * launchSpeed, ForceMode2D.Impulse);
        }
    }

    private void FixedUpdate()
    {
        if (Time.time >= spawnTime + lifeTime)
        {
            gameObject.SetActive(false);
            return;
        }

        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Monster m = collision.GetComponent<Monster>();
        if (m != null && !m.IsDead)
        {
            m.TakeDamage(damage);
            gameObject.SetActive(false);
        }
    }
}
