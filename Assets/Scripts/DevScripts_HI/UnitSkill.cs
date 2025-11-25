using UnityEngine;

public class UnitSkill
{
    protected Unit owner;
    protected SkillData skillData;
    protected float lastUsedTime;

    public int SkillID => skillData.SKILL_ID;
    public string SkillName => skillData.SKILL_NAME;

    public UnitSkill(Unit owner, SkillData skillData)
    {
        this.owner = owner;
        this.skillData = skillData;
        this.lastUsedTime = -skillData.SKILL_COOLTIME; // 즉시 사용 가능하도록
    }

    // 쿨타임이 돌았는지 확인
    public bool CanUse()
    {
        return Time.time >= lastUsedTime + skillData.SKILL_COOLTIME;
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
        Debug.Log("스킬 시전");
        SpawnKillPrefab();
    }

    // 스킬 투사체 생성
    protected virtual void SpawnKillPrefab()
    {
        Debug.Log("투사체 발사");
    }

    // 남은 쿨타임 (초)
    public float GetRemainingCooldown()
    {
        float remaining = (lastUsedTime + skillData.SKILL_COOLTIME) - Time.time;
        return Mathf.Max(0, remaining);
    }

    // 쿨타임 진행률 (0~1)
    public float GetCooldownProgress()
    {
        if (skillData.SKILL_COOLTIME <= 0) 
            return 1f;

        float elapsed = Time.time - lastUsedTime;
        return Mathf.Clamp01(elapsed / skillData.SKILL_COOLTIME);
    }
}