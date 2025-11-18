using System;
using UnityEngine;

public abstract class PlayerSkillBase : MonoBehaviour
{
    [Header("기본 스킬 정보")]
    [SerializeField] protected float cooldown = 5f;
    [SerializeField] protected string skillName;
    [SerializeField] protected float damage = 10f;
    
    [Header("상태이상 설정")]
    [SerializeField] protected bool applyStatusEffect = false;
    [SerializeField] protected StatusEffectType statusEffectType;
    [SerializeField] protected float statusEffectDuration = 3f;
    [SerializeField] protected float statusEffectValue = 0f; // 데미지, 슬로우 비율 등
    
    public bool isOnCoolTime = false;

    public string SkillName => skillName;
    public bool IsOnCoolTime => isOnCoolTime;
    public float CoolDown => cooldown;

    public float elapsed = 0f;

    public float CooldownProgress => Mathf.Clamp01(elapsed/cooldown);

    public event Action<float> OnCooldownProgress;
    public event Action OnCooldownEnd;
    
    protected PlayerSkillSystem skillSystem;

    protected virtual void Awake()
    {
        // 플레이어의 SkillSystem 찾기 // 장철희
        skillSystem = GameObject.FindWithTag("Player")?.GetComponent<PlayerSkillSystem>();
        
        if (skillSystem == null)
        {
            Debug.LogWarning($"{skillName}: SkillSystem을 찾을 수 없습니다!");
        }
    }

    // 자식 클래스에서 구현할 메서드 // 장철희
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
    
    
    // 단일 타겟에게 상태이상 적용 // 장철희
    
    protected void ApplyStatusEffectToTarget(GameObject target)
    {
        if (!applyStatusEffect || skillSystem == null || target == null)
            return;
        
        skillSystem.ApplyStatusEffectToTarget(target, statusEffectType, statusEffectDuration, statusEffectValue);
    }
    
    // 범위 내 모든 적에게 상태이상 적용 // 장철희
    protected void ApplyStatusEffectInRange(Vector3 center, float range)
    {
        if (!applyStatusEffect || skillSystem == null)
            return;
        Debug.Log($"{skillName}: 범위 내 상태이상 적용 함수 진입");
        skillSystem.ApplyStatusEffectInRange(center, range, statusEffectType, statusEffectDuration, statusEffectValue);
    }
    
    // 가장 가까운 적에게 상태이상 적용 // 장철희
    
    protected void ApplyStatusEffectToNearest(Vector3 position)
    {
        if (!applyStatusEffect || skillSystem == null)
            return;
        
        skillSystem.ApplyStatusEffectToNearest(position, statusEffectType, statusEffectDuration, statusEffectValue);
    }
    
    
} 
