using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class PlayerSkillBase : MonoBehaviour
{
    [Header("기본 스킬 정보")]
    [SerializeField] protected float cooldown = 5f;
    [SerializeField] protected int skillID;
    [SerializeField] protected float damage = 10f;
    
    [Header("상태이상 설정")]
    [SerializeField] protected bool applyStatusEffect = false;
    [SerializeField] protected StatusEffectType statusEffectType;
    [SerializeField] protected float statusEffectDuration = 3f;
    [SerializeField] protected float statusEffectValue = 0f; // 데미지, 슬로우 비율 등
    private SkillData skillData;
    
    public bool isOnCoolTime = false;

    public int SkillID => skillID;
    public bool IsOnCoolTime => isOnCoolTime;
    public float CoolDown => cooldown;

    public float elapsed = 0f;

    public float CooldownProgress => Mathf.Clamp01(elapsed/cooldown);

    public event Action<float> OnCooldownProgress;
    public event Action OnCooldownEnd;
    
    protected SkillEffectApplier skillEffectApplier;

    protected virtual void Awake()
    {
        // 플레이어의 SkillSystem 찾기 // 장철희
        
        skillEffectApplier = GameObject.FindWithTag("Player")?.GetComponent<SkillEffectApplier>();
        
        
    }
    
    protected virtual void Start()
    {
        
        LoadSkillDataAsync().Forget();
    }
    private async UniTaskVoid LoadSkillDataAsync()
    {
         while(!DataTableManager.IsInitialized)
    {
        await UniTask.Yield();  
    }
    
    // 데이터 로드
    skillData = DataTableManager.Get<DataTable_Skill>(DataTableIds.Skill)?.Get(skillID);
    
    if(skillData != null)
    {
         cooldown = skillData.SKILL_COOLTIME;
        damage = skillData.SKILL_CRT_DMG;
        applyStatusEffect = skillData.SKILL_CRT > 0;
        statusEffectType = (StatusEffectType)skillData.SKILL_EFFECT1; 
        statusEffectDuration = skillData.EFFECT_TIME1; 
        statusEffectValue = 0; // EffectTable 읽어와서 수정해야됨  
    }
    }

    // 자식 클래스에서 구현할 메서드 // 장철희
    public abstract void OnUse(Vector3 spawnPoint);

    public void TryUse(Vector3 spawnPoint)
    {
        if (skillData == null)
    {
        return;
    }
    
    if(isOnCoolTime) return;
    
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
        if (!applyStatusEffect || skillEffectApplier == null || target == null)
            return;
        
        skillEffectApplier.ApplyStatusEffectToTarget(target, statusEffectType, statusEffectDuration, statusEffectValue);
    }
    
    // 범위 내 모든 적에게 상태이상 적용 // 장철희
    protected void ApplyStatusEffectInRange(Vector3 center, float range)
    {
        if (!applyStatusEffect || skillEffectApplier == null)
            return;
        
        skillEffectApplier.ApplyStatusEffectInRange(center, range, statusEffectType, statusEffectDuration, statusEffectValue);
    }
    
    // 가장 가까운 적에게 상태이상 적용 // 장철희
    
    protected void ApplyStatusEffectToNearest(Vector3 position)
    {
        if (!applyStatusEffect || skillEffectApplier == null)
            return;
        
        skillEffectApplier.ApplyStatusEffectToNearest(position, statusEffectType, statusEffectDuration, statusEffectValue);
    }
    
    // 이펙트 생성 공통 메서드
    protected void SpawnEffect(GameObject effectPrefab, Vector3 position, float lifetime = 3f)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, lifetime);
        }
    }
    
    // 범위 내 모든 적에게 데미지 적용
    protected int DamageEnemiesInRange(Vector3 center, float range)
    {
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        int hitCount = 0;
        
        foreach (GameObject monster in monsters)
        {
            float distance = Vector3.Distance(center, monster.transform.position);
            
            if (distance <= range)
            {
                monster.GetComponent<IDamagable>()?.TakeDamage(damage);
                hitCount++;
            }
        }
        
        return hitCount;
    }
    
    // 범위 데미지 + 상태이상 적용을 한 번에 처리
    protected int DamageAndApplyEffectInRange(Vector3 center, float range)
    {
        int hitCount = DamageEnemiesInRange(center, range);
        ApplyStatusEffectInRange(center, range);
        return hitCount;
    }
    
    
    
} 
