using UnityEngine;

public class EarthQuakeSkill : PlayerSkillBase
{
    [Header("지진 스킬 설정")]
    [SerializeField] private float earthquakeRange = 10f;
    [SerializeField] private float effectLifetime = 3f;

    public override void OnUse(Vector3 spawnPoint)
    {
        SpawnEffect(spawnPoint, effectLifetime);
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, earthquakeRange);
    }
}
