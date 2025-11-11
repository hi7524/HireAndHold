using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private float attackInterval = 1.0f;
    [SerializeField] private float attackDamage = 5;
    [SerializeField] private UnitProjectile projectilePrf;

    [SerializeField] private ObjectPoolManager poolManager;
    [SerializeField] private string projectileKey = "Projectile";

    private Monster attackTarget;
    private float lastAttackTime;

    private void Update()
    {
        attackTarget = FindNearestTarget();

        if (attackTarget != null && Time.time >= lastAttackTime + attackInterval)
        {
            lastAttackTime = Time.time;
            Attack(attackTarget);
        }
    }

    // 사거리에 따라 적 감지
    private Monster FindNearestTarget()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, attackRange);

        Monster nearest = null;
        float minDis = attackRange;

        foreach (var coll in colliders)
        {
            Monster monster = coll.GetComponent<Monster>();

            if (monster != null && !monster.IsDead)
            {
                float distance = Vector3.Distance(transform.position, coll.transform.position);
                if (distance < minDis)
                {
                    minDis = distance;
                    nearest = monster;
                }
            }
        }

        return nearest;
    }

    // 타겟 공격
    private void Attack(Monster target)
    {
        //var projectile = Instantiate(projectilePrf, transform.position, Quaternion.identity);
        //projectile.SetDamage(attackDamage);
        //projectile.SetTarget(target);

        //projectile.Launch();

        // Pool 반환용 연결
        GameObject projectileObj = poolManager.Get(projectileKey);
        projectileObj.transform.position = transform.position;
        projectileObj.transform.rotation = Quaternion.identity;

        UnitProjectile projectile = projectileObj.GetComponent<UnitProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(poolManager, projectileKey); 
            projectile.SetDamage(attackDamage);
            projectile.SetTarget(target.transform);
            projectile.Launch();
        }
    }

    // 사거리 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    // 현재 타게팅된 적이 없다면 새로 찾기?
}
