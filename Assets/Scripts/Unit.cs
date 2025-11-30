using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Unit : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private string projectileKey = "Projectile";

    public int UnitID { get; private set; } = 11101;

    // 유닛 데이터
    private UnitData unitData;
    private Stat attackDamage;
    private Stat criticalRate;
    private Stat criticalDamage;
    private float attackRange;
    private float attackInterval;
    private int unitSkill1ID;
    private int unitSkill2ID;
    private string unitIconID;

    // 유닛 프리팹
    private AsyncOperationHandle<GameObject> visualHandle;
    private CancellationTokenSource cts;

    // 외부 참조
    private PassiveSkillManager passiveSkillManager;
    private ObjectPoolManager poolManager;

    private Enemy attackTarget;
    private float lastAttackTime;

    private readonly List<UnitSkill> skills = new(); // 성급 업그레이드에 따라 추가될 자동 시전 스킬


    private void Awake()
    {
        cts = new CancellationTokenSource();
    }

    public async Task SetUnitID(int ID)
    {
        UnitID = ID;
        unitData = DataTableManager.UnitTable.Get(ID);

        Debug.Log($"Unit ID changed to: {ID}");
        SetStats();
        await SetVisualPrefabAsync();
    }

    private void SetStats()
    {
        if (unitData != null)
        {
            // 데이터 테이블에서 로드한 값으로 Stat 초기화
            attackDamage = new Stat(unitData.ATTACK);
            criticalRate = new Stat(unitData.ATTACK_CRITICAL);
            criticalDamage = new Stat(unitData.CRITICAL_DAMAGE);
            attackRange = unitData.ATTACK_RANGE;
            attackInterval = unitData.ATTACK_COOLTIME;
            unitSkill1ID = unitData.UNIT_SKILL1;
            unitSkill2ID = unitData.UNIT_SKILL2;
            unitIconID = unitData.UNIT_ICON;

            //  저장된 영구 강화 불러오기 예시
            // PlayerUpgradeData upgradeData = SaveManager.Instance.LoadUpgradeData();
            // Attack.SetUpgradeValue(upgradeData.AttackUpgrade);
            // Defense.SetUpgradeValue(upgradeData.DefenseUpgrade);
            // MaxHealth.SetUpgradeValue(upgradeData.HealthUpgrade);
        }
    }

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

        // 기존 자식 오브젝트 제거
        if (visualRoot.childCount > 0)
        {
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(visualRoot.GetChild(i).gameObject);
            }
        }

        var visualObj = Instantiate(visualPrefab, visualRoot);

    }

    private void Start()
    {
        poolManager = GameObject.FindWithTag(Tags.PoolManager).GetComponent<ObjectPoolManager>();
        passiveSkillManager = FindFirstObjectByType<PassiveSkillManager>();
    }

    private void OnEnable()
    {
        lastAttackTime = Time.time;
    }

    private void Update()
    {
        attackTarget = FindNearestTarget();

        if (attackTarget != null && Time.time >= lastAttackTime + attackInterval)
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

    public void SetPool(ObjectPoolManager poolManager)
    {
        this.poolManager = poolManager;
    }

    // 사거리에 따라 적 감지
    private Enemy FindNearestTarget()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, attackRange);

        Enemy nearest = null;
        float minDis = attackRange;

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

    // 타겟 공격
    private void Attack(Enemy target)
    {
        // Pool 반환용 연결
        GameObject projectileObj = poolManager.Get(projectileKey);
        projectileObj.transform.position = transform.position;
        projectileObj.transform.rotation = Quaternion.identity;

        PlayerUnitProjectile projectile = projectileObj.GetComponent<PlayerUnitProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(poolManager, projectileKey);

            float damage = attackDamage.Value;

            // 보스 추가 데미지 (IsBoss 프로퍼티 사용)
            if (target.IsBoss && passiveSkillManager != null)
            {
                PassiveSkillEffects effects = passiveSkillManager.GetCurrentEffects();
                damage *= (1f + effects.bossDamageBonus / 100f);
            }

            // 치명타 판정
            bool isCritical = Random.value < (criticalRate.Value / 100f);
            if (isCritical)
            {
                damage *= criticalRate.Value;
            }

            projectile.SetDamage(damage);
            projectile.SetTarget(target.transform);
            projectile.Launch();
        }
    }

    // 사거리 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
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