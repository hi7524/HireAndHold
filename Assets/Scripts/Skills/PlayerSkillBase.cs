using System;
using System.Linq;
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

    public float CooldownProgress => Mathf.Clamp01(elapsed / cooldown);

    public event Action<float> OnCooldownProgress;
    public event Action OnCooldownEnd;

    protected SkillEffectApplier skillEffectApplier;
    public bool IsSkillDataLoaded => skillData != null;

    protected virtual void Awake()
    {
        skillEffectApplier = GetComponent<SkillEffectApplier>();
        if (skillEffectApplier == null)
        {
            skillEffectApplier = gameObject.AddComponent<SkillEffectApplier>();
        }
    }



    protected virtual void Start()
    {
        LoadSkillDataAsync().Forget();
    }
    public void Init()
    {
        LoadSkillDataAsync().Forget();
    }
    private async UniTaskVoid LoadSkillDataAsync()
    {
        while (!DataTableManager.IsInitialized)
        {
            await UniTask.Yield();
        }
        isOnCoolTime = false;
        skillData = DataTableManager.Get<DataTable_Skill>(DataTableIds.Skill)?.Get(skillID);

        if (skillData != null)
        {
            cooldown = skillData.SKILL_COOLTIME;
            damage = DataTableManager.EffectTable.Get(skillData.SKILL_EFFECT2_ID).EFFECT_VALUE;
            statusEffectType = (StatusEffectType)skillData.SKILL_EFFECT1_ID;
            statusEffectDuration = skillData.EFFECT_TIME1;
            statusEffectValue = 0; 
        }
    }


    public abstract void OnUse(Vector3 spawnPoint);

    public void TryUse(Vector3 spawnPoint)
    {
        if (skillData == null)
        {
            Debug.LogWarning($"스킬 데이터 로딩 중... Inspector 값으로 실행");
        
        }
        if (skillEffectApplier == null)
        {
            skillEffectApplier = GetComponent<SkillEffectApplier>();
            if (skillEffectApplier == null)
            {
                skillEffectApplier = gameObject.AddComponent<SkillEffectApplier>();
            }
        }
        if (isOnCoolTime) return;
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
        if (!isOnCoolTime)
        {
            return;
        }

        elapsed += Time.deltaTime;
        OnCooldownProgress?.Invoke(elapsed / cooldown);

        if (elapsed >= cooldown)
        {
            isOnCoolTime = false;
            OnCooldownEnd?.Invoke();
        }
    }


    protected void ApplyStatusEffectToTarget(GameObject target)
    {
        if (!applyStatusEffect || skillEffectApplier == null || target == null)
            return;

        skillEffectApplier.ApplyStatusEffectToTarget(target, statusEffectType, statusEffectDuration, statusEffectValue);
    }
    protected void ApplyStatusEffectInRange(Vector3 center, float range)
    {
        if (!applyStatusEffect || skillEffectApplier == null)
            return;
        skillEffectApplier.ApplyStatusEffectInRange(center, range, statusEffectType, statusEffectDuration, statusEffectValue);
    }


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
            Debug.Log($"[PlayerSkillBase] 이펙트 생성: {effectPrefab.name} at {position}");
            Destroy(effect, lifetime);
        }
    }

    // 범위 내 모든 적에게 데미지 적용
    protected int DamageEnemiesInRange(Vector3 center, float range)
    {
        if (MonsterSpawner.Instance == null) return 0;

        var monsters = MonsterSpawner.Instance.GetActiveMonsters();
        int hitCount = 0;

        foreach (Enemy monster in monsters.ToArray())
        {
            if (monster == null || !monster.gameObject.activeSelf) continue;

            float distance = Vector3.Distance(center, monster.transform.position);

            if (distance <= range)
            {
                monster.TakeDamage(damage);
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
