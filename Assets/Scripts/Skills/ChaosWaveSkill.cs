using UnityEngine;


public class ChaosWaveSkill : PlayerSkillBase
{
    [Header("혼돈의 파동 스킬 설정")]
    [SerializeField] private GameObject chaosEffectPrefab;
    [SerializeField] private float effectLifetime = 3f;
    
    public override void OnUse(Vector3 spawnPoint)
    {
       
        SpawnEffect(chaosEffectPrefab, spawnPoint, effectLifetime);
        
    
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, Mathf.Infinity);
        
        Debug.Log($"[ChaosWave] 혼돈의 파동 발동! 전체 {hitCount}마리 방어력 감소 ({statusEffectValue * 100}% 증뎀), 데미지: {damage}");
    }
}