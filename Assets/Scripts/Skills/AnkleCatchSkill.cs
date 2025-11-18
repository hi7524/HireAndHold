using UnityEngine;


public class AnkleCatchSkill : PlayerSkillBase
{
    [Header("발목 잡기 스킬 설정")]
    [SerializeField] private GameObject catchEffectPrefab;
    [SerializeField] private float effectLifetime = 5f;
    
    public override void OnUse(Vector3 spawnPoint)
    {
        
        SpawnEffect(catchEffectPrefab, spawnPoint, effectLifetime);
        
        
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, Mathf.Infinity);
        
        Debug.Log($"[AnkleCatch] 발목 잡기 발동! 전체 {hitCount}마리 속박 ({statusEffectDuration}초), 데미지: {damage}");
    }
}