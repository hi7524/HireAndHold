using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttackTest : MonoBehaviour
{
    [SerializeField] private ObjectPoolManager poolManager;
    [SerializeField] private string projectileKey = "Projectile";
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireCooldown = 0.2f;

    private float nextFireTime;

    private void Update()
    {
        GameObject monster = GameObject.FindWithTag("Monster");
        if (monster == null)
        {
            return; 
        }

        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireCooldown;
            Fire(monster.transform);
        }
    }


    private void Fire(Transform target)
    {
        GameObject projectileGo = poolManager.Get(projectileKey);
        projectileGo.transform.position = firePoint.position;

        Projectile projectile = projectileGo.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(poolManager, projectileKey);
            projectile.SetTarget(target);
        }
    }



}
