using UnityEngine;

public class SupernovaSkill : PlayerSkillBase
{
    [Header("슈퍼노바 스킬 설정")]
    [SerializeField] private float effectLifetime = 6f;

    public override void OnUse(Vector3 spawnPoint)
    {
        SpawnEffect(spawnPoint, effectLifetime);

        if (MonsterSpawner.Instance == null) return;

        var monsters = MonsterSpawner.Instance.GetActiveMonsters();
        int hitCount = 0;

        foreach (Enemy monster in monsters)
        {
            if (monster == null || !monster.gameObject.activeSelf) continue;

            monster.TakeDamage(damage);
            hitCount++;
        }

        Debug.Log($"[Supernova] 슈퍼노바 발동! 전체 {hitCount}마리 타격, 데미지: {damage}");
    }
}
