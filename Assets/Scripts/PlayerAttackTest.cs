using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttackTest : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
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
        GameObject projectileGo = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile projectile = projectileGo.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.SetTarget(target);
        }
    }



}
