using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SkillManager : MonoBehaviour
{
    [SerializeField] private SkillUiControl skillUi;
    [SerializeField] private SkillSelectUi skillSelectUi;
    [SerializeField] private Transform skillParent;

    [Header("스킬 발동 위치 참조")]
    [SerializeField] private BattleUnitManager battleUnitManager; // 유닛 매니저 참조

    private static readonly int[] PlayerSkillIds = { 22059, 22060, 22061, 22062, 22063, 22064, 22065, 22066, 22067, 22068, 22069 };

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

    // 버프 스킬 ID 목록 (깃발 스킬들)
    private static readonly HashSet<int> BuffSkillIds = new HashSet<int>
    {
        22066, // 승리의 깃발
        22068, // 용기의 깃발
        22069, // 신속의 깃발
    };

    // 이미 로드된 스킬 인스턴스 (중복 로드 방지)
    private Dictionary<int, PlayerSkillBase> loadedSkills = new Dictionary<int, PlayerSkillBase>();

    // 직접 Addressable 로드 시 핸들 저장 (OnDestroy에서 Release)
    private List<AsyncOperationHandle<GameObject>> skillHandles = new List<AsyncOperationHandle<GameObject>>();

    private void Start()
    {
        skillUi.gameObject.SetActive(false);
        skillSelectUi.OnSkillSelected += HandleSkillSelected;

        // 참조가 null인 경우 자동으로 찾기
        AutoFindReferences();
    }

    /// <summary>
    /// Inspector에서 연결되지 않은 참조를 자동으로 찾습니다.
    /// </summary>
    private void AutoFindReferences()
    {
        // battleUnitManager: UnitManager 오브젝트에서 찾기
        if (battleUnitManager == null)
        {
            var unitManager = GameObject.Find("UnitManager");
            if (unitManager != null)
            {
                battleUnitManager = unitManager.GetComponent<BattleUnitManager>();
            }
        }
    }

    private async void HandleSkillSelected(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= PlayerSkillIds.Length)
        {
            Debug.LogWarning($"[SkillManager] 잘못된 스킬 인덱스: {skillIndex}");
            return;
        }

        int skillId = PlayerSkillIds[skillIndex];
        Vector3 spawnPosition = GetSkillSpawnPosition(skillId);

        // 이미 로드된 스킬이 있는지 확인
        if (loadedSkills.TryGetValue(skillId, out var existingSkill) && existingSkill != null)
        {
            // 버프 스킬인 경우 BattleUnitManager 주입
            InjectDependencies(existingSkill, skillId);

            skillUi.gameObject.SetActive(true);
            skillUi.AddSkill(existingSkill, spawnPosition);
            return;
        }

        // Addressable에서 스킬 로드
        var skill = await LoadSkillAsync(skillId);
        if (skill != null)
        {
            // 버프 스킬인 경우 BattleUnitManager 주입
            InjectDependencies(skill, skillId);

            skillUi.gameObject.SetActive(true);
            skillUi.AddSkill(skill, spawnPosition);
        }
    }

    /// <summary>
    /// 스킬 타입에 따른 발동 위치 반환
    /// </summary>
    private Vector3 GetSkillSpawnPosition(int skillId)
    {
        // 버프 스킬은 아군 영역 (0, -3, 0)
        if (BuffSkillIds.Contains(skillId))
        {
            return new Vector3(0f, -3f, 0f);
        }

        // 공격 스킬은 몬스터 영역 (0, 3, 0)
        return new Vector3(0f, 3f, 0f);
    }

    /// <summary>
    /// 스킬에 필요한 의존성 주입
    /// </summary>
    private void InjectDependencies(PlayerSkillBase skill, int skillId)
    {
        // 버프 스킬에 BattleUnitManager 주입
        if (BuffSkillIds.Contains(skillId) && battleUnitManager != null)
        {
            if (skill is IUnitManagerInjectable injectable)
            {
                injectable.SetBattleUnitManager(battleUnitManager);
            }
        }
    }

    // 캐시 우선 → Addressable 직접 로드 순서로 스킬 프리팹 로드
    private async UniTask<PlayerSkillBase> LoadSkillAsync(int skillId)
    {
        if (!SkillAddressableKeys.TryGetValue(skillId, out string addressableKey))
        {
            Debug.LogError($"[SkillManager] 스킬 ID {skillId}에 대한 Addressable Key가 없습니다.");
            return null;
        }

        // 캐시 히트 시 핸들 관리 불필요
        if (AddressablePreloader.Instance != null)
        {
            var cached = AddressablePreloader.Instance.GetCachedPrefab(addressableKey);
            if (cached != null)
            {
                var instance = Instantiate(cached, skillParent);
                var skill = instance.GetComponent<PlayerSkillBase>();
                if (skill != null)
                {
                    loadedSkills[skillId] = skill;
                    return skill;
                }
            }
        }

        // 캐시 미스 시 직접 로드 - 핸들 저장하여 OnDestroy에서 Release
        try
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
            skillHandles.Add(handle);
            var prefab = await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded && prefab != null)
            {
                var instance = Instantiate(prefab, skillParent);
                var skill = instance.GetComponent<PlayerSkillBase>();
                if (skill != null)
                {
                    loadedSkills[skillId] = skill;
                    return skill;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SkillManager] 스킬 로드 실패: {addressableKey}, {e.Message}");
        }

        return null;
    }

    /// <summary>
    /// 전체 플레이어 스킬 수 반환
    /// </summary>
    public int GetTotalSkillCount()
    {
        return PlayerSkillIds.Length;
    }

    /// <summary>
    /// 인덱스로 스킬 ID 조회
    /// </summary>
    public int GetSkillID(int index)
    {
        if (index < 0 || index >= PlayerSkillIds.Length)
        {
            Debug.LogWarning($"[SkillManager] 잘못된 스킬 인덱스: {index}");
            return -1;
        }
        return PlayerSkillIds[index];
    }

    private void OnDestroy()
    {
        if (skillSelectUi != null)
        {
            skillSelectUi.OnSkillSelected -= HandleSkillSelected;
        }

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
