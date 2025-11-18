using UnityEngine;

public class EternalBlizzardSkill : PlayerSkillBase
{
    [Header("블리자드 스킬 설정")]
    [SerializeField] private float blizzardRange = 12f;          
    [SerializeField] private GameObject blizzardEffectPrefab;    
    [SerializeField] private float effectLifetime = 5f;   
    
    public override void OnUse(Vector3 spawnPoint)
    {
        // 이펙트 생성
        SpawnEffect(blizzardEffectPrefab, spawnPoint, effectLifetime);
        
        // 데미지 + 상태이상 적용
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, blizzardRange);
        
        Debug.Log($"[EternalBlizzard] 블리자드 발동! {hitCount}마리 타격, " +
                  $"데미지: {damage}, 이속 감소: {statusEffectValue * 100}% / {statusEffectDuration}초");
    }
}