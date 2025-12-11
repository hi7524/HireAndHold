using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using Assets.HeroEditor.Common.Scripts.CharacterScripts;

/// <summary>
// 플레이어 유닛을 관리하는 클래스
// 유닛의 스탯, 비주얼, 공격, 스킬 시스템 담당
/// </summary>
public class Unit : MonoBehaviour
{
    private const float PercentToDivider = 100f;

    [SerializeField] private Transform visualRoot;
    [SerializeField] private string projectileKey = "Projectile";
    [SerializeField] private AttackTriggerZone attackTriggerZone;

    public int UnitID { get; private set; } = 11101;
    public Enemy AttackTarget { get; private set; }
    public ObjectPoolManager GetPoolManager() => poolManager;
    public Animator GetAnimator() => visualAnimator;

    // 유닛 데이터
    private UnitData unitData;
    private Stat attackDamage;
    private Stat attackCooltime;
    private Stat criticalRate;
    private Stat criticalDamage;

    // 유닛 프리팹
    private AsyncOperationHandle<GameObject> visualHandle;
    private CancellationTokenSource cts;

    // 외부 참조 (Initialize에서 설정)
    private static PassiveSkillManager cachedPassiveSkillManager;
    private static ObjectPoolManager cachedPoolManager;
    private ObjectPoolManager poolManager;

    private float lastAttackTime;
    private GameObject visualObject;
    private Animator visualAnimator;
    private SortingGroup sortingGroup;
    private AnimationEvents animationEvents;

    private readonly List<UnitSkill> skills = new(); // 성급 업그레이드에 따라 추가될 자동 시전 스킬


    private void Awake()
    {
        cts = new CancellationTokenSource();
    }

    private void Start()
    {
        // 캐시된 참조 사용
        poolManager = cachedPoolManager;
    }

    /// <summary>
    /// 씬 시작 시 한 번만 호출하여 공통 참조를 캐싱
    /// </summary>
    public static void SetSceneReferences(ObjectPoolManager poolMgr, PassiveSkillManager passiveMgr)
    {
        cachedPoolManager = poolMgr;
        cachedPassiveSkillManager = passiveMgr;
    }

    public static void ClearSceneReferences()
    {
        cachedPoolManager = null;
        cachedPassiveSkillManager = null;
    }

    private void OnEnable()
    {
        lastAttackTime = Time.time;
    }

    private void Update()
    {
        AttackTarget = FindNearestTarget();

        if (AttackTarget != null && Time.time >= lastAttackTime + unitData.ATTACK_COOLTIME)
        {
            lastAttackTime = Time.time;
            Attack(AttackTarget);
        }

        HandleAutoSkills();
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();

        if (visualHandle.IsValid())
        {
            Addressables.Release(visualHandle);
        }
    }

    // 유닛 ID 설정 및 데이터 로드
    public void SetUnitID(int ID)
    {
        UnitID = ID;
        unitData = DataTableManager.UnitTable.Get(ID);

        Debug.Log($"Unit ID changed to: {ID}");
        SetStats();
        SetSkills();
        SetVisualPrefab();
    }

    // 유닛 ID 업데이트 (합성 시 Stat 모디파이어 보존)
    public void UpdateUnitID(int ID)
    {
        UnitID = ID;
        unitData = DataTableManager.UnitTable.Get(ID);

        Debug.Log($"Unit ID updated (preserving modifiers) to: {ID}");
        UpdateStatBaseValues();
        SetSkills();
        SetVisualPrefab();
    }

    // Stat 기본값만 업데이트 (모디파이어 보존)
    private void UpdateStatBaseValues()
    {
        if (unitData == null) return;

        if (attackDamage != null)
            attackDamage.SetBaseValue(unitData.ATTACK);
        else
            attackDamage = new Stat(unitData.ATTACK);

        if (attackCooltime != null)
            attackCooltime.SetBaseValue(unitData.ATTACK_COOLTIME);
        else
            attackCooltime = new Stat(unitData.ATTACK_COOLTIME);

        if (criticalRate != null)
            criticalRate.SetBaseValue(unitData.ATTACK_CRITICAL);
        else
            criticalRate = new Stat(unitData.ATTACK_CRITICAL);

        if (criticalDamage != null)
            criticalDamage.SetBaseValue(unitData.CRITICAL_DAMAGE);
        else
            criticalDamage = new Stat(unitData.CRITICAL_DAMAGE);
    }

    // 데이터 테이블에서 로드한 값으로 유닛 스탯 초기화
    private void SetStats()
    {
        if (unitData != null)
        {
            attackDamage = new Stat(unitData.ATTACK);
            attackCooltime = new Stat(unitData.ATTACK_COOLTIME);
            criticalRate = new Stat(unitData.ATTACK_CRITICAL);
            criticalDamage = new Stat(unitData.CRITICAL_DAMAGE);

            //  저장된 영구 강화 불러오기 예시
            // PlayerUpgradeData upgradeData = SaveManager.Instance.LoadUpgradeData();
            // Attack.SetUpgradeValue(upgradeData.AttackUpgrade);
            // Defense.SetUpgradeValue(upgradeData.DefenseUpgrade);
            // MaxHealth.SetUpgradeValue(upgradeData.HealthUpgrade);

            ApplyEnforceBonus();
            //강화 데이터 유닛에 저장(junseo)
        }
    }

    //강화 데이터 유닛에 저장
    private void ApplyEnforceBonus()
    {
        string id = UnitID.ToString();
        var character = DatabaseManager.Instance.GetCharacter(id);

        if (character == null)
        {
            Debug.LogError($"[Enforce] 캐릭터 데이터 없음: {id}");
            return;
        }

        int enforceLv = character.enforceLevel;
        if (enforceLv <= 0)
        {
            Debug.Log($"[Enforce] 강화레벨 0 → 적용 없음 (ID:{id})");
            return;
        }

        float totalAtkUp = 0f;
        int rank = unitData.RANK;

        foreach (var kv in NormalEnforceSystem.SharedTable.All)
        {
            var data = kv.Value;
            if (data.Class == rank && data.Normal_Enforce_LV <= enforceLv)
            {
                totalAtkUp += data.AttackUp;
            }
        }

        attackDamage.AddModifier(new StatModifier(totalAtkUp, ModifierType.Flat));

        Debug.Log($"[Enforce 적용됨] Unit:{UnitID}, 강화Lv:{enforceLv}, " + $"추가Atk:{totalAtkUp}, 최종Atk:{attackDamage.Value}");
    }

    // 유닛 데이터에서 스킬 로드 및 추가
    private void SetSkills()
    {
        if (unitData == null)
            return;

        // 기존 스킬 전체 제거
        ClearAllSkills();

        // UNIT_SKILL1 추가
        if (unitData.UNIT_SKILL1 > 0)
            AddSkill(unitData.UNIT_SKILL1);

        // UNIT_SKILL2 추가
        if (unitData.UNIT_SKILL2 > 0)
            AddSkill(unitData.UNIT_SKILL2);
    }

    // 유닛 비주얼 프리팹 동기 로드 및 인스턴스화
    public void SetVisualPrefab()
    {
        if (unitData == null)
        {
            Debug.LogError($"unitData is null! UnitID: {UnitID}");
            return;
        }

        if (string.IsNullOrEmpty(unitData.PREFAB_NAME))
        {
            Debug.LogError($"PREFAB_NAME is null or empty! UnitID: {UnitID}");
            return;
        }

        GameObject visualPrefab = null;

        // 캐시에서 먼저 시도
        if (AddressablePreloader.Instance != null && AddressablePreloader.Instance.HasCachedPrefab(unitData.PREFAB_NAME))
        {
            visualPrefab = AddressablePreloader.Instance.GetCachedPrefab(unitData.PREFAB_NAME);
        }
        else
        {
            // 캐시에 없으면 동기 로드 (fallback)
            visualHandle = Addressables.LoadAssetAsync<GameObject>(unitData.PREFAB_NAME);
            visualPrefab = visualHandle.WaitForCompletion();

            if (visualHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Failed to load prefab: {unitData.PREFAB_NAME}");
                return;
            }
        }

        if (visualPrefab == null)
        {
            Debug.LogError($"Visual prefab is null: {unitData.PREFAB_NAME}");
            return;
        }

        ClearVisualChildren();
        visualObject = Instantiate(visualPrefab, visualRoot);

        // visualObject의 자식 오브젝트에서 Animator, AnimationEvents 캐싱
        if (visualObject != null)
        {
            visualAnimator = visualObject.GetComponentInChildren<Animator>();
            sortingGroup = visualObject.GetComponentInChildren<SortingGroup>();
            animationEvents = visualObject.GetComponentInChildren<AnimationEvents>();

            // AnimationEvents 이벤트 구독
            if (animationEvents != null)
            {
                animationEvents.OnCustomEvent += OnAnimationEvent;
            }

            if (sortingGroup == null)
            {
                Debug.LogError("소팅그룹안됨");
            }
        }
    }

    // 소팅 그룹의 소팅 오더 변경
    public void SetSortingOrder(int order)
    {
        sortingGroup.sortingOrder = order;
    }

    // 캐싱된 비주얼 오브젝트 제거
    private void ClearVisualChildren()
    {
        // AnimationEvents 이벤트 구독 해제
        if (animationEvents != null)
        {
            animationEvents.OnCustomEvent -= OnAnimationEvent;
            animationEvents = null;
        }

        if (visualObject != null)
        {
            Destroy(visualObject);
            visualObject = null;
            visualAnimator = null;
        }
    }

    // 애니메이션 이벤트 콜백
    private void OnAnimationEvent(string eventName)
    {
        switch (eventName)
        {
            case "ReleaseArrow":
            case "Hit":
                FireProjectile();
                break;
        }
    }

    // ObjectPoolManager 설정
    public void SetPool(ObjectPoolManager poolManager)
    {
        this.poolManager = poolManager;
    }

    // AttackTriggerZone 설정
    public void SetAttackZone(AttackTriggerZone zone)
    {
        this.attackTriggerZone = zone;
    }

    // AttackTriggerZone 내에서 가장 가까운 살아있는 적 탐색
    public Enemy FindNearestTarget()
    {
        if (attackTriggerZone == null) return null;

        var enemies = attackTriggerZone.GetEnemiesInZone();
        if (enemies.Count == 0) return null;

        Enemy nearest = null;
        float minDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    // 대상 적에게 투사체를 발사
    private void Attack(Enemy target)
    {
        if (poolManager == null || target == null) return;

        // 공격 대상을 임시 저장 (애니메이션 이벤트에서 사용)
        AttackTarget = target;

        // 공격 애니메이션 재생
        if (visualAnimator != null)
        {
            if (unitData.PROJECTILE_TYPE == ProjectileType.Bow)
                visualAnimator.SetTrigger(AnimParams.SimpleBowShot);
            else
                visualAnimator.SetTrigger(AnimParams.Slash);
        }
        else
        {
            // Animator가 없으면 즉시 발사
            FireProjectile();
        }
    }

    // 실제 발사체 발사 (애니메이션 이벤트에서 호출)
    public void FireProjectile()
    {
        if (poolManager == null || AttackTarget == null) return;

        GameObject projectileObj = poolManager.Get(unitData.PROJECTILE);
        if (projectileObj == null)
        {
            projectileObj = poolManager.Get("TestProjectile"); // 테스트용
        }

        projectileObj.transform.position = transform.position;

        PlayerUnitProjectile projectile = projectileObj.GetComponent<PlayerUnitProjectile>();
        if (projectile == null) return;

        projectile.Initialize(poolManager, projectileKey);

        float damage = CalculateDamage(AttackTarget);

        projectile.SetDamage(damage);
        projectile.SetTarget(AttackTarget.transform);
        projectile.Launch();
    }

    // 대상에게 가할 최종 데미지를 계산
    private float CalculateDamage(Enemy target)
    {
        float damage = attackDamage.Value;

        // 보스 추가 데미지 (PassiveSkillManager에서 직접 가져옴 - 보스만 해당이라 Stat 시스템 밖에서 처리)
        if (target.IsBoss && cachedPassiveSkillManager != null)
        {
            PassiveSkillEffects effects = cachedPassiveSkillManager.GetCurrentEffects();
            damage *= 1f + effects.bossDamageBonus / PercentToDivider;
        }

        // 치명타 판정 - 이제 criticalRate.Value에 패시브 보너스가 포함됨
        bool isCritical = Random.value < (criticalRate.Value / PercentToDivider);
        if (isCritical)
        {
            damage *= criticalDamage.Value; // 패시브 보너스 포함
        }

        return damage;
    }

    // 스킬용 데미지 계산 (치명타율/치명타 데미지 커스터마이징 가능)
    public float CalculateDamage(Enemy target, float baseDamage, float critRate, float critDamage)
    {
        float damage = baseDamage;

        // 보스 추가 데미지 적용
        if (target.IsBoss && cachedPassiveSkillManager != null)
        {
            PassiveSkillEffects effects = cachedPassiveSkillManager.GetCurrentEffects();
            damage *= 1f + effects.bossDamageBonus / PercentToDivider;
        }

        // 치명타 판정 및 적용
        bool isCritical = Random.value < (critRate / PercentToDivider);
        if (isCritical)
        {
            damage *= critDamage;
        }

        return damage;
    }

    // AttackTriggerZone 시각화 (필요시)
    // private void OnDrawGizmosSelected()
    // {
    //     // AttackTriggerZone이 BoxCollider2D를 사용하므로 별도 시각화 불필요
    // }

    // 자동 시전 스킬, 성급 업그레이드에 따라 추가될 스킬 목록 시전
    private void HandleAutoSkills()
    {
        foreach (var skill in skills)
        {
            skill.TryExecute();
        }
    }

    // 스킬 ID를 통해 유닛에 스킬 추가
    public void AddSkill(int skillId)
    {
        // DataTable에서 스킬 데이터 가져오기
        var skillData = DataTableManager.SkillTable.Get(skillId);
        if (skillData == null)
        {
            return;
        }

        var skill = new UnitSkill(this, skillData);
        skills.Add(skill);
    }

    // 특정 스킬을 제거
    public void RemoveSkill(int skillId)
    {
        var skillToRemove = skills.Find(s => s.SkillID == skillId);

        if (skillToRemove != null)
        {
            skills.Remove(skillToRemove);
        }
    }

    // 모든 스킬 제거
    public void ClearAllSkills()
    {
        skills.Clear();
    }

    // 현재 보유한 스킬 개수
    public int GetSkillCount()
    {
        return skills.Count;
    }

    // 특정 스킬이 있는지 확인
    public bool HasSkill(int skillId)
    {
        return skills.Exists(s => s.SkillID == skillId);
    }

    // 공격력 Stat 반환
    public Stat GetAttackDamageStat()
    {
        return attackDamage;
    }

    public Stat GetCriticalRateStat()
    {
        return criticalRate;
    }

    // 치명타 데미지 Stat 반환
    public Stat GetCriticalDamageStat()
    {
        return criticalDamage;
    }

    // 공격 쿨타임 Stat 반환
    public Stat GetAttackCooltimeStat()
    {
        return attackCooltime;
    }
}
