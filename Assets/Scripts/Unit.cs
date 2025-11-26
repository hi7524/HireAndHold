using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private float attackInterval = 1.0f;
    [SerializeField] private float attackDamage = 5;
    [SerializeField] private string projectileKey = "Projectile";

    private ObjectPoolManager poolManager;
    private Monster attackTarget;
    private float lastAttackTime;

    private readonly List<UnitSkill> skills = new(); // 성급 업그레이드에 따라 추가될 자동 시전 스킬


    private void Start()
    {
        poolManager = GameObject.FindWithTag(Tags.PoolManager).GetComponent<ObjectPoolManager>();
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

    public void SetPool(ObjectPoolManager poolManager)
    {
        this.poolManager = poolManager;
    }

    // 사거리에 따라 적 감지
    private Monster FindNearestTarget()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, attackRange);

        Monster nearest = null;
        float minDis = attackRange;

        foreach (var coll in colliders)
        {
            Monster monster = coll.GetComponent<Monster>();

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
    private void Attack(Monster target)
    {
        // Pool 반환용 연결
        GameObject projectileObj = poolManager.Get(projectileKey);
        projectileObj.transform.position = transform.position;
        projectileObj.transform.rotation = Quaternion.identity;

        UnitProjectile projectile = projectileObj.GetComponent<UnitProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(poolManager, projectileKey);
            projectile.SetDamage(attackDamage);
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
            Debug.LogError($"Skill ID: {skillId}를 찾을 수 없습니다");
            return;
        }

        var skill = new UnitSkill(this, skillData);
        skills.Add(skill);

        Debug.Log($"Skill: {skillId} ({skillData.SKILL_NAME}) 추가");
    }

    // 특정 스킬을 제거
    public void RemoveSkill(int skillId)
    {
        var skillToRemove = skills.Find(s => s.SkillID == skillId);

        if (skillToRemove != null)
        {
            skills.Remove(skillToRemove);
            Debug.Log($"Skill: {skillId} removed");
        }
    }

    // 모든 스킬 제거
    public void ClearAllSkills()
    {
        skills.Clear();
        Debug.Log("All skills cleared");
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