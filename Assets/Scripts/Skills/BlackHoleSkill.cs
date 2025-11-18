using UnityEngine;


public class BlackHoleSkill : PlayerSkillBase
{
    [Header("블랙홀 스킬 설정")]
    [SerializeField] private GameObject blackHoleEffectPrefab;
    [SerializeField] private float effectLifetime = 4f;
    
    public override void OnUse(Vector3 spawnPoint)
    {
       
        SpawnEffect(blackHoleEffectPrefab, spawnPoint, effectLifetime);
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, Mathf.Infinity);
        
        Debug.Log($"[BlackHole] 블랙홀 발동! 전체 {hitCount}마리 끌어당김, 데미지: {damage}");
    }
}