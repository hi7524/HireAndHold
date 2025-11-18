using UnityEngine;


public class GreatSlowSkill : PlayerSkillBase
{
    [Header("그레이트 슬로우 스킬 설정")]
    [SerializeField] private GameObject greatSlowEffectPrefab;
    [SerializeField] private float effectLifetime = 8f;
    
    public override void OnUse(Vector3 spawnPoint)
    {
        
        SpawnEffect(greatSlowEffectPrefab, spawnPoint, effectLifetime);
        
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, Mathf.Infinity);
        
        Debug.Log($"[GreatSlow] 그레이트 슬로우 발동! 전체 {hitCount}마리 이속 {statusEffectValue * 100}% 감소 ({statusEffectDuration}초), 데미지: {damage}");
    }
}