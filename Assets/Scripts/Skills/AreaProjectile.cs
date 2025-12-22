using UnityEngine;
using UnityEngine.VFX;

public class AreaProjectile : MonoBehaviour, ISkillProjectile
{
    private const int EFFECT_SORTING_ORDER = 100;

    [SerializeField] private float lifeTime = 1f;
    [SerializeField] private float damageDelay = 0f;
    [SerializeField] private ParticleSystem effect;
    [SerializeField] private VisualEffect vfxEffect;

    private SkillProjectileData data;
    private bool sortingOrderSet = false;

    public void Initialize(ref SkillProjectileData data)
    {
        this.data = data;
    }

    public void Launch()
    {
        // 타겟 위치에 즉시 이동
        transform.position = data.targetPosition;

        // Sorting Order 설정 (최초 1회)
        if (!sortingOrderSet)
        {
            SetEffectSortingOrder();
            sortingOrderSet = true;
        }

        // 이펙트 재생
        if (effect != null)
            effect.Play();
        if (vfxEffect != null)
            vfxEffect.Play();

        // 범위 내 적 데미지 (딜레이 적용)
        if (damageDelay > 0f)
            Invoke(nameof(DamageEnemiesInRange), damageDelay);
        else
            DamageEnemiesInRange();

        // 일정 시간 후 풀에 반환
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    private void DamageEnemiesInRange()
    {
        Vector2 pos = transform.position;

        // Physics2D로 범위 내 콜라이더 감지
        var hits = Physics2D.OverlapCircleAll(pos, data.range);
        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(data.damage, data.isCritical);
            }
        }
    }

    private void ReturnToPool()
    {
        if (data.poolManager != null)
            data.poolManager.Release(data.poolKey, gameObject);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void SetEffectSortingOrder()
    {
        var renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (var renderer in renderers)
        {
            renderer.sortingOrder = EFFECT_SORTING_ORDER;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, data.range);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.range);
    }
#endif
}
