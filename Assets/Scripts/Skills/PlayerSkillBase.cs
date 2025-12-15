using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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

    // Addressable 이펙트 프리팹 캐시
    protected GameObject cachedEffectPrefab;

    // 직접 Addressable 로드 시 핸들 저장 (캐시 히트 시에는 사용 안함)
    private AsyncOperationHandle<GameObject> effectHandle;

    // true면 OnDestroy에서 핸들 Release 필요 (캐시에서 가져온 경우 false)
    private bool isEffectLoadedFromAddressable = false;

    /// <summary>
    /// 이펙트 프리팹 로드 완료 여부
    /// </summary>
    public bool IsEffectLoaded => cachedEffectPrefab != null;

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

            // SKILL_EFFECT2_ID가 유효한 경우에만 데미지 로드
            if (skillData.SKILL_EFFECT2_ID > 0)
            {
                var effectData = DataTableManager.EffectTable.Get(skillData.SKILL_EFFECT2_ID);
                if (effectData != null)
                {
                    damage = effectData.EFFECT_VALUE;
                }
            }

            // SKILL_EFFECT1_ID로 EffectTable 조회하여 상태이상 타입/값 로드
            if (skillData.SKILL_EFFECT1_ID > 0)
            {
                var statusEffectData = DataTableManager.EffectTable.Get(skillData.SKILL_EFFECT1_ID);
                if (statusEffectData != null)
                {
                    statusEffectType = (StatusEffectType)statusEffectData.EFFECT_TYPE;
                    statusEffectValue = statusEffectData.EFFECT_VALUE;
                    applyStatusEffect = true;
                }
            }
            statusEffectDuration = skillData.EFFECT_TIME1;

            // Addressable 이펙트 로드
            await LoadEffectPrefabAsync();
        }
    }

    // CSV SKILL_EFFECT 키로 이펙트 로드. 캐시 우선 → Addressable 직접 로드 순서
    private async UniTask LoadEffectPrefabAsync()
    {
        if (skillData == null || string.IsNullOrEmpty(skillData.SKILL_EFFECT))
            return;

        string effectKey = skillData.SKILL_EFFECT;

        // 캐시 히트 시 핸들 관리 불필요
        if (AddressablePreloader.Instance != null)
        {
            var cached = AddressablePreloader.Instance.GetCachedPrefab(effectKey);
            if (cached != null)
            {
                cachedEffectPrefab = cached;
                return;
            }
        }

        // 캐시 미스 시 직접 로드 - 핸들 저장하여 OnDestroy에서 Release
        try
        {
            effectHandle = Addressables.LoadAssetAsync<GameObject>(effectKey);
            cachedEffectPrefab = await effectHandle.ToUniTask();

            if (effectHandle.Status == AsyncOperationStatus.Succeeded)
            {
                isEffectLoadedFromAddressable = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerSkillBase] 이펙트 로드 실패: {effectKey}, {e.Message}");
        }
    }

    /// <summary>
    /// Addressable 핸들 해제 (직접 로드한 경우에만)
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (isEffectLoadedFromAddressable && effectHandle.IsValid())
        {
            Addressables.Release(effectHandle);
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

    /// <summary>
    /// Addressable에서 로드한 이펙트 프리팹으로 이펙트 생성
    /// </summary>
    /// <param name="position">생성 위치</param>
    /// <param name="lifetime">이펙트 지속 시간 (초)</param>
    protected void SpawnEffect(Vector3 position, float lifetime = 3f)
    {
        if (cachedEffectPrefab != null)
        {
            GameObject effect = Instantiate(cachedEffectPrefab);
            effect.transform.position = position;
            Destroy(effect, lifetime);
        }
        else
        {
            Debug.LogWarning($"[PlayerSkillBase] 이펙트 프리팹이 로드되지 않음 (SkillID: {skillID})");
        }
    }

    /// <summary>
    /// 직접 프리팹을 전달하여 이펙트 생성 (하위 호환용)
    /// </summary>
    protected void SpawnEffect(GameObject effectPrefab, Vector3 position, float lifetime = 3f)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, lifetime);
        }
    }

    /// <summary>
    /// 범위 내 모든 적에게 데미지 적용
    /// </summary>
    /// <returns>피격된 적 수</returns>
    protected int DamageEnemiesInRange(Vector3 center, float range)
    {
        if (!MonsterProviderRegistry.HasProvider) return 0;

        var monsters = MonsterProviderRegistry.GetActiveMonsters();
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

    /// <summary>
    /// 범위 데미지 + 상태이상을 한 번에 적용
    /// </summary>
    /// <returns>피격된 적 수</returns>
    protected int DamageAndApplyEffectInRange(Vector3 center, float range)
    {
        int hitCount = DamageEnemiesInRange(center, range);
        ApplyStatusEffectInRange(center, range);
        return hitCount;
    }



}
