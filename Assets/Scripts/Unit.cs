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

    // 영웅 강화 공격력 배율 (기본 1.0)
    private float heroAttackMultiplier = 1f;

    // 영웅 강화 스킬 강화 효과 저장용
    private float heroSkillDamageMultiplier = 1f;
    private int heroProjectileBonus = 0;
    private float heroSkillDurationBonus = 0f;
    private float heroSkillCooltimeBonus = 1f;

    //영구 캐릭터 아이디 (합성 기본 구분)
    public int BaseCharacterID { get; private set; }


    public void AddHeroAttackMultiplier(float mul)
    {
        heroAttackMultiplier *= mul;
    }

    public void AddHeroSkillDamageMultiplier(float mul)
    {
        heroSkillDamageMultiplier *= mul;
    }

    public void AddHeroSkillDuration(float value)
    {
        heroSkillDurationBonus += value;
    }

    public void AddHeroSkillCooltimeMultiplier(float mul)
    {
        heroSkillCooltimeBonus *= mul;
    }

    public void AddHeroProjectileBonus(int value)
    {
        heroProjectileBonus += value;
    }


    private readonly List<UnitSkill> skills = new(); // 성급 업그레이드에 따라 추가될 자동 시전 스킬
    public UnitData GetUnitData() => unitData;

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

        if (AttackTarget != null && Time.time >= lastAttackTime + attackCooltime.Value)
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

        BaseCharacterID = ID;

        unitData = DataTableManager.UnitTable.Get(ID);

        Debug.Log($"Unit ID changed to: {ID}");
        SetStats();
        SetSkills();
        ApplyEnforceBonus();
        SetVisualPrefab();
    }

    // 유닛 ID 업데이트 (합성 시 Stat 모디파이어 보존)
    public void UpdateUnitID(int ID)
    {
        UnitID = ID;
        unitData = DataTableManager.UnitTable.Get(ID);

        UpdateStatBaseValues();

        SetSkills();

        ApplyEnforceBonus();

        SetVisualPrefab();

        Debug.Log($"<color=magenta>[HeroEnforce After Merge]</color> atkMul:{heroAttackMultiplier}, dmgMul:{heroSkillDamageMultiplier}, proj:{heroProjectileBonus}, cool:{heroSkillCooltimeBonus}, dur:{heroSkillDurationBonus}");
    }


    // Stat 기본값만 업데이트 (모디파이어 보존)
    private void UpdateStatBaseValues()
    {
        if (unitData == null)
        {
            return;
        }

        if (attackDamage != null)
        {
            attackDamage.SetBaseValue(unitData.ATTACK);
        }
        else
        {
            attackDamage = new Stat(unitData.ATTACK);
        }

        if (attackCooltime != null)
        {
            attackCooltime.SetBaseValue(unitData.ATTACK_COOLTIME);
        }
        else
        {
            attackCooltime = new Stat(unitData.ATTACK_COOLTIME);
        }

        if (criticalRate != null)
        {
            criticalRate.SetBaseValue(unitData.ATTACK_CRITICAL);
        }
        else
        {
            criticalRate = new Stat(unitData.ATTACK_CRITICAL);
        }

        if (criticalDamage != null)
        {
            criticalDamage.SetBaseValue(unitData.CRITICAL_DAMAGE);
        }
        else
        {
            criticalDamage = new Stat(unitData.CRITICAL_DAMAGE);
        }
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

            //ApplyEnforceBonus();
            //강화 데이터 유닛에 저장(junseo)
        }
    }

    // 강화 데이터 유닛에 저장 (모든 레벨 효과 누적)
    private void ApplyEnforceBonus()
    {
        // DatabaseManager가 없으면 스킵 (TestScene 등)
        if (DatabaseManager.Instance == null)
        {
            Debug.Log($"[Enforce] DatabaseManager가 없어 강화 적용 스킵 (ID:{UnitID})");
            return;
        }

        //string id = UnitID.ToString();
        string id = BaseCharacterID.ToString();
        var character = DatabaseManager.Instance.GetCharacter(id);

        if (character == null)
        {
            Debug.LogWarning($"[Enforce] 캐릭터 데이터 없음: {id}");
            return;
        }

        // NORMAL 강화 효과 적용
        int enforceLv = character.enforceLevel;
        if (enforceLv > 0)
        {
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
            Debug.Log($"[Normal Enforce 적용] Unit:{UnitID}, Lv:{enforceLv}, +Atk:{totalAtkUp}");
        }

        // HERO 강화 효과 적용 (⭐ 모든 레벨 누적)
        int heroLv = character.heroEnforceLevel;
        if (heroLv <= 0)
        {
            Debug.Log("[HeroEnforce] Lv 0 → Skip");
            return;
        }

        // 현재 유닛의 스킬
        int skill1 = unitData.UNIT_SKILL1;
        int skill2 = unitData.UNIT_SKILL2;

        // ⭐ 1레벨부터 현재 레벨까지 모든 효과 누적 적용
        for (int lv = 1; lv <= heroLv; lv++)
        {
            var heroData = DataTableManager.heroEnforceTable.Get(BaseCharacterID, lv);
            if (heroData == null)
            {
                Debug.LogWarning($"[HeroEnforce] heroData NULL (BaseID:{BaseCharacterID}, Lv:{lv})");
                continue;
            }

            var effect = DataTableManager.heroEnforceEffectTable.Get(heroData.Hero_Enforce_EffectID);
            if (effect == null)
            {
                Debug.LogWarning($"[HeroEnforce] effectData NULL (EffectID:{heroData.Hero_Enforce_EffectID})");
                continue;
            }

            // 스킬 매칭 검사
            bool skillMatched = false;

            // Attack_Up만 있는 경우 (스킬 무관 강화)
            if (effect.Attack_Up > 1f &&
                effect.SkillID1.GetValueOrDefault(0) == 0 &&
                effect.SkillID2.GetValueOrDefault(0) == 0)
            {
                skillMatched = true;
            }
            // 스킬 조건 없음
            else if (effect.SkillID1.GetValueOrDefault(0) == 0 &&
                     effect.SkillID2.GetValueOrDefault(0) == 0)
            {
                skillMatched = true;
            }
            // 스킬 매칭
            else
            {
                int effectSkill1 = effect.SkillID1.GetValueOrDefault(0);
                int effectSkill2 = effect.SkillID2.GetValueOrDefault(0);

                if ((effectSkill1 > 0 && (effectSkill1 == skill1 || effectSkill1 == skill2)) ||
                    (effectSkill2 > 0 && (effectSkill2 == skill1 || effectSkill2 == skill2)))
                {
                    skillMatched = true;
                }
            }

            if (!skillMatched)
            {
                Debug.Log($"[HeroEnforce Lv{lv}] 스킬 불일치 → Skip (Unit:{UnitID}, SK1:{skill1}, SK2:{skill2})");
                continue;
            }

            // 효과 누적 적용
            if (effect.Attack_Up > 1f)
            {
                heroAttackMultiplier *= effect.Attack_Up;
                Debug.Log($"[HeroEnforce Lv{lv}] AttackUp ×{effect.Attack_Up}");
            }

            if (effect.Skill_Damage_Up > 1f)
            {
                heroSkillDamageMultiplier *= effect.Skill_Damage_Up;
                Debug.Log($"[HeroEnforce Lv{lv}] SkillDamageUp ×{effect.Skill_Damage_Up}");
            }

            if (effect.Duration_Up > 0f)
            {
                heroSkillDurationBonus += effect.Duration_Up;
                Debug.Log($"[HeroEnforce Lv{lv}] Duration +{effect.Duration_Up}");
            }

            if (effect.Projectile > 0)
            {
                heroProjectileBonus += effect.Projectile;
                Debug.Log($"[HeroEnforce Lv{lv}] Projectile +{effect.Projectile}");
            }

            if (effect.CoolTime_Down > 0f && effect.CoolTime_Down < 1f)
            {
                heroSkillCooltimeBonus *= effect.CoolTime_Down;
                Debug.Log($"[HeroEnforce Lv{lv}] CoolTime ×{effect.CoolTime_Down}");
            }
        }

        Debug.Log($"[HeroEnforce Final] atkMul={heroAttackMultiplier}, dmgMul={heroSkillDamageMultiplier}, proj={heroProjectileBonus}, cool={heroSkillCooltimeBonus}");
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

        // 치명타 판정
        float damage = attackDamage.Value;
        bool isCritical = Random.value < (criticalRate.Value / PercentToDivider);

        if (isCritical)
        {
            damage *= criticalDamage.Value;
        }

        // 보스 추가 데미지
        if (AttackTarget.IsBoss && cachedPassiveSkillManager != null)
        {
            PassiveSkillEffects effects = cachedPassiveSkillManager.GetCurrentEffects();
            damage *= 1f + effects.bossDamageBonus / PercentToDivider;
        }

        damage *= heroAttackMultiplier;
        float finalDamage = Mathf.Round(damage * 10f) / 10f;

        projectile.SetDamage(finalDamage, isCritical);
        projectile.SetTarget(AttackTarget.transform);
        projectile.SetHitAudioClip(unitData.HIT_AUDIO_CLIP);
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

        damage *= heroAttackMultiplier;

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

    public float GetHeroSkillDamageMultiplier() => heroSkillDamageMultiplier;
    public int GetHeroProjectileBonus() => heroProjectileBonus;
    public float GetHeroSkillCooltimeMultiplier() => heroSkillCooltimeBonus;
    public float GetHeroSkillDurationBonus() => heroSkillDurationBonus;



}
