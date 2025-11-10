using UnityEngine;

public class UnitProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float launchSpeed = 3f;

    private float damage;
    private Monster target;
    private float spawnTime;
    private Rigidbody2D rb;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetDamage(float damage)
    {
        this.damage = damage;
    }

    public void SetTarget(Monster monster)
    {
        target = monster;
    }

    public void Launch()
    {
        spawnTime = Time.time;

        if (target != null)
        {
            Vector2 direction = (target.transform.position - transform.position).normalized;
            rb.AddForce(transform.up * launchSpeed, ForceMode2D.Impulse);
        }
    }

    private void Update()
    {
        if (Time.time >= spawnTime + lifeTime)
        {
            rb.linearVelocity = Vector2.zero;
            gameObject.SetActive(false);
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (target != null &&
            collision.gameObject.CompareTag(Tags.Monster) &&
            collision.gameObject == target.gameObject)
        {
            target.TakeDamage(damage);
            rb.linearVelocity = Vector2.zero;
            gameObject.SetActive(false);
        }
    }
}