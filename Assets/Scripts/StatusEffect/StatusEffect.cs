using UnityEngine;

public enum StatusEffectType
{
    None = 0,
    Slow = 1,
    Stun = 2,
    DamageOverTime = 3,
    DefenseDown = 4,
    Root = 5,
    Knockback = 6,
    Pull = 7,   
    AttackUp = 8,
    DamageUpPercent = 9,
    DamageUpFlat = 10,
    FixedDamage = 11,   
    DamageTakenUpPercent = 12,
    CritRateUp = 13,
    CritDamageUp = 14,
    ExpGainUp = 15,
    BarrierRegenPerSecond = 16,
    BossDamageUp = 17

}

public abstract class StatusEffect
{
    protected StatusEffect effectType;

    protected float amount;     // 틱마다 적용할 수치
    protected float effectDuration; // 이펙트 지속될 시간
    protected float startTime;
    protected float lastTickTime;
    protected float tickInterval = 0.2f; // 틱 간격

    private bool forceStop = false;
    public bool IsEndEffect => forceStop || (Time.time >= startTime + effectDuration);
    public abstract StatusEffectType Type { get; }

    protected StatusEffect(float amount, float duration, float tickInterval = 0.2f)
    {
        this.amount = amount;
        effectDuration = duration;
        this.tickInterval = tickInterval;
        startTime = Time.time;
        lastTickTime = Time.time;

        forceStop = false;
    }

    public abstract void OnStartEffect(GameObject target);
    public abstract void WhileEffect(GameObject target);
    public abstract void OnEndEffect(GameObject target);

    public bool TryTick()
    {
        return Time.time >= lastTickTime + tickInterval;
    }

    public void UpdateTickTime()
    {
        lastTickTime = Time.time;
    }

    public void ForceStopEffect()
    {
        forceStop = true;
    }
}