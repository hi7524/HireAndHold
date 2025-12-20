using UnityEngine;

public class AirForceSkill : PlayerSkillBase
{
    [Header("에어포스 스킬 설정")]
    [SerializeField] private float effectLifetime = 2f;

    public override void OnUse(Vector3 spawnPoint)
    {
        SpawnEffect(spawnPoint, effectLifetime);

        int hitCount = DamageAndApplyEffectInRange(spawnPoint, Mathf.Infinity);
    }
}
