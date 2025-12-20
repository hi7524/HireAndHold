using UnityEngine;

public class ChaosWaveSkill : PlayerSkillBase
{
    [Header("혼돈의 파동 스킬 설정")]
    [SerializeField] private float effectLifetime = 3f;

    public override void OnUse(Vector3 spawnPoint)
    {
        SpawnEffect(spawnPoint, effectLifetime);

        int hitCount = DamageAndApplyEffectInRange(spawnPoint, Mathf.Infinity);
    }
}
