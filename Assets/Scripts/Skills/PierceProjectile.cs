using System.Collections.Generic;
using UnityEngine;

public class PierceProjectile : MonoBehaviour, ISkillProjectile
{
    [SerializeField] private float launchSpeed = 10f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float pierceWidth = 0.5f;
    [SerializeField] private float rotationOffset = -90f; // 스프라이트가 위쪽을 향하는 경우 -90 (기본값)
    [SerializeField] private ParticleSystem mainEffect;

    private SkillProjectileData data;
    private Vector2 direction;
    private float spawnTime;
    private Rigidbody2D rb;
    private HashSet<Enemy> hitEnemies;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitEnemies = new HashSet<Enemy>();
    }

    public void Initialize(ref SkillProjectileData data)
    {
        this.data = data;
        hitEnemies.Clear();
    }

    public void Launch()
    {
        spawnTime = Time.time;
        transform.position = data.spawnPosition;

        // 방향 계산
        direction = (data.targetPosition - data.spawnPosition).normalized;

        // 회전 적용
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);

        // 이펙트 재생
        if (mainEffect != null)
            mainEffect.Play();
    }

    private void FixedUpdate()
    {
        if (Time.time >= spawnTime + lifeTime)
        {
            ReturnToPool();
            return;
        }

        rb.MovePosition(rb.position + direction * launchSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null && !enemy.IsDead && !hitEnemies.Contains(enemy))
        {
            hitEnemies.Add(enemy);
            enemy.TakeDamage(data.damage, data.isCritical);
        }
    }

    private void ReturnToPool()
    {
        if (data.poolManager != null)
            data.poolManager.Release(data.poolKey, gameObject);
    }

    private void OnDisable()
    {
        hitEnemies.Clear();
    }
}
