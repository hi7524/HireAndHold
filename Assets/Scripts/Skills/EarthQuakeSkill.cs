using UnityEngine;

public class EarthQuakeSkill : PlayerSkillBase
{
    
  [Header("지진 스킬 설정")]
    [SerializeField] private float earthquakeRange = 10f;
    [SerializeField] private GameObject earthquakeEffectPrefab;
    
    public override void OnUse(Vector3 spawnPoint)
    {
        Debug.Log("지진 스킬 사용 위치: " + spawnPoint);
        SpawnEffect(earthquakeEffectPrefab, spawnPoint, 3f);
        
        Debug.Log("[EarthQuake] 지진 발동!");
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, earthquakeRange);
        
        Debug.Log($"지진: {hitCount}마리 타격, 데미지 {damage}");
    }
}
