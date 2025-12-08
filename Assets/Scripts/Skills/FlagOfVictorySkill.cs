using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 승리의 깃발 스킬 (SkillID: 22066)
/// EffectTable 기준:
/// - 38197: 공격력 25% 증가 (EFFECT_TYPE=8, AttackUp)
/// - 39198: 공격 속도 30% 증가 (EFFECT_TYPE=9, DamageUpPercent - 실제로는 공격속도)
/// 지속시간: 10초
/// </summary>
public class FlagOfVictorySkill : PlayerSkillBase
{
    [Header("승리의 깃발 스킬 설정")]
    [SerializeField] private GameObject flagEffectPrefab;
    [SerializeField] private float effectLifetime = 10f;

    [Header("버프 수치 (EffectTable 기준)")]
    [SerializeField] private float attackUpPercent = 25f;      // 38197: 공격력 25% 증가
    [SerializeField] private float attackSpeedUpPercent = 30f; // 39198: 공격 속도 30% 증가
    [SerializeField] private float buffDuration = 10f;         // 버프 지속시간

    private List<BuffedUnitInfo> buffedUnits = new List<BuffedUnitInfo>();

    public override void OnUse(Vector3 spawnPoint)
    {
        // 이펙트 생성
        SpawnEffect(flagEffectPrefab, spawnPoint, effectLifetime);

        // 모든 아군 유닛에게 버프 적용
        ApplyBuffToAllUnits();

        Debug.Log($"[FlagOfVictory] 승리의 깃발 발동! 아군 공격력 {attackUpPercent}% / 공격속도 {attackSpeedUpPercent}% 증가 ({buffDuration}초)");
    }

    private void ApplyBuffToAllUnits()
    {
        // 기존 버프 제거
        RemoveAllBuffs();

        // 모든 유닛 찾기
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);

        foreach (Unit unit in units)
        {
            // 공격력 버프 적용
            Stat attackStat = unit.GetAttackDamageStat();
            if (attackStat != null)
            {
                StatModifier attackModifier = new StatModifier(attackUpPercent / 100f, ModifierType.PercentAdd);
                attackStat.AddModifier(attackModifier);
                buffedUnits.Add(new BuffedUnitInfo(unit, attackStat, attackModifier));
            }

            // 공격속도 버프 적용 (쿨타임 감소)
            // 공격속도 30% 증가 = 쿨타임 약 23% 감소 (1 / 1.3 ≈ 0.77)
            Stat cooldownStat = unit.GetAttackCooltimeStat();
            if (cooldownStat != null)
            {
                float cooldownReduction = -1f * (1f - (1f / (1f + attackSpeedUpPercent / 100f)));
                StatModifier speedModifier = new StatModifier(cooldownReduction, ModifierType.PercentAdd);
                cooldownStat.AddModifier(speedModifier);
                buffedUnits.Add(new BuffedUnitInfo(unit, cooldownStat, speedModifier));
            }
        }

        // 버프 지속시간 후 제거
        RemoveBuffAfterDuration(buffDuration).Forget();
    }

    private async UniTaskVoid RemoveBuffAfterDuration(float duration)
    {
        await UniTask.Delay((int)(duration * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
        RemoveAllBuffs();
        Debug.Log("[FlagOfVictory] 승리의 깃발 버프 종료!");
    }

    private void RemoveAllBuffs()
    {
        foreach (var info in buffedUnits)
        {
            if (info.Unit != null && info.Stat != null)
            {
                info.Stat.RemoveModifier(info.Modifier);
            }
        }
        buffedUnits.Clear();
    }

    private void OnDestroy()
    {
        RemoveAllBuffs();
    }

    private struct BuffedUnitInfo
    {
        public Unit Unit;
        public Stat Stat;
        public StatModifier Modifier;

        public BuffedUnitInfo(Unit unit, Stat stat, StatModifier modifier)
        {
            Unit = unit;
            Stat = stat;
            Modifier = modifier;
        }
    }
}
