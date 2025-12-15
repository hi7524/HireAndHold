using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// 상태 효과 이펙트 테스트 컨트롤러
/// 몬스터에 직접 상태 효과를 적용하여 이펙트를 테스트
/// </summary>
public class TestStatusEffectController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown effectDropdown;
    [SerializeField] private Button applyEffectButton;
    [SerializeField] private Button clearEffectsButton;
    [SerializeField] private TMP_InputField durationInput;
    [SerializeField] private TMP_InputField valueInput;
    [SerializeField] private Toggle applyToAllToggle;

    [Header("Effect Settings")]
    [SerializeField] private float defaultDuration = 5f;
    [SerializeField] private float defaultValue = 50f;

    private StatusEffectType selectedEffectType = StatusEffectType.Slow;
    private List<StatusEffectType> availableEffects = new List<StatusEffectType>();

    // 이펙트가 있는 상태 효과 타입들
    private static readonly Dictionary<StatusEffectType, string> effectDisplayNames = new Dictionary<StatusEffectType, string>
    {
        { StatusEffectType.DefenseDown, "방어력 감소 (fear)" },
        { StatusEffectType.Root, "이동 불가 (stuned)" },
        { StatusEffectType.Slow, "이동 속도 감소 (slowDown)" },
        { StatusEffectType.Stun, "행동 불가 (confusion)" },
        { StatusEffectType.AttackUp, "공격력 증가 (powerUp)" },
        { StatusEffectType.DamageUpPercent, "공격 속도 증가 (powerUp 빨강)" },
    };

    public void Initialize()
    {
        SetupDropdown();
        SetupButtons();
        SetupInputs();

        Debug.Log("[TestStatusEffect] 상태 효과 테스트 컨트롤러 초기화 완료");
    }

    private void SetupDropdown()
    {
        if (effectDropdown == null) return;

        effectDropdown.ClearOptions();
        availableEffects.Clear();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach (var kvp in effectDisplayNames)
        {
            availableEffects.Add(kvp.Key);
            options.Add(new TMP_Dropdown.OptionData(kvp.Value));
        }

        effectDropdown.AddOptions(options);
        effectDropdown.onValueChanged.AddListener(OnEffectSelected);

        // 첫 번째 효과 선택
        if (availableEffects.Count > 0)
        {
            selectedEffectType = availableEffects[0];
        }
    }

    private void SetupButtons()
    {
        if (applyEffectButton != null)
        {
            applyEffectButton.onClick.AddListener(OnApplyEffectClicked);
        }

        if (clearEffectsButton != null)
        {
            clearEffectsButton.onClick.AddListener(OnClearEffectsClicked);
        }
    }

    private void SetupInputs()
    {
        if (durationInput != null)
        {
            durationInput.text = defaultDuration.ToString("F1");
        }

        if (valueInput != null)
        {
            valueInput.text = defaultValue.ToString("F1");
        }
    }

    private void OnEffectSelected(int index)
    {
        if (index >= 0 && index < availableEffects.Count)
        {
            selectedEffectType = availableEffects[index];
            Debug.Log($"[TestStatusEffect] 선택된 효과: {effectDisplayNames[selectedEffectType]}");
        }
    }

    private void OnApplyEffectClicked()
    {
        float duration = defaultDuration;
        float value = defaultValue;

        if (durationInput != null && float.TryParse(durationInput.text, out float parsedDuration))
        {
            duration = parsedDuration;
        }

        if (valueInput != null && float.TryParse(valueInput.text, out float parsedValue))
        {
            value = parsedValue;
        }

        bool applyToAll = applyToAllToggle != null && applyToAllToggle.isOn;

        if (applyToAll)
        {
            ApplyEffectToAllMonsters(selectedEffectType, duration, value);
        }
        else
        {
            ApplyEffectToNearestMonster(selectedEffectType, duration, value);
        }
    }

    private void ApplyEffectToAllMonsters(StatusEffectType type, float duration, float value)
    {
        var monsters = GameObject.FindGameObjectsWithTag("Monster");
        int count = 0;

        foreach (var monsterObj in monsters)
        {
            var enemy = monsterObj.GetComponent<Enemy>();
            if (enemy == null || enemy.IsDead) continue;

            var statusEffectManager = monsterObj.GetComponent<StatusEffectManager>();
            if (statusEffectManager == null)
            {
                // StatusEffectManager가 없으면 추가
                statusEffectManager = monsterObj.AddComponent<StatusEffectManager>();
            }

            // 상태이상 이펙트 높이 조정 (테스트 씬용)
            AdjustEffectOffset(statusEffectManager);

            var effect = CreateStatusEffect(type, duration, value);
            if (effect != null)
            {
                statusEffectManager.AddStatusEffect(type, effect);
                count++;
            }
        }

        Debug.Log($"[TestStatusEffect] {count}마리 몬스터에 {effectDisplayNames[type]} 적용 (지속: {duration}초, 값: {value})");
    }

    private void ApplyEffectToNearestMonster(StatusEffectType type, float duration, float value)
    {
        var monsters = GameObject.FindGameObjectsWithTag("Monster");
        GameObject nearest = null;
        float minDistance = float.MaxValue;
        Vector3 center = Camera.main != null ? Camera.main.transform.position : Vector3.zero;

        foreach (var monsterObj in monsters)
        {
            var enemy = monsterObj.GetComponent<Enemy>();
            if (enemy == null || enemy.IsDead) continue;

            float distance = Vector3.Distance(center, monsterObj.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = monsterObj;
            }
        }

        if (nearest != null)
        {
            var statusEffectManager = nearest.GetComponent<StatusEffectManager>();
            if (statusEffectManager == null)
            {
                statusEffectManager = nearest.AddComponent<StatusEffectManager>();
            }

            // 상태이상 이펙트 높이 조정 (테스트 씬용)
            AdjustEffectOffset(statusEffectManager);

            var effect = CreateStatusEffect(type, duration, value);
            if (effect != null)
            {
                statusEffectManager.AddStatusEffect(type, effect);
                Debug.Log($"[TestStatusEffect] 가장 가까운 몬스터에 {effectDisplayNames[type]} 적용");
            }
        }
        else
        {
            Debug.LogWarning("[TestStatusEffect] 적용할 몬스터가 없습니다!");
        }
    }

    private void OnClearEffectsClicked()
    {
        var monsters = GameObject.FindGameObjectsWithTag("Monster");
        int count = 0;

        foreach (var monsterObj in monsters)
        {
            var statusEffectManager = monsterObj.GetComponent<StatusEffectManager>();
            if (statusEffectManager != null)
            {
                statusEffectManager.StopAllEffects();
                count++;
            }
        }

        Debug.Log($"[TestStatusEffect] {count}마리 몬스터의 모든 상태 효과 제거됨");
    }

    /// <summary>
    /// StatusEffectManager의 이펙트 오프셋을 조정 (테스트 씬용)
    /// </summary>
    private void AdjustEffectOffset(StatusEffectManager manager)
    {
        var effectOffsetField = typeof(StatusEffectManager).GetField("effectOffset",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (effectOffsetField != null)
        {
            effectOffsetField.SetValue(manager, new Vector3(0f, 0.2f, 0f));
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
            case StatusEffectType.DefenseDown:
                return new StatusEffectDefenseDown(value, duration);
            case StatusEffectType.Root:
                return new StatusEffectRoot(value, duration);
            case StatusEffectType.AttackUp:
                // AttackUp은 버프용이지만 테스트 목적으로 생성
                return new StatusEffectSlow(0f, duration); // 임시: 슬로우 0%로 대체
            case StatusEffectType.DamageUpPercent:
                // DamageUpPercent도 버프용이지만 테스트 목적으로 생성
                return new StatusEffectSlow(0f, duration); // 임시: 슬로우 0%로 대체
            default:
                Debug.LogWarning($"[TestStatusEffect] 지원하지 않는 상태 효과: {type}");
                return null;
        }
    }

    // 키보드 단축키로 빠른 테스트
    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 숫자 키로 빠른 효과 적용
        if (keyboard.digit1Key.wasPressedThisFrame) ApplyEffectQuick(0);
        if (keyboard.digit2Key.wasPressedThisFrame) ApplyEffectQuick(1);
        if (keyboard.digit3Key.wasPressedThisFrame) ApplyEffectQuick(2);
        if (keyboard.digit4Key.wasPressedThisFrame) ApplyEffectQuick(3);
        if (keyboard.digit5Key.wasPressedThisFrame) ApplyEffectQuick(4);
        if (keyboard.digit6Key.wasPressedThisFrame) ApplyEffectQuick(5);

        // C키로 모든 효과 제거
        if (keyboard.cKey.wasPressedThisFrame)
        {
            OnClearEffectsClicked();
        }
    }

    public void ApplyEffectQuick(int index)
    {
        if (index >= 0 && index < availableEffects.Count)
        {
            selectedEffectType = availableEffects[index];
            if (effectDropdown != null)
            {
                effectDropdown.value = index;
            }

            bool applyToAll = applyToAllToggle != null && applyToAllToggle.isOn;
            if (applyToAll)
            {
                ApplyEffectToAllMonsters(selectedEffectType, defaultDuration, defaultValue);
            }
            else
            {
                ApplyEffectToNearestMonster(selectedEffectType, defaultDuration, defaultValue);
            }
        }
    }
}
