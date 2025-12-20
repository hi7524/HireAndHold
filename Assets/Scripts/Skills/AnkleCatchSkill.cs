using UnityEngine;

public class AnkleCatchSkill : PlayerSkillBase
{
    [Header("발목 잡기 스킬 설정")]
    [SerializeField] private float effectLifetime = 5f;

    public override void OnUse(Vector3 spawnPoint)
    {
        SpawnEffect(spawnPoint, effectLifetime);

        int hitCount = DamageAndApplyEffectInRange(spawnPoint, Mathf.Infinity);
    }
}
