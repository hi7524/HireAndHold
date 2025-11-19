using UnityEngine;

public class SkillEffectApplier : MonoBehaviour
{


    public void ApplyStatusEffectToTarget(GameObject target, StatusEffectType type, float duration, float value)
    {
        var targetEffectManager = target.GetComponent<StatusEffectManager>();

        if (targetEffectManager == null)
        {
            Debug.LogWarning($"{target.name}에 StatusEffectManager가 없습니다!");
            return;
        }

        StatusEffect effect = CreateStatusEffect(type, duration, value);

        if (effect != null)
        {
            targetEffectManager.AddStatusEffect(type, effect);
        }
    }


    public void ApplyStatusEffectInRange(Vector3 center, float range, StatusEffectType type, float duration, float value)
    {
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        int affectedCount = 0;

        // range가 Infinity 또는 음수면 전체 공격
        bool isGlobalEffect = float.IsInfinity(range) || range < 0f;

        foreach (GameObject monster in monsters)
        {
            if (isGlobalEffect)
            {
                // 전역 효과: 거리 체크 없이 모든 몬스터에게 적용
                ApplyStatusEffectToTarget(monster, type, duration, value);
                affectedCount++;
            }
            else
            {
                // 범위 효과: 거리 체크 후 적용
                float distance = Vector3.Distance(center, monster.transform.position);

                if (distance <= range)
                {
                    ApplyStatusEffectToTarget(monster, type, duration, value);
                    affectedCount++;
                }
            }
        }

        Debug.Log($"{affectedCount}마리의 몬스터에게 {type} 적용!");
    }


    public void ApplyStatusEffectToNearest(Vector3 position, StatusEffectType type, float duration, float value)
    {
        GameObject nearest = FindNearestMonster(position);

        if (nearest != null)
        {
            ApplyStatusEffectToTarget(nearest, type, duration, value);
        }
        else
        {
            Debug.Log("범위 내에 몬스터가 없습니다!");
        }
    }


    private StatusEffect CreateStatusEffect(StatusEffectType type, float duration, float value)
    {
        switch (type)
        {
            case StatusEffectType.Stun:
                return new StatusEffectStun(0f, duration);
            case StatusEffectType.Slow:
                return new StatusEffectSlow(value, duration);
            case StatusEffectType.Pull:
                return new StatusEffectPull(value, duration);
            case StatusEffectType.DamageOverTime:
                return new StatusEffectDamageOverTime(value, duration);
            case StatusEffectType.DefenseDown:
                return new StatusEffectDefenseDown(value, duration);
            case StatusEffectType.Root:
                return new StatusEffectRoot(value, duration);
            case StatusEffectType.Knockback:
                return new StatusEffectKnockback(value, duration);
            default:
                Debug.LogWarning($"알 수 없는 StatusEffectType: {type}");
                return null;
        }
    }


    private GameObject FindNearestMonster(Vector3 position)
    {
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        GameObject nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject monster in monsters)
        {
            float distance = Vector3.Distance(position, monster.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = monster;
            }
        }

        return nearest;
    }


}