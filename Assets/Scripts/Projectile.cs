using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform targetMonster;

    [SerializeField] private float speed;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackCooldown;

    private ObjectPoolManager poolManager;
    private string poolKey;
    private Rigidbody2D rb;

    private bool isReturnedToPool = false; 

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(ObjectPoolManager manager, string key)
    {
        poolManager = manager;
        poolKey = key;
        isReturnedToPool = false; 
    }

    public void SetTarget(Transform target)
    {
        targetMonster = target;
    }

    private void Update()
    {
        if (targetMonster == null)
        {
            return;
        }


        Vector2 direction = (targetMonster.position - transform.position).normalized;
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isReturnedToPool)
        {
            return;
        }

        if (other.CompareTag("Monster"))
        {
            IDamagable monster = other.GetComponent<IDamagable>();

            if (monster != null)
            {
                monster.TakeDamage(attackDamage);
            }

            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (isReturnedToPool)
        {
            return;
        }

        isReturnedToPool = true;

        if (poolManager != null)
        {
            poolManager.Release(poolKey, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        isReturnedToPool = false;
    }
}
