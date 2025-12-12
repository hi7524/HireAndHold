using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;
using Cysharp.Threading.Tasks;

/// <summary>
/// 플레이어 스킬 테스트 컨트롤러 (개선 버전)
/// SkillTable/EffectTable 기반 스킬 로드 및 테스트
/// 스킬 사용 시 상태이상 자동 적용
/// </summary>
public class TestPlayerSkillController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown skillDropdown;
    [SerializeField] private Button useSkillButton;
    [SerializeField] private Toggle noCooldownToggle;
    [SerializeField] private TMP_Text skillInfoText;

    [Header("Skill Spawn Point")]
    [SerializeField] private Transform skillSpawnPoint;

    // SkillTable 기반 플레이어 스킬 목록 (SKILL_OBJECT == 2)
    private List<PlayerSkillInfo> playerSkillInfos = new List<PlayerSkillInfo>();
    private int selectedSkillIndex = -1;
    private bool noCooldown = false;

    // 로드된 스킬 인스턴스
    private Dictionary<int, PlayerSkillBase> loadedSkills = new Dictionary<int, PlayerSkillBase>();
    private List<AsyncOperationHandle<GameObject>> skillHandles = new List<AsyncOperationHandle<GameObject>>();

    // SkillID → Addressable Key 매핑 (SkillManager와 동일)
    private static readonly Dictionary<int, string> SkillAddressableKeys = new Dictionary<int, string>
    {
        { 22059, "EarthQuake" },
        { 22060, "EternalBlizzard" },
        { 22061, "BlackHoleSkill" },
        { 22062, "AirForce" },
        { 22063, "ChaosWave" },
        { 22064, "AnkleCatch" },
        { 22065, "SuperNova" },
        { 22066, "FlagOfVictory" },
        { 22067, "GreatSlow" },
        { 22068, "FlagOfCourage" },
        { 22069, "FlagOfSpeed" },
    };

    // 버프 스킬 ID 목록
    private static readonly HashSet<int> BuffSkillIds = new HashSet<int>
    {
        22066, // 승리의 깃발
        22068, // 용기의 깃발
        22069, // 신속의 깃발
    };

    // 상태이상 타입 이름 매핑
    private static readonly Dictionary<int, string> EffectTypeNames = new Dictionary<int, string>
    {
        { 0, "없음" },
        { 1, "슬로우" },
        { 2, "스턴" },
        { 3, "지속 데미지" },
        { 4, "방어력 감소" },
        { 5, "이동 불가" },
        { 6, "넉백" },
        { 7, "끌어당김" },
        { 8, "공격력 증가" },
        { 9, "공격속도 증가" },
        { 10, "고정 데미지" },
        { 11, "고정 데미지" },
    };

    /// <summary>
    /// 플레이어 스킬 정보 구조체
    /// </summary>
    private struct PlayerSkillInfo
    {
        public int skillId;
        public string skillName;
        public float cooltime;
        public float damage;
        public int effectType;      // 상태이상 타입
        public float effectValue;   // 상태이상 값
        public float effectDuration;// 상태이상 지속시간
        public string effectPrefab; // 이펙트 프리팹 키
        public bool isBuff;         // 버프 스킬 여부
    }

    public void Initialize()
    {
        SetupDropdownAsync().Forget();
        SetupButtons();
    }

    private async UniTaskVoid SetupDropdownAsync()
    {
        // DataTable 로드 대기
        while (!DataTableManager.IsInitialized)
        {
            await UniTask.Yield();
        }

        LoadPlayerSkills();
    }

    /// <summary>
    /// SkillTable에서 플레이어 스킬(SKILL_OBJECT=2) 목록 로드
    /// </summary>
    private void LoadPlayerSkills()
    {
        playerSkillInfos.Clear();

        if (skillDropdown != null)
        {
            skillDropdown.ClearOptions();
        }

        var skillTable = DataTableManager.SkillTable;
        var effectTable = DataTableManager.EffectTable;

        if (skillTable == null)
        {
            Debug.LogError("[TestPlayerSkill] SkillTable이 없습니다!");
            return;
        }

        var allSkills = skillTable.GetAll();
        var options = new List<TMP_Dropdown.OptionData>();

        foreach (var skill in allSkills)
        {
            // SKILL_OBJECT == 2: 플레이어 스킬만 필터
            // 스킬 ID 22059~22069 범위만 (활성화된 플레이어 스킬)
            if (skill.SKILL_OBJECT != 2) continue;
            if (!SkillAddressableKeys.ContainsKey(skill.SKILL_ID)) continue;

            var info = new PlayerSkillInfo
            {
                skillId = skill.SKILL_ID,
                skillName = GetSkillName(skill.SKILL_NAME),
                cooltime = skill.SKILL_COOLTIME,
                effectPrefab = skill.SKILL_EFFECT,
                isBuff = BuffSkillIds.Contains(skill.SKILL_ID)
            };

            // SKILL_EFFECT1_ID: 상태이상 정보
            if (skill.SKILL_EFFECT1_ID > 0 && effectTable != null)
            {
                var effectData = effectTable.Get(skill.SKILL_EFFECT1_ID);
                if (effectData != null)
                {
                    info.effectType = effectData.EFFECT_TYPE;
                    info.effectValue = effectData.EFFECT_VALUE;
                }
            }
            info.effectDuration = skill.EFFECT_TIME1;

            // SKILL_EFFECT2_ID: 데미지 정보
            if (skill.SKILL_EFFECT2_ID > 0 && effectTable != null)
            {
                var effectData = effectTable.Get(skill.SKILL_EFFECT2_ID);
                if (effectData != null)
                {
                    info.damage = effectData.EFFECT_VALUE;
                }
            }

            playerSkillInfos.Add(info);

            // 드롭다운 옵션: ID: 이름 (타입)
            string typeStr = info.isBuff ? "버프" : "공격";
            string displayName = $"{skill.SKILL_ID}: {info.skillName} ({typeStr})";
            options.Add(new TMP_Dropdown.OptionData(displayName));
        }

        if (skillDropdown != null)
        {
            skillDropdown.AddOptions(options);
            skillDropdown.onValueChanged.AddListener(OnSkillSelected);
        }

        // 첫 번째 스킬 선택
        if (playerSkillInfos.Count > 0)
        {
            selectedSkillIndex = 0;
            UpdateSkillInfo();
        }

        Debug.Log($"[TestPlayerSkill] {playerSkillInfos.Count}개 플레이어 스킬 로드 완료");
    }

    /// <summary>
    /// 스킬 이름 가져오기 (StringTable 또는 원본)
    /// </summary>
    private string GetSkillName(string nameField)
    {
        if (int.TryParse(nameField, out int nameId))
        {
            string localized = DataTableManager.StringTable?.Get(nameId);
            if (!string.IsNullOrEmpty(localized))
                return localized;
        }
        return nameField;
    }

    private void SetupButtons()
    {
        if (useSkillButton != null)
        {
            useSkillButton.onClick.AddListener(OnUseSkillClicked);
        }

        if (noCooldownToggle != null)
        {
            noCooldownToggle.onValueChanged.AddListener(OnNoCooldownChanged);
        }
    }

    private void OnSkillSelected(int index)
    {
        if (index >= 0 && index < playerSkillInfos.Count)
        {
            selectedSkillIndex = index;
            UpdateSkillInfo();
            Debug.Log($"[TestPlayerSkill] 선택: {playerSkillInfos[index].skillName} (ID: {playerSkillInfos[index].skillId})");
        }
    }

    /// <summary>
    /// 선택된 스킬 정보 표시
    /// </summary>
    private void UpdateSkillInfo()
    {
        if (skillInfoText == null || selectedSkillIndex < 0) return;

        var info = playerSkillInfos[selectedSkillIndex];

        string effectTypeName = EffectTypeNames.TryGetValue(info.effectType, out string name) ? name : $"타입{info.effectType}";

        string infoStr = $"<b>{info.skillName}</b>\n";
        infoStr += $"ID: {info.skillId}\n";
        infoStr += $"타입: {(info.isBuff ? "버프" : "공격")}\n";
        infoStr += $"쿨타임: {info.cooltime}초\n";

        if (info.damage > 0)
        {
            infoStr += $"데미지: {info.damage}\n";
        }

        if (info.effectType > 0)
        {
            infoStr += $"\n<color=#FFD700>상태이상:</color>\n";
            infoStr += $"  {effectTypeName}";

            if (info.effectValue > 0)
            {
                // 상태이상 값 해석
                if (info.effectType == 1) // Slow
                    infoStr += $" ({info.effectValue}%)";
                else if (info.effectType == 4) // DefenseDown
                    infoStr += $" ({info.effectValue}%)";
                else if (info.effectType == 6) // Knockback
                    infoStr += $" (힘: {info.effectValue})";
                else if (info.effectType == 8) // AttackUp
                    infoStr += $" (+{info.effectValue}%)";
                else if (info.effectType == 9) // DamageUpPercent
                    infoStr += $" (+{info.effectValue}%)";
            }

            if (info.effectDuration > 0)
            {
                infoStr += $"\n  지속: {info.effectDuration}초";
            }
        }

        skillInfoText.text = infoStr;
    }

    /// <summary>
    /// 스킬 사용 버튼 클릭
    /// </summary>
    private void OnUseSkillClicked()
    {
        if (selectedSkillIndex < 0 || selectedSkillIndex >= playerSkillInfos.Count)
        {
            Debug.LogWarning("[TestPlayerSkill] 스킬을 선택해주세요!");
            return;
        }

        var info = playerSkillInfos[selectedSkillIndex];
        UseSkillAsync(info.skillId).Forget();
    }

    /// <summary>
    /// 스킬 로드 및 사용
    /// </summary>
    private async UniTaskVoid UseSkillAsync(int skillId)
    {
        // 이미 로드된 스킬 확인
        if (!loadedSkills.TryGetValue(skillId, out var skill) || skill == null)
        {
            // Addressable에서 스킬 로드
            skill = await LoadSkillAsync(skillId);
            if (skill == null)
            {
                Debug.LogError($"[TestPlayerSkill] 스킬 로드 실패: {skillId}");
                return;
            }
        }

        // 쿨다운 무시 옵션
        if (noCooldown)
        {
            skill.isOnCoolTime = false;
            skill.elapsed = skill.CoolDown;
        }

        // 스킬 발동 위치 결정
        Vector3 spawnPos = GetSkillSpawnPosition(skillId);

        // 스킬 사용 (이펙트만 생성)
        skill.TryUse(spawnPos);

        var info = playerSkillInfos.Find(x => x.skillId == skillId);
        Debug.Log($"[TestPlayerSkill] 스킬 사용: {info.skillName} (위치: {spawnPos})");

        // 버프 스킬이 아닌 경우에만 테스트 씬용 직접 데미지/상태이상 적용
        if (!info.isBuff)
        {
            ApplySkillEffectToMonsters(info);
        }
    }

    /// <summary>
    /// Addressable에서 스킬 프리팹 로드
    /// </summary>
    private async UniTask<PlayerSkillBase> LoadSkillAsync(int skillId)
    {
        if (!SkillAddressableKeys.TryGetValue(skillId, out string addressableKey))
        {
            Debug.LogError($"[TestPlayerSkill] 스킬 ID {skillId}에 대한 Addressable Key가 없습니다.");
            return null;
        }

        try
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
            skillHandles.Add(handle);
            var prefab = await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded && prefab != null)
            {
                var instance = Instantiate(prefab, transform);
                instance.SetActive(true);

                var skill = instance.GetComponent<PlayerSkillBase>();
                if (skill != null)
                {
                    skill.Init();
                    loadedSkills[skillId] = skill;
                    return skill;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TestPlayerSkill] 스킬 로드 실패: {addressableKey}, {e.Message}");
        }

        return null;
    }

    /// <summary>
    /// 스킬 타입에 따른 발동 위치 반환
    /// </summary>
    private Vector3 GetSkillSpawnPosition(int skillId)
    {
        // skillSpawnPoint가 설정되어 있으면 사용
        if (skillSpawnPoint != null)
        {
            // 버프 스킬은 아래쪽 (아군 영역)
            if (BuffSkillIds.Contains(skillId))
            {
                return new Vector3(skillSpawnPoint.position.x, 0f, skillSpawnPoint.position.z);
            }
            return skillSpawnPoint.position;
        }

        // 기본값
        if (BuffSkillIds.Contains(skillId))
        {
            return new Vector3(0f, 0f, 0f);
        }
        return new Vector3(0f, 3f, 0f);
    }

    private void OnNoCooldownChanged(bool value)
    {
        noCooldown = value;
        Debug.Log($"[TestPlayerSkill] 쿨다운 무시: {noCooldown}");
    }

    /// <summary>
    /// 외부에서 스킬 직접 사용 (인덱스)
    /// </summary>
    public void UseSkillByIndex(int index)
    {
        if (index >= 0 && index < playerSkillInfos.Count)
        {
            UseSkillAsync(playerSkillInfos[index].skillId).Forget();
        }
    }

    /// <summary>
    /// 외부에서 스킬 직접 사용 (ID)
    /// </summary>
    public void UseSkillById(int skillId)
    {
        UseSkillAsync(skillId).Forget();
    }

    /// <summary>
    /// 테스트 씬용: 직접 몬스터에게 데미지와 상태이상 적용
    /// (PlayerSkillBase의 DamageEnemiesInRange가 테스트 씬에서 작동하지 않는 문제 우회)
    /// </summary>
    private void ApplySkillEffectToMonsters(PlayerSkillInfo info)
    {
        if (!MonsterProviderRegistry.HasProvider)
        {
            Debug.LogWarning("[TestPlayerSkill] MonsterProviderRegistry에 Provider가 없습니다!");
            return;
        }

        var monsters = MonsterProviderRegistry.GetActiveMonsters();
        int hitCount = 0;

        foreach (var enemy in monsters)
        {
            if (enemy == null || !enemy.gameObject.activeSelf || enemy.IsDead) continue;

            // 데미지 적용
            if (info.damage > 0)
            {
                enemy.TakeDamage(info.damage);
            }

            // 상태이상 적용
            if (info.effectType > 0 && info.effectDuration > 0)
            {
                var statusEffectManager = enemy.GetComponent<StatusEffectManager>();
                if (statusEffectManager == null)
                {
                    statusEffectManager = enemy.gameObject.AddComponent<StatusEffectManager>();
                }

                // 이펙트 오프셋 조정
                AdjustEffectOffset(statusEffectManager);

                var effect = CreateStatusEffect((StatusEffectType)info.effectType, info.effectDuration, info.effectValue);
                if (effect != null)
                {
                    statusEffectManager.AddStatusEffect((StatusEffectType)info.effectType, effect);
                }
            }

            hitCount++;
        }

        Debug.Log($"[TestPlayerSkill] {hitCount}마리 몬스터에 데미지({info.damage}) 및 상태이상({(StatusEffectType)info.effectType}) 적용");
    }

    /// <summary>
    /// StatusEffectManager의 이펙트 오프셋 조정 (테스트 씬용)
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

    /// <summary>
    /// 상태이상 효과 생성
    /// </summary>
    private StatusEffect CreateStatusEffect(StatusEffectType type, float duration, float value)
    {
        switch (type)
        {
            case StatusEffectType.Stun:
                return new StatusEffectStun(0f, duration);
            case StatusEffectType.Slow:
                return new StatusEffectSlow(value, duration);
            case StatusEffectType.DamageOverTime:
                return new StatusEffectDamageOverTime(value, duration);
            case StatusEffectType.DefenseDown:
                return new StatusEffectDefenseDown(value, duration);
            case StatusEffectType.Root:
                return new StatusEffectRoot(value, duration);
            case StatusEffectType.Knockback:
                return new StatusEffectKnockback(value, duration);
            case StatusEffectType.Pull:
                return new StatusEffectPull(value, Vector3.zero, duration);
            default:
                Debug.LogWarning($"[TestPlayerSkill] 지원하지 않는 상태 효과: {type}");
                return null;
        }
    }

    /// <summary>
    /// 모든 스킬 쿨다운 리셋
    /// </summary>
    public void ResetAllCooldowns()
    {
        foreach (var skill in loadedSkills.Values)
        {
            if (skill != null)
            {
                skill.isOnCoolTime = false;
                skill.elapsed = skill.CoolDown;
            }
        }
        Debug.Log("[TestPlayerSkill] 모든 스킬 쿨다운 리셋됨");
    }

    /// <summary>
    /// 스킬 개수 반환
    /// </summary>
    public int GetSkillCount()
    {
        return playerSkillInfos.Count;
    }

    /// <summary>
    /// 스킬 ID 반환
    /// </summary>
    public int GetSkillId(int index)
    {
        if (index >= 0 && index < playerSkillInfos.Count)
        {
            return playerSkillInfos[index].skillId;
        }
        return -1;
    }

    private void OnDestroy()
    {
        // Addressable 핸들 해제
        foreach (var handle in skillHandles)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        skillHandles.Clear();
    }
}
