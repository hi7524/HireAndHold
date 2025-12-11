using UnityEngine;

public class EarthQuakeSkill : PlayerSkillBase
{
    [Header("지진 스킬 설정")]
    [SerializeField] private float earthquakeRange = 10f;
    [SerializeField] private float effectLifetime = 3f;

    public override void OnUse(Vector3 spawnPoint)
    {
        Debug.Log("지진 스킬 사용 위치: " + spawnPoint);
        SpawnEffect(spawnPoint, effectLifetime);

        Debug.Log("[EarthQuake] 지진 발동!");
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, earthquakeRange);

        Debug.Log($"지진: {hitCount}마리 타격, 데미지 {damage}");
    }
}
