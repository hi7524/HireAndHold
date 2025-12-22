using UnityEngine;

public class SupernovaSkill : PlayerSkillBase
{
    [Header("슈퍼노바 스킬 설정")]
    [SerializeField] private float effectLifetime = 6f;

    public override void OnUse(Vector3 spawnPoint)
    {
        SpawnEffect(spawnPoint, effectLifetime);

        // 전체 범위 공격 (Mathf.Infinity = 무한 범위)
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, Mathf.Infinity);
    }
}
