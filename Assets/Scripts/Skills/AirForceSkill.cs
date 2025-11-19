using UnityEngine;


public class AirForceSkill : PlayerSkillBase
{
    [Header("에어포스 스킬 설정")]
    [SerializeField] private GameObject airForceEffectPrefab;
    [SerializeField] private float effectLifetime = 2f;
    
    public override void OnUse(Vector3 spawnPoint)
    {
        
        SpawnEffect(airForceEffectPrefab, spawnPoint, effectLifetime);
        
        
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, Mathf.Infinity);
        
        Debug.Log($"[AirForce] 에어포스 발동! 전체 {hitCount}마리 넉백, 데미지: {damage}");
    }
}