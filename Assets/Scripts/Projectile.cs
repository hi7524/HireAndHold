using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float attackDamage = 20f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Monster m = collision.gameObject.GetComponent<Monster>();
        if(m != null)
        {
            m.TakeDamage(attackDamage);
        }
    }
}
