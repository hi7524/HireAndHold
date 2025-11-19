using UnityEngine;


public class SupernovaSkill : PlayerSkillBase
{
    [Header("슈퍼노바 스킬 설정")]
    [SerializeField] private GameObject supernovaEffectPrefab;
    [SerializeField] private float effectLifetime = 6f;
    
    public override void OnUse(Vector3 spawnPoint)
    {
       
        SpawnEffect(supernovaEffectPrefab, spawnPoint, effectLifetime);
        
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        int hitCount = 0;
        
        foreach (GameObject monster in monsters)
        {
            monster.GetComponent<IDamagable>()?.TakeDamage(damage);
            hitCount++;
        }
        
        Debug.Log($"[Supernova] 슈퍼노바 발동! 전체 {hitCount}마리 타격, 데미지: {damage}");
    }
}