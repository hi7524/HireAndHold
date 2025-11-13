using System;
using UnityEngine;

public abstract class PlayerSkillBase : MonoBehaviour
{
    [SerializeField] protected float cooldown = 5f;
    [SerializeField] protected string skillName;
    public bool isOnCoolTime = false;

    public string SkillName => skillName;
    public bool IsOnCoolTime => isOnCoolTime;
    public float CoolDown => cooldown;

    public float elapsed = 0f;

    public float CooldownProgress => Mathf.Clamp01(elapsed/cooldown);

    public event Action<float> OnCooldownProgress;

    public event Action OnCooldownEnd;

    public abstract void OnUse(Vector3 spawnPoint);

    public void TryUse(Vector3 spawnPoint)
    {
        if(isOnCoolTime)
        {
            return;
        }

        OnUse(spawnPoint);

        StartCooldown();
    }

    public void StartCooldown()
    {
        isOnCoolTime = true;
        elapsed = 0f;
    }

    public void Update()
    {
        if(!isOnCoolTime)
        {
            return;
        }

        elapsed += Time.deltaTime;

        OnCooldownProgress?.Invoke(elapsed/cooldown);

        if(elapsed >= cooldown)
        {
            isOnCoolTime=false;
            OnCooldownEnd?.Invoke();
        }
    }
} 
