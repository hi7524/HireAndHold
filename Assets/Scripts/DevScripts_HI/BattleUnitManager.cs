using System.Collections.Generic;
using UnityEngine;

public class BattleUnitManager : MonoBehaviour
{
    [SerializeField] private BuffManager buffManager;
    [SerializeField] private PassiveSkillManager passiveSkillManager;
    [SerializeField] private ObjectPoolManager poolManager;

    public List<Unit> GetAllUnits() => activeUnits;
    public int GetActiveUnitCount() => activeUnits.Count;

    private readonly List<Unit> activeUnits = new(); // 현재 유닛 담아 놓을 리스트
    private readonly Dictionary<Unit, StatModifier> activeBuffModifiers = new();
    private readonly Dictionary<Unit, List<StatModifier>> activePassiveModifiers = new();

    private void Awake()
    {
        // Unit에서 사용할 씬 참조 설정
        Unit.SetSceneReferences(poolManager, passiveSkillManager);
    }

    private void Start()
    {
        if (buffManager != null)
            buffManager.OnBuffPercentageChanged += OnBuffChanged;

        if (passiveSkillManager != null)
            passiveSkillManager.OnPassiveSkillChanged += OnPassiveSkillChanged;
    }

    private void OnDestroy()
    {
        Unit.ClearSceneReferences();

        if (buffManager != null)
            buffManager.OnBuffPercentageChanged -= OnBuffChanged;

        if (passiveSkillManager != null)
            passiveSkillManager.OnPassiveSkillChanged -= OnPassiveSkillChanged;
    }

    public void RegisterUnit(Unit unit)
    {
        if (unit == null || activeUnits.Contains(unit))
            return;

        activeUnits.Add(unit);

        if (buffManager != null)
        {
            ApplyBuffToUnit(unit, buffManager.GlobalBuffPercentage);
        }

        if (passiveSkillManager != null)
            ApplyPassiveToUnit(unit, passiveSkillManager.GetCurrentEffects());
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit == null)
            return;

        RemoveBuffFromUnit(unit);
        RemovePassiveFromUnit(unit);
        activeUnits.Remove(unit);
    }

    private void OnBuffChanged(float buffPercentage)
    {
        foreach (var unit in activeUnits)
        {
            UpdateUnitBuffs(unit, buffPercentage);
        }
    }

    private void UpdateUnitBuffs(Unit unit, float buffPercentage)
    {
        RemoveBuffFromUnit(unit);
        ApplyBuffToUnit(unit, buffPercentage);
    }

    private void ApplyBuffToUnit(Unit unit, float buffPercentage)
    {
        if (unit == null)
            return;

        Stat attackStat = unit.GetAttackDamageStat();
        if (attackStat == null)
            return;

        float buffValue = buffPercentage / 100f;
        StatModifier modifier = new StatModifier(buffValue, ModifierType.PercentAdd);

        attackStat.AddModifier(modifier);
        activeBuffModifiers[unit] = modifier;
    }

    private void RemoveBuffFromUnit(Unit unit)
    {
        if (unit == null)
            return;

        if (activeBuffModifiers.TryGetValue(unit, out StatModifier modifier))
        {
            Stat attackStat = unit.GetAttackDamageStat();
            if (attackStat != null)
            {
                attackStat.RemoveModifier(modifier);
            }

            activeBuffModifiers.Remove(unit);
        }
    }

    private void OnPassiveSkillChanged()
    {
        if (passiveSkillManager == null) return;

        var effects = passiveSkillManager.GetCurrentEffects();
        foreach (var unit in activeUnits)
        {
            RemovePassiveFromUnit(unit);
            ApplyPassiveToUnit(unit, effects);
        }
    }

    private void ApplyPassiveToUnit(Unit unit, PassiveSkillEffects effects)
    {
        if (unit == null || effects == null) return;

        var modifiers = new List<StatModifier>();

        // 공격력 증가
        if (effects.damageBonus > 0)
        {
            var damageMod = new StatModifier(effects.damageBonus / 100f, ModifierType.PercentAdd);
            unit.GetAttackDamageStat()?.AddModifier(damageMod);
            modifiers.Add(damageMod);
        }

        // 치명타 확률 증가
        if (effects.critRateBonus > 0)
        {
            var critRateMod = new StatModifier(effects.critRateBonus / 100f, ModifierType.PercentAdd);
            unit.GetCriticalRateStat()?.AddModifier(critRateMod);
            modifiers.Add(critRateMod);
        }

        // 치명타 데미지 증가
        if (effects.critDamageBonus > 0)
        {
            var critDamageMod = new StatModifier(effects.critDamageBonus / 100f, ModifierType.PercentAdd);
            unit.GetCriticalDamageStat()?.AddModifier(critDamageMod);
            modifiers.Add(critDamageMod);
        }

        activePassiveModifiers[unit] = modifiers;
    }

    private void RemovePassiveFromUnit(Unit unit)
    {
        if (unit == null) return;

        if (activePassiveModifiers.TryGetValue(unit, out var modifiers))
        {
            foreach (var mod in modifiers)
            {
                unit.GetAttackDamageStat()?.RemoveModifier(mod);
                unit.GetCriticalRateStat()?.RemoveModifier(mod);
                unit.GetCriticalDamageStat()?.RemoveModifier(mod);
            }
            activePassiveModifiers.Remove(unit);
        }
    }
}