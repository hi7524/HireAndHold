using UnityEngine;

public class UnitSkill
{
    protected Unit owner;
    protected SkillData skillData;
    protected float lastUsedTime;

    public int SkillID => skillData.SKILL_ID;
    public string SkillName
    {
        get
        {
            if (int.TryParse(skillData.SKILL_NAME, out int nameId))
                return DataTableManager.StringTable.Get(nameId);
            return skillData.SKILL_NAME;
        }
    }

    public UnitSkill(Unit owner, SkillData skillData)
    {
        this.owner = owner;
        this.skillData = skillData;
        this.lastUsedTime = -skillData.SKILL_COOLTIME; // 즉시 사용 가능하도록

        Debug.Log($"스킬 추가: {SkillName}");
    }

    // 쿨타임이 돌았는지 확인
    public bool CanUse()
    {
        float finalCooltime = skillData.SKILL_COOLTIME * owner.GetHeroSkillCooltimeMultiplier();
        return Time.time >= lastUsedTime + finalCooltime;
    }

    // 쿨타임 확인 및 스킬 사용 시도
    public void TryExecute()
    {
        if (!CanUse())
            return;

        lastUsedTime = Time.time;
        OnExecute();
    }

    // 실제 스킬 실행 로직
    protected virtual void OnExecute()
    {
        Enemy target = FindTarget();
        if (target == null)
            return;

        PlaySkillAnimation();
        PlaySkillSound();
        SpawnProjectiles(target);
    }

    // 스킬 타겟 찾기 (SKILL_TARGET1 기반)
    private Enemy FindTarget()
    {
        if (skillData.SKILL_TARGET1 == 1)
        {
            return owner.AttackTarget;
        }

        return null;
    }

    // 스킬 애니메이션 재생
    private void PlaySkillAnimation()
    {
        var animator = owner.GetAnimator();
        if (animator != null)
        {
            animator.SetTrigger(AnimParams.Cast);
        }
    }

    // 스킬 사운드 재생
    private void PlaySkillSound()
    {
        if (!string.IsNullOrEmpty(skillData.SKILL_SOUND))
        {
            // TODO: 사운드 매니저 연동
        }
    }

    // 다중 투사체 발사
    private void SpawnProjectiles(Enemy target)
    {
        var poolManager = owner.GetPoolManager();
        if (poolManager == null)
            return;

        int baseProjectile = Mathf.Max(1, skillData.SKILL_BOLT);
        int bonusProjectile = owner.GetHeroProjectileBonus();

        int projectileCount = baseProjectile + bonusProjectile;

        Debug.Log($"[SkillProjectile] {SkillName} → Base:{baseProjectile}, Bonus:{bonusProjectile}, Total:{projectileCount}");


        for (int i = 0; i < projectileCount; i++)
        {
            // 투사체 풀에서 가져오기
            GameObject projectileObj = poolManager.Get(skillData.SKILL_EFFECT);

            if (projectileObj == null) // TODO: 임시, 테스트용 기본 프리팹 사용
                projectileObj = poolManager.Get("UnitProjectile_Fire"); 

            if (projectileObj == null)
                return;

            projectileObj.transform.position = owner.transform.position;
            PlayerUnitProjectile projectile = projectileObj.GetComponent<PlayerUnitProjectile>();

            // 투사체 초기화
            projectile.Initialize(poolManager, skillData.SKILL_EFFECT);

            // 데미지 계산 (스킬 치명타 스탯 사용)
            float baseDamage = owner.GetAttackDamageStat().Value;
            float damage = owner.CalculateDamage(target, baseDamage, skillData.SKILL_CRT, skillData.SKILL_CRT_DMG);

            damage *= owner.GetHeroSkillDamageMultiplier();

            projectile.SetDamage(damage);
            projectile.SetTarget(target.transform);
            projectile.Launch();
        }
    }

    // 남은 쿨타임 (초)
    //public float GetRemainingCooldown()
    //{
    //    float remaining = (lastUsedTime + skillData.SKILL_COOLTIME) - Time.time;
    //    return Mathf.Max(0, remaining);
    //}

    //// 쿨타임 진행률 (0~1)
    //public float GetCooldownProgress()
    //{
    //    if (skillData.SKILL_COOLTIME <= 0) 
    //        return 1f;

    //    float elapsed = Time.time - lastUsedTime;
    //    return Mathf.Clamp01(elapsed / skillData.SKILL_COOLTIME);
    //}

    float finalCooltime => skillData.SKILL_COOLTIME * owner.GetHeroSkillCooltimeMultiplier();

    public float GetRemainingCooldown()
    {
        float remaining = (lastUsedTime + finalCooltime) - Time.time;
        return Mathf.Max(0, remaining);
    }

    public float GetCooldownProgress()
    {
        return Mathf.Clamp01((Time.time - lastUsedTime) / finalCooltime);
    }

}
