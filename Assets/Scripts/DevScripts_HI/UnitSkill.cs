using UnityEngine;

public abstract class UnitSkill : MonoBehaviour
{
    protected float lastUsedTime;
    protected float cooldown;

    public bool CanUse() => Time.time >= lastUsedTime + cooldown;

    public void TryExecute()
    {
        if (!CanUse()) 
            return;

        lastUsedTime = Time.time;
        OnExecute();
    }

    protected abstract void OnExecute();
}