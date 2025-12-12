using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StatusEffectManager : MonoBehaviour
{
    private Dictionary<StatusEffectType, StatusEffect> statusEffects = new Dictionary<StatusEffectType, StatusEffect>();
    private Dictionary<StatusEffectType, GameObject> activeVisualEffects = new Dictionary<StatusEffectType, GameObject>();

    private readonly List<StatusEffectType> keysToRemove = new List<StatusEffectType>();

    // 상태 효과 타입별 이펙트 프리팹 키 매핑
    private static readonly Dictionary<StatusEffectType, string> effectKeyMap = new Dictionary<StatusEffectType, string>
    {
        { StatusEffectType.DefenseDown, "effect_state_fear" },
        { StatusEffectType.Root, "effect_state_stuned" },
        { StatusEffectType.Slow, "effect_state_slowDown" },
        { StatusEffectType.Stun, "effect_state_confusion" },
        { StatusEffectType.AttackUp, "effect_state_powerUp" },
        { StatusEffectType.DamageUpPercent, "effect_state_powerUp" }, // 빨간색 계열 (런타임 색상 변경)
    };

    // 빨간색 계열로 색상을 변경해야 하는 효과 타입들
    private static readonly HashSet<StatusEffectType> redColorEffects = new HashSet<StatusEffectType>
    {
        StatusEffectType.DamageUpPercent, // 공격 속도 증가
    };

    // 이펙트 표시 위치 오프셋 (머리 위)
    [SerializeField] private Vector3 effectOffset = new Vector3(0f, 0.5f, 0f);

    // 이펙트 크기 배율
    [SerializeField] private float effectScale = 0.5f;

    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        statusEffects.Clear();
        keysToRemove.Clear();
        ClearAllVisualEffects();

        // Enemy 죽음 이벤트 구독
        if (enemy != null)
        {
            enemy.OnDeath += OnEnemyDeath;
        }
    }

    private void OnEnemyDeath(Enemy e)
    {
        StopAllEffects();
    }

    private void Update()
    {
        if (statusEffects.Count == 0)
            return;

        foreach (var effect in statusEffects.Values)
        {
            if (!effect.IsEndEffect)
            {
                effect.WhileEffect(gameObject);
            }
        }
        
        RemoveEffects();
    }

    public void AddStatusEffect(StatusEffectType type, StatusEffect effect)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type].OnEndEffect(gameObject);
            statusEffects.Remove(type);
            RemoveVisualEffect(type);
        }

        statusEffects.Add(type, effect);
        statusEffects[type].OnStartEffect(gameObject);
        SpawnVisualEffect(type);
    }
    
    private void RemoveEffects()
    {
        keysToRemove.Clear();
        
        foreach (var end in statusEffects)
        {
            if (end.Value.IsEndEffect)
            {
                keysToRemove.Add(end.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            statusEffects[key].OnEndEffect(gameObject);
            statusEffects.Remove(key);
            RemoveVisualEffect(key);
        }
    }

    public void StopAllEffects()
    {
        foreach (var effect in statusEffects.Values)
        {
            effect.ForceStopEffect();
        }
        ClearAllVisualEffects();
    }

    public void RemoveStatusEffect(StatusEffectType type)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type].OnEndEffect(gameObject);
            statusEffects.Remove(type);
            RemoveVisualEffect(type);
        }
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        return statusEffects.ContainsKey(type);
    }

    #region Visual Effect Methods

    private void SpawnVisualEffect(StatusEffectType type)
    {
        // 이미 활성화된 이펙트가 있으면 제거
        RemoveVisualEffect(type);

        // 이펙트 키 가져오기
        if (!effectKeyMap.TryGetValue(type, out string effectKey))
        {
            return; // 매핑된 이펙트가 없으면 스킵
        }

        // Addressable 캐시에서 프리팹 로드
        GameObject prefab = null;
        if (AddressablePreloader.Instance != null && AddressablePreloader.Instance.HasCachedPrefab(effectKey))
        {
            prefab = AddressablePreloader.Instance.GetCachedPrefab(effectKey);
        }
        else
        {
            // 캐시에 없으면 동기 로드 시도
            var handle = Addressables.LoadAssetAsync<GameObject>(effectKey);
            prefab = handle.WaitForCompletion();
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[StatusEffectManager] 이펙트 로드 실패: {effectKey}");
                return;
            }
        }

        if (prefab == null)
        {
            return;
        }

        // 이펙트 인스턴스 생성 (대상 위에 배치)
        GameObject effectInstance = Instantiate(prefab, transform);
        effectInstance.transform.localPosition = effectOffset;
        effectInstance.transform.localScale = Vector3.one * effectScale;

        // 빨간색 계열 효과인 경우 파티클 시스템 색상 변경
        if (redColorEffects.Contains(type))
        {
            ApplyRedColorToParticles(effectInstance);
        }

        activeVisualEffects[type] = effectInstance;
    }

    private void ApplyRedColorToParticles(GameObject effectObj)
    {
        var particleSystems = effectObj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            // 기존 색상을 빨간색 계열로 변경
            Color redColor = new Color(1f, 0.3f, 0.2f, main.startColor.color.a);
            main.startColor = new ParticleSystem.MinMaxGradient(redColor);
        }
    }

    private void RemoveVisualEffect(StatusEffectType type)
    {
        if (activeVisualEffects.TryGetValue(type, out GameObject effectObj))
        {
            if (effectObj != null)
            {
                Destroy(effectObj);
            }
            activeVisualEffects.Remove(type);
        }
    }

    private void ClearAllVisualEffects()
    {
        foreach (var kvp in activeVisualEffects)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        activeVisualEffects.Clear();
    }

    private void OnDisable()
    {
        // Enemy 죽음 이벤트 구독 해제
        if (enemy != null)
        {
            enemy.OnDeath -= OnEnemyDeath;
        }
        ClearAllVisualEffects();
    }

    private void OnDestroy()
    {
        if (enemy != null)
        {
            enemy.OnDeath -= OnEnemyDeath;
        }
        ClearAllVisualEffects();
    }

    #endregion
}