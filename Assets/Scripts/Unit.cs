using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// 플레이어 유닛을 관리하는 클래스
// 유닛의 스탯, 비주얼, 공격, 스킬 시스템 담당
public class Unit : MonoBehaviour
{
    private const float PercentToDivider = 100f;

    [SerializeField] private Transform visualRoot;
    [SerializeField] private string projectileKey = "Projectile";

    public int UnitID { get; private set; } = 11101;

    // 유닛 데이터
    private UnitData unitData;
    private Stat attackDamage;
    private Stat criticalRate;
    private Stat criticalDamage;

    // 유닛 프리팹
    private AsyncOperationHandle<GameObject> visualHandle;
    private CancellationTokenSource cts;

    // 외부 참조
    private PassiveSkillManager passiveSkillManager;
    private ObjectPoolManager poolManager;

    private Enemy attackTarget;
    private float lastAttackTime;
    private GameObject visualObject;
    private Animator visualAnimator;

    private readonly List<UnitSkill> skills = new(); // 성급 업그레이드에 따라 추가될 자동 시전 스킬


    private void Awake()
    {
        cts = new CancellationTokenSource();
    }

    private void Start()
    {
        var poolManagerObj = GameObject.FindWithTag(Tags.PoolManager);
        if (poolManagerObj != null)
        {
            poolManager = poolManagerObj.GetComponent<ObjectPoolManager>();
        }

        passiveSkillManager = FindFirstObjectByType<PassiveSkillManager>(); // JCH: 프로토타입 이후 수정하기 **
    }

    private void OnEnable()
    {
        lastAttackTime = Time.time;
    }

    private void Update()
    {
        attackTarget = FindNearestTarget();

        if (attackTarget != null && Time.time >= lastAttackTime + unitData.ATTACK_COOLTIME)
        {
            lastAttackTime = Time.time;
            Attack(attackTarget);
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
    public async Task SetUnitID(int ID)
    {
        UnitID = ID;
        unitData = DataTableManager.UnitTable.Get(ID);

        Debug.Log($"Unit ID changed to: {ID}");
        SetStats();
        await SetVisualPrefabAsync();
    }

    // 데이터 테이블에서 로드한 값으로 유닛 스탯 초기화
    private void SetStats()
    {
        if (unitData != null)
        {
            attackDamage = new Stat(unitData.ATTACK);
            criticalRate = new Stat(unitData.ATTACK_CRITICAL);
            criticalDamage = new Stat(unitData.CRITICAL_DAMAGE);

            //  저장된 영구 강화 불러오기 예시
            // PlayerUpgradeData upgradeData = SaveManager.Instance.LoadUpgradeData();
            // Attack.SetUpgradeValue(upgradeData.AttackUpgrade);
            // Defense.SetUpgradeValue(upgradeData.DefenseUpgrade);
            // MaxHealth.SetUpgradeValue(upgradeData.HealthUpgrade);
        }
    }

    // 유닛 비주얼 프리팹 비동기 로드 및 인스턴스화
    public async UniTask SetVisualPrefabAsync()
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

        visualHandle = Addressables.LoadAssetAsync<GameObject>(unitData.PREFAB_NAME);
        var visualPrefab = await visualHandle.ToUniTask(cancellationToken: cts.Token);

        if (visualHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to load prefab: {unitData.PREFAB_NAME}");
            return;
        }

        ClearVisualChildren();
        visualObject = Instantiate(visualPrefab, visualRoot);

        // visualObject의 자식 오브젝트에서 Animator 캐싱
        if (visualObject != null)
        {
            visualAnimator = visualObject.GetComponentInChildren<Animator>();
            if (visualAnimator == null)
            {
                Debug.LogWarning($"Animator not found in visual prefab: {unitData.PREFAB_NAME}");
            }
        }
    }

    // 캐싱된 비주얼 오브젝트 제거
    private void ClearVisualChildren()
    {
        if (visualObject != null)
        {
            Destroy(visualObject);
            visualObject = null;
            visualAnimator = null;
        }
    }

    // ObjectPoolManager 설정
    public void SetPool(ObjectPoolManager poolManager)
    {
        this.poolManager = poolManager;
    }

    // 유닛의 공격 사거리 내에서 가장 가까운 살아있는 적 탐색
    private Enemy FindNearestTarget()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, unitData.ATTACK_RANGE);

        Enemy nearest = null;
        float minDis = unitData.ATTACK_RANGE;

        foreach (var coll in colliders)
        {
            Enemy monster = coll.GetComponent<Enemy>();

            if (monster != null && !monster.IsDead)
            {
                float distance = Vector3.Distance(transform.position, coll.transform.position);
                if (distance < minDis)
                {
                    minDis = distance;
                    nearest = monster;
                }
            }
        }

        return nearest;
    }

    // 대상 적에게 투사체를 발사
    private void Attack(Enemy target)
    {
        if (poolManager == null || target == null) return;

        // 공격 애니메이션 재생
        if (visualAnimator != null)
        {
            visualAnimator.SetTrigger("Slash");
        }

        GameObject projectileObj = poolManager.Get(unitData.PROJECTILE);
        if (projectileObj == null)
        {
            projectileObj = poolManager.Get("TestProjectile"); // 테스트용            
        }

        projectileObj.transform.position = transform.position;
        projectileObj.transform.rotation = Quaternion.identity;

        PlayerUnitProjectile projectile = projectileObj.GetComponent<PlayerUnitProjectile>();
        if (projectile == null) return;

        projectile.Initialize(poolManager, projectileKey);

        float damage = CalculateDamage(target);

        projectile.SetDamage(damage);
        projectile.SetTarget(target.transform);
        projectile.Launch();
    }

    // 대상에게 가할 최종 데미지를 계산
    private float CalculateDamage(Enemy target)
    {
        float damage = attackDamage.Value;

        // 보스 추가 데미지 적용
        if (target.IsBoss && passiveSkillManager != null)
        {
            PassiveSkillEffects effects = passiveSkillManager.GetCurrentEffects();
            damage *= 1f + effects.bossDamageBonus / PercentToDivider;
        }

        // 치명타 판정 및 적용
        bool isCritical = Random.value < (criticalRate.Value / PercentToDivider);
        if (isCritical)
        {
            damage *= criticalDamage.Value;
        }

        return damage;
    }

    // 사거리 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, unitData.ATTACK_RANGE);
    }

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
}