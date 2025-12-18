using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 용기의 깃발 스킬 (SkillID: 22068)
/// EffectTable 기준:
/// - 381100: 공격력 50% 증가 (EFFECT_TYPE=8, AttackUp)
/// 지속시간: 10초
/// </summary>
public class FlagOfCourageSkill : PlayerSkillBase, IUnitManagerInjectable
{
    [Header("용기의 깃발 스킬 설정")]
    [SerializeField] private float effectLifetime = 10f;

    [Header("버프 수치 (EffectTable 기준)")]
    [SerializeField] private float attackUpPercent = 50f; // 381100: 공격력 50% 증가
    [SerializeField] private float buffDuration = 10f;    // 버프 지속시간

    private BattleUnitManager battleUnitManager;
    private List<BuffedUnitInfo> buffedUnits = new List<BuffedUnitInfo>();

    public void SetBattleUnitManager(BattleUnitManager manager)
    {
        battleUnitManager = manager;
    }

    public override void OnUse(Vector3 spawnPoint)
    {
        // 아군 버프 스킬은 전달받은 spawnPoint에서 이펙트 생성
        SpawnEffect(spawnPoint, effectLifetime);

        // 모든 아군 유닛에게 버프 적용
        ApplyBuffToAllUnits();

        Debug.Log($"[FlagOfCourage] 용기의 깃발 발동! 아군 공격력 {attackUpPercent}% 증가 ({buffDuration}초)");
    }

    private void ApplyBuffToAllUnits()
    {
        // 기존 버프 제거
        RemoveAllBuffs();

        // BattleUnitManager에서 유닛 목록 가져오기
        if (battleUnitManager == null)
        {
            Debug.LogWarning("[FlagOfCourage] BattleUnitManager가 설정되지 않았습니다!");
            return;
        }

        List<Unit> units = battleUnitManager.GetAllUnits();

        foreach (Unit unit in units)
        {
            // 버프 이펙트 표시 (StatusEffectManager 사용)
            var statusEffectManager = unit.GetComponent<StatusEffectManager>();
            if (statusEffectManager != null)
            {
                var buffEffect = new StatusEffectBuff(StatusEffectType.AttackUp, buffDuration);
                statusEffectManager.AddStatusEffect(StatusEffectType.AttackUp, buffEffect);
            }

            // 공격력 버프 적용
            Stat attackStat = unit.GetAttackDamageStat();
            if (attackStat != null)
            {
                StatModifier attackModifier = new StatModifier(attackUpPercent / 100f, ModifierType.PercentAdd);
                attackStat.AddModifier(attackModifier);

                buffedUnits.Add(new BuffedUnitInfo(unit, attackStat, attackModifier));
            }
        }

        // 버프 지속시간 후 제거
        RemoveBuffAfterDuration(buffDuration).Forget();
    }

    private async UniTaskVoid RemoveBuffAfterDuration(float duration)
    {
        await UniTask.Delay((int)(duration * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
        RemoveAllBuffs();
        Debug.Log("[FlagOfCourage] 용기의 깃발 버프 종료!");
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
