using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 신속의 깃발 스킬 (SkillID: 22069)
/// EffectTable 기준:
/// - 391101: 공격 속도 75% 증가 (EFFECT_TYPE=9)
/// 지속시간: 10초
/// </summary>
public class FlagOfSpeedSkill : PlayerSkillBase, IUnitManagerInjectable
{
    [Header("신속의 깃발 스킬 설정")]
    [SerializeField] private float effectLifetime = 10f;

    [Header("버프 수치 (EffectTable 기준)")]
    [SerializeField] private float attackSpeedUpPercent = 75f; // 391101: 공격 속도 75% 증가
    [SerializeField] private float buffDuration = 10f;          // 버프 지속시간

    private BattleUnitManager battleUnitManager;
    private List<BuffedUnitInfo> buffedUnits = new List<BuffedUnitInfo>();

    public void SetBattleUnitManager(BattleUnitManager manager)
    {
        battleUnitManager = manager;
    }

    public override void OnUse(Vector3 spawnPoint)
    {
        // 아군 버프 스킬은 원점(0,0,0)에서 이펙트 생성
        SpawnEffect(Vector3.zero, effectLifetime);

        // 모든 아군 유닛에게 버프 적용
        ApplyBuffToAllUnits();

        Debug.Log($"[FlagOfSpeed] 신속의 깃발 발동! 아군 공격속도 {attackSpeedUpPercent}% 증가 ({buffDuration}초)");
    }

    private void ApplyBuffToAllUnits()
    {
        // 기존 버프 제거
        RemoveAllBuffs();

        // BattleUnitManager에서 유닛 목록 가져오기
        if (battleUnitManager == null)
        {
            Debug.LogWarning("[FlagOfSpeed] BattleUnitManager가 설정되지 않았습니다!");
            return;
        }

        List<Unit> units = battleUnitManager.GetAllUnits();

        foreach (Unit unit in units)
        {
            // 공격속도 버프 적용 (쿨타임 감소)
            // 공격속도 75% 증가 = 쿨타임 약 43% 감소 (1 / 1.75 ≈ 0.57)
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
        Debug.Log("[FlagOfSpeed] 신속의 깃발 버프 종료!");
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
