using UnityEngine;

public class BlackHoleSkill : PlayerSkillBase
{
    [Header("블랙홀 스킬 설정")]
    [SerializeField] private float effectLifetime = 4f;

    public override void OnUse(Vector3 spawnPoint)
    {
        SpawnEffect(spawnPoint, effectLifetime);
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, Mathf.Infinity);
    }
}
