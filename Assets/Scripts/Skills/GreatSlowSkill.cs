using UnityEngine;

public class GreatSlowSkill : PlayerSkillBase
{
    [Header("그레이트 슬로우 스킬 설정")]
    [SerializeField] private float effectLifetime = 8f;

    public override void OnUse(Vector3 spawnPoint)
    {
        SpawnEffect(spawnPoint, effectLifetime);

        int hitCount = DamageAndApplyEffectInRange(spawnPoint, Mathf.Infinity);
    }
}
