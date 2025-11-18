using UnityEngine;

public class ProjectileSkill : PlayerSkillBase
{
    [SerializeField] private GameObject skillprf;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float lifeTime = 3f;

    public override void OnUse(Vector3 spawnPoint)
    {
        GameObject obj = Instantiate(skillprf, spawnPoint, Quaternion.identity);
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();


        rb.linearVelocity = Vector2.up * speed;
        if (applyStatusEffect)
        {
            ApplyStatusEffectInRange(spawnPoint, 10f);
            
        }

        Destroy(obj, lifeTime);
    }
}
