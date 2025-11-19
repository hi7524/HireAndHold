using System.Collections.Generic;
using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    private Dictionary<StatusEffectType, StatusEffect> statusEffects = new Dictionary<StatusEffectType, StatusEffect>();

    private readonly List<StatusEffectType> keysToRemove = new List<StatusEffectType>();

    private void OnEnable()
    {
        statusEffects.Clear();
        keysToRemove.Clear();
    }

    private void Update()
    {
        if (statusEffects.Count == 0)
            return;

        foreach (var effect in statusEffects.Values)
        {
            if (!effect.IsEndEffect)
            {
                effect.WhileEffect(gameObject);
            }
        }
        
        RemoveEffects();
    }

    public void AddStatusEffect(StatusEffectType type, StatusEffect effect)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type].OnEndEffect(gameObject);
            statusEffects.Remove(type);
        }

        statusEffects.Add(type, effect);
        statusEffects[type].OnStartEffect(gameObject);
    }
    
    private void RemoveEffects()
    {
        keysToRemove.Clear();
        
        foreach (var end in statusEffects)
        {
            if (end.Value.IsEndEffect)
            {
                keysToRemove.Add(end.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            statusEffects[key].OnEndEffect(gameObject);
            statusEffects.Remove(key);
        }
    }

    public void StopAllEffects()
    {
        foreach (var effect in statusEffects.Values)
        {
            effect.ForceStopEffect();
        }
    }
    
    public void RemoveStatusEffect(StatusEffectType type)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type].OnEndEffect(gameObject);
            statusEffects.Remove(type);
        }
    }
    
    public bool HasStatusEffect(StatusEffectType type)
    {
        return statusEffects.ContainsKey(type);
    }
}