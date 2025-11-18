using UnityEngine;

public class EarthQuakeSkill : PlayerSkillBase
{
    
  [Header("지진 스킬 설정")]
    [SerializeField] private float earthquakeRange = 10f;
    [SerializeField] private GameObject earthquakeEffectPrefab;
    
    public override void OnUse(Vector3 spawnPoint)
    {
        // 이펙트 생성
        SpawnEffect(earthquakeEffectPrefab, spawnPoint, 3f);
        
        // 데미지 + 상태이상 적용
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, earthquakeRange);
        
        Debug.Log($"지진: {hitCount}마리 타격, 데미지 {damage}");
    }
}
