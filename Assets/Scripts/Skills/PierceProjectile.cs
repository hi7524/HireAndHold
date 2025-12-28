using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PierceProjectile : MonoBehaviour, ISkillProjectile
{
    private const int EFFECT_SORTING_ORDER = 100;

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
    private bool sortingOrderSet = false;

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

        // Sorting Order 설정 (최초 1회)
        if (!sortingOrderSet)
        {
            SetEffectSortingOrder();
            sortingOrderSet = true;
        }

        // 방향 계산
        direction = (data.targetPosition - data.spawnPosition).normalized;

        // 회전 적용
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);

        // 이펙트 재생
        if (mainEffect != null)
            mainEffect.Play();

        // 발사 시 사운드 1회 재생
        PlayLaunchSound();
    }

    private void PlayLaunchSound()
    {
        if (string.IsNullOrEmpty(data.hitAudioClipName))
            return;

        if (SoundManager.Instance == null || AddressablePreloader.Instance == null)
            return;

        if (!AddressablePreloader.Instance.HasCachedAudioClip(data.hitAudioClipName))
            return;

        var clip = AddressablePreloader.Instance.GetCachedAudioClip(data.hitAudioClipName);
        if (clip != null)
            SoundManager.Instance.PlaySFX(clip);
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
