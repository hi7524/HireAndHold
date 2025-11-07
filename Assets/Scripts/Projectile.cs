using Unity.VisualScripting;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform targetMonster;

    [SerializeField] private float speed;
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackCooldown;
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        Destroy(gameObject, lifeTime);
    }


    public void SetTarget(Transform target)
    {
        targetMonster = target;
    }

    private void Update()
    {
        if (targetMonster == null) return;

        Vector2 direction = (targetMonster.position - transform.position).normalized;
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Monster"))
        {
            IDamagable monster = other.GetComponent<IDamagable>();
            if (monster != null)
            {
                monster.TakeDamage(attackDamage);
            }
            Destroy(gameObject);
        }
    }

}
