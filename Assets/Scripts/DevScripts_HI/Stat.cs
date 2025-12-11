using System.Collections.Generic;
using UnityEngine;

public class Stat
{
    private float baseValue;           // 설계상 기본값
    private float upgradeValue;        // 영구 강화값 (메타 진행도)
    private List<StatModifier> modifiers = new List<StatModifier>();
    private float? cachedValue;

    public Stat(float baseValue)
    {
        this.baseValue = baseValue;
    }

    public float Value
    {
        get
        {
            if (cachedValue == null)
                cachedValue = CalculateFinalValue();
            return cachedValue.Value;
        }
    }

    // 영구 강화 설정 (게임 시작 시 한 번만)
    public void SetUpgradeValue(float value)
    {
        upgradeValue = value;
        cachedValue = null;
    }

    // 기본값 변경 (합성 시 모디파이어 보존용)
    public void SetBaseValue(float value)
    {
        baseValue = value;
        cachedValue = null;
    }

    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
        cachedValue = null;
    }

    public void RemoveModifier(StatModifier modifier)
    {
        modifiers.Remove(modifier);
        cachedValue = null;
    }

    private float CalculateFinalValue()
    {
        float finalValue = baseValue + upgradeValue;

        // Flat 가산
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].Type == ModifierType.Flat)
                finalValue += modifiers[i].Value;
        }

        // PercentAdd 가산
        float percentSum = 0f;
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].Type == ModifierType.PercentAdd)
                percentSum += modifiers[i].Value;
        }
        finalValue *= (1f + percentSum);

        // PercentMult 승산
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].Type == ModifierType.PercentMult)
                finalValue *= (1f + modifiers[i].Value);
        }

        return finalValue;
    }
}

public class StatModifier
{
    public float Value;
    public ModifierType Type;

    public StatModifier(float value, ModifierType type)
    {
        Value = value;
        Type = type;
    }
}

public enum ModifierType
{
    Flat,
    PercentAdd,
    PercentMult
}
