using UnityEngine;

public class EarthQuakeSkill : PlayerSkillBase
{
    [Header("吏吏??ㅽ궗 ?ㅼ젙")]
    [SerializeField] private float earthquakeRange = 10f;
    [SerializeField] private float effectLifetime = 3f;

    public override void OnUse(Vector3 spawnPoint)
    {
        Debug.Log("吏吏??ㅽ궗 ?ъ슜 ?꾩튂: " + spawnPoint);
        SpawnEffect(spawnPoint, effectLifetime);

        Debug.Log("[EarthQuake] 吏吏?諛쒕룞!");
        int hitCount = DamageAndApplyEffectInRange(spawnPoint, earthquakeRange);

        Debug.Log($"吏吏? {hitCount}留덈━ ?寃? ?곕?吏 {damage}");
    }
}
