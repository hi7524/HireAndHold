using System.Collections.Generic;
using UnityEngine;

public class BattleUnitManager : MonoBehaviour
{
    [SerializeField] private BuffManager buffManager;

    public List<Unit> GetAllUnits() => activeUnits;
    public int GetActiveUnitCount() => activeUnits.Count;

    private readonly List<Unit> activeUnits = new(); // 현재 유닛 담아 놓을 리스트
    private readonly Dictionary<Unit, StatModifier> activeBuffModifiers = new();

    private void Start()
    {
        if (buffManager != null)
            buffManager.OnBuffPercentageChanged += OnBuffChanged;
    }

    private void OnDestroy()
    {
        if (buffManager != null)
            buffManager.OnBuffPercentageChanged -= OnBuffChanged;
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
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit == null)
            return;

        RemoveBuffFromUnit(unit);
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
}