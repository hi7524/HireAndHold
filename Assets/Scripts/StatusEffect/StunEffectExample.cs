using UnityEngine;

/// <summary>
/// 스턴 효과를 테스트하거나 적용하는 예시 클래스
/// </summary>
public class StunEffectExample : MonoBehaviour
{
    [Header("스턴 설정")]
    [SerializeField] private float stunDuration = 2.0f; // 스턴 지속 시간
    [SerializeField] private KeyCode testKey = KeyCode.S; // 테스트용 키

    /// <summary>
    /// 테스트용 - S키를 누르면 주변 몬스터들에게 스턴 적용
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            ApplyStunToNearbyMonsters();
        }
    }

    /// <summary>
    /// 주변 몬스터들에게 스턴 효과 적용
    /// </summary>
    private void ApplyStunToNearbyMonsters()
    {
        // 주변의 모든 몬스터 찾기
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        
        foreach (GameObject monster in monsters)
        {
            ApplyStunEffect(monster);
        }
        
        Debug.Log($"[StunEffectExample] {monsters.Length}마리의 몬스터에게 스턴 효과 적용!");
    }

    /// <summary>
    /// 특정 몬스터에게 스턴 효과 적용
    /// </summary>
    /// <param name="target">스턴을 적용할 대상</param>
    public void ApplyStunEffect(GameObject target)
    {
        if (target == null) return;

        // StatusEffectManager 컴포넌트 가져오기 (없으면 추가)
        StatusEffectManager effectManager = target.GetComponent<StatusEffectManager>();
        if (effectManager == null)
        {
            effectManager = target.AddComponent<StatusEffectManager>();
        }

        // 스턴 효과 생성 및 적용
        StatusEffectStun stunEffect = new StatusEffectStun(0f, stunDuration, 0.1f);
        effectManager.AddStatusEffect(StatusEffectType.Stun, stunEffect);
    }

    /// <summary>
    /// 스킬이나 무기에서 호출할 수 있는 public 메서드
    /// </summary>
    /// <param name="target">스턴을 적용할 대상</param>
    /// <param name="duration">스턴 지속 시간</param>
    public static void ApplyStun(GameObject target, float duration = 2.0f)
    {
        if (target == null) return;

        // Monster 컴포넌트가 있는지 확인
        Monster monster = target.GetComponent<Monster>();
        if (monster == null) return;

        // StatusEffectManager 컴포넌트 가져오기 (없으면 추가)
        StatusEffectManager effectManager = target.GetComponent<StatusEffectManager>();
        if (effectManager == null)
        {
            effectManager = target.AddComponent<StatusEffectManager>();
        }

        // 스턴 효과 생성 및 적용
        StatusEffectStun stunEffect = new StatusEffectStun(0f, duration, 0.1f);
        effectManager.AddStatusEffect(StatusEffectType.Stun, stunEffect);

        Debug.Log($"[StunEffect] {target.name}에게 {duration}초 스턴 적용!");
    }
}