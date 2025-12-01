using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using GameData;

public class DatabaseManager : MonoBehaviour
{
    private static DatabaseManager instance;
    public static DatabaseManager Instance => instance;

    private Database database;
    private bool isInitialized = false;

    private const int MAX_PRESET_COUNT = 5;
    private const int MAX_SKILL_PER_PRESET = 6;

    public bool IsInitialized => isInitialized;
    public UserData CurrentUser { get; private set; }

    private string UserId => AuthManager.Instance.UserId;

    #region 초기화

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async UniTaskVoid Start()
    {
        await FirebaseInitializer.Instance.WaitForInitializationAsync();
        await AuthManager.Instance.WaitForInitializationAsync();

        database = new Database();
        database.Initialize();

        isInitialized = true;
        Debug.Log("[DatabaseManager] 초기화 완료");
    }

    public async UniTask WaitForInitializationAsync()
    {
        await UniTask.WaitUntil(() => isInitialized);
    }

    #endregion

    #region 유저 데이터 로드/저장

    public async UniTask<UserData> LoadUserDataAsync()
    {
        if (string.IsNullOrEmpty(UserId))
        {
            Debug.LogError("[DB] 로그인 필요");
            return null;
        }

        Debug.Log($"[DB] 유저 데이터 로드 시작: {UserId}");
        string path = $"users/{UserId}";
        var (data, success) = await database.GetDataAsync<UserData>(path);

        Debug.Log($"[DB] GetData 결과: success={success}, data={(data != null ? "있음" : "null")}");

        if (success && data != null)
        {
            CurrentUser = data;

            // 마지막 로그인 시간 갱신
            CurrentUser.profile.lastLoginTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await SaveProfileAsync();

            Debug.Log($"[DB] 유저 데이터 로드: {CurrentUser.profile?.nickname}");
        }
        else
        {
            Debug.Log("[DB] 기존 데이터 없음, 신규 유저 생성 시작");
            CurrentUser = CreateNewUserData();
            Debug.Log($"[DB] 신규 유저 데이터 생성 완료: {CurrentUser.profile.nickname}");

            bool saveResult = await SaveAllAsync();
            Debug.Log($"[DB] 신규 유저 저장 결과: {saveResult}");
        }

        return CurrentUser;
    }

    public async UniTask<bool> SaveAllAsync()
    {
        if (CurrentUser == null || string.IsNullOrEmpty(UserId))
        {
            return false;
        }

        string path = $"users/{UserId}";
        bool success = await database.SetDataAsync(path, CurrentUser);

        if (success)
        {
            Debug.Log("[DB] 전체 저장 완료");
        }

        return success;
    }

    private UserData CreateNewUserData()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var userData = new UserData
        {
            profile = new UserProfile
            {
                oderId = UserId,
                nickname = $"Player_{UserId.Substring(0, 6)}",
                level = 1,
                exp = 0,
                lastLoginTime = now,
                createdAt = now,
                totalPlayTime = 0,
                highestStage = 0,
                totalPower = 0
            },
            currency = new UserCurrency
            {
                gold = 10000,
                diamond = 100,
                stamina = 120,
                maxStamina = 120,
                lastStaminaTime = now,
                summonTicket = 10,
                enhanceStone = 0,
                skillPoint = 0
            },
            activePresetIndex = 0,
            settings = new UserSettings()
        };

        // 기본 캐릭터
        userData.characters["char_001"] = new OwnedCharacter("char_001", 1);

        // 파티 프리셋 5개
        for (int i = 0; i < MAX_PRESET_COUNT; i++)
        {
            string key = $"preset_{i}";
            userData.partyPresets[key] = new PartyPreset(i);
        }

        // 첫 번째 프리셋 기본 설정
        userData.partyPresets["preset_0"].characterId = "char_001";
        userData.partyPresets["preset_0"].skillIds.Add("skill_001");

        return userData;
    }

    #endregion

    #region 부분 저장

    public async UniTask<bool> SaveProfileAsync()
    {
        string path = $"users/{UserId}/profile";
        return await database.SetDataAsync(path, CurrentUser.profile);
    }

    public async UniTask<bool> SaveCurrencyAsync()
    {
        string path = $"users/{UserId}/currency";
        return await database.SetDataAsync(path, CurrentUser.currency);
    }

    public async UniTask<bool> SaveCharacterAsync(string characterId)
    {
        if (!CurrentUser.characters.TryGetValue(characterId, out var character))
            return false;

        string path = $"users/{UserId}/characters/{characterId}";
        return await database.SetDataAsync(path, character);
    }

    public async UniTask<bool> SaveEquipmentAsync(string uid)
    {
        if (!CurrentUser.inventory.TryGetValue(uid, out var equipment))
            return false;

        string path = $"users/{UserId}/inventory/{uid}";
        return await database.SetDataAsync(path, equipment);
    }

    public async UniTask<bool> SaveStageProgressAsync(string stageId)
    {
        if (!CurrentUser.stageProgress.TryGetValue(stageId, out var progress))
            return false;

        string path = $"users/{UserId}/stageProgress/{stageId}";
        return await database.SetDataAsync(path, progress);
    }

    public async UniTask<bool> SavePresetAsync(int index)
    {
        string key = $"preset_{index}";
        if (!CurrentUser.partyPresets.TryGetValue(key, out var preset))
            return false;

        preset.lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string path = $"users/{UserId}/partyPresets/{key}";
        return await database.SetDataAsync(path, preset);
    }

    public async UniTask<bool> SaveActivePresetIndexAsync()
    {
        string path = $"users/{UserId}/activePresetIndex";
        return await database.SetDataAsync(path, CurrentUser.activePresetIndex);
    }

    #endregion

    #region 재화 관리

    public bool HasEnoughGold(long amount) => CurrentUser.currency.gold >= amount;
    public bool HasEnoughDiamond(int amount) => CurrentUser.currency.diamond >= amount;
    public bool HasEnoughStamina(int amount) => CurrentUser.currency.stamina >= amount;

    /// <summary>
    /// 골드 증감 (트랜잭션)
    /// </summary>
    public async UniTask<bool> AddGoldAsync(long amount)
    {
        string path = $"users/{UserId}/currency/gold";

        bool success = await database.IncrementValueAsync(path, amount);

        if (success)
        {
            CurrentUser.currency.gold += amount;
        }

        return success;
    }

    /// <summary>
    /// 다이아 증감 (트랜잭션)
    /// </summary>
    public async UniTask<bool> AddDiamondAsync(int amount)
    {
        string path = $"users/{UserId}/currency/diamond";

        bool success = await database.IncrementValueAsync(path, amount);

        if (success)
        {
            CurrentUser.currency.diamond += amount;
        }

        return success;
    }

    /// <summary>
    /// 스태미나 증감
    /// </summary>
    public async UniTask<bool> AddStaminaAsync(int amount)
    {
        int newValue = Mathf.Clamp(
            CurrentUser.currency.stamina + amount,
            0,
            CurrentUser.currency.maxStamina
        );

        CurrentUser.currency.stamina = newValue;
        CurrentUser.currency.lastStaminaTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return await SaveCurrencyAsync();
    }

    /// <summary>
    /// 스태미나 자동 회복 계산
    /// </summary>
    public void RecoverStamina(int recoveryPerMinute = 1)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsed = now - CurrentUser.currency.lastStaminaTime;
        int recovered = (int)(elapsed / 60) * recoveryPerMinute;

        if (recovered > 0)
        {
            CurrentUser.currency.stamina = Mathf.Min(
                CurrentUser.currency.stamina + recovered,
                CurrentUser.currency.maxStamina
            );
            CurrentUser.currency.lastStaminaTime = now;
        }
    }

    #endregion

    #region 캐릭터 관리

    public async UniTask<bool> AddCharacterAsync(string characterId, int star = 1)
    {
        if (CurrentUser.characters.ContainsKey(characterId))
        {
            // 이미 보유 - 각성 재료로 전환
            CurrentUser.characters[characterId].awakening++;
            return await SaveCharacterAsync(characterId);
        }

        CurrentUser.characters[characterId] = new OwnedCharacter(characterId, star);
        return await SaveCharacterAsync(characterId);
    }

    public async UniTask<bool> LevelUpCharacterAsync(string characterId, int addExp)
    {
        if (!CurrentUser.characters.TryGetValue(characterId, out var character))
            return false;

        character.exp += addExp;

        int expPerLevel = character.level * 100;
        while (character.exp >= expPerLevel)
        {
            character.exp -= expPerLevel;
            character.level++;
            expPerLevel = character.level * 100;
        }

        return await SaveCharacterAsync(characterId);
    }

    public OwnedCharacter GetCharacter(string characterId)
    {
        return CurrentUser.characters.TryGetValue(characterId, out var character) ? character : null;
    }

    public List<OwnedCharacter> GetAllCharacters()
    {
        return new List<OwnedCharacter>(CurrentUser.characters.Values);
    }

    #endregion

    #region 장비 관리

    public async UniTask<string> AddEquipmentAsync(string baseId, int type, int grade)
    {
        var equipment = new OwnedEquipment(baseId, type, grade);
        CurrentUser.inventory[equipment.uid] = equipment;
        await SaveEquipmentAsync(equipment.uid);
        return equipment.uid;
    }

    public async UniTask<bool> LevelUpEquipmentAsync(string uid, long goldCost)
    {
        if (!CurrentUser.inventory.TryGetValue(uid, out var equipment))
            return false;

        if (!HasEnoughGold(goldCost))
        {
            Debug.LogWarning("[DB] 골드 부족");
            return false;
        }

        await AddGoldAsync(-goldCost);
        equipment.level++;

        return await SaveEquipmentAsync(uid);
    }

    public async UniTask<bool> DeleteEquipmentAsync(string uid)
    {
        if (!CurrentUser.inventory.ContainsKey(uid))
            return false;

        CurrentUser.inventory.Remove(uid);

        string path = $"users/{UserId}/inventory/{uid}";
        return await database.DeleteDataAsync(path);
    }

    public OwnedEquipment GetEquipment(string uid)
    {
        return CurrentUser.inventory.TryGetValue(uid, out var equipment) ? equipment : null;
    }

    public List<OwnedEquipment> GetAllEquipments()
    {
        return new List<OwnedEquipment>(CurrentUser.inventory.Values);
    }

    public List<OwnedEquipment> GetEquipmentsByType(int type)
    {
        var result = new List<OwnedEquipment>();
        foreach (var equipment in CurrentUser.inventory.Values)
        {
            if (equipment.type == type)
            {
                result.Add(equipment);
            }
        }
        return result;
    }

    #endregion

    #region 스테이지 관리

    public async UniTask<bool> RecordStageClearAsync(string stageId, int score, float clearTime, int starRating)
    {
        if (!CurrentUser.stageProgress.TryGetValue(stageId, out var progress))
        {
            progress = new StageProgress();
            CurrentUser.stageProgress[stageId] = progress;
        }

        progress.isCleared = true;
        progress.playCount++;

        if (starRating > progress.starRating)
            progress.starRating = starRating;

        return await SaveStageProgressAsync(stageId);
    }

    public StageProgress GetStageProgress(string stageId)
    {
        return CurrentUser.stageProgress.TryGetValue(stageId, out var progress) ? progress : null;
    }

    public bool IsStageCleared(string stageId)
    {
        return CurrentUser.stageProgress.TryGetValue(stageId, out var progress) && progress.isCleared;
    }

    #endregion

    #region 파티 프리셋 관리

    public PartyPreset GetPreset(int index)
    {
        if (index < 0 || index >= MAX_PRESET_COUNT)
            return null;

        string key = $"preset_{index}";
        return CurrentUser.partyPresets.TryGetValue(key, out var preset) ? preset : null;
    }

    public PartyPreset GetActivePreset()
    {
        return GetPreset(CurrentUser.activePresetIndex);
    }

    public async UniTask<bool> SetActivePresetAsync(int index)
    {
        if (index < 0 || index >= MAX_PRESET_COUNT)
            return false;

        CurrentUser.activePresetIndex = index;
        return await SaveActivePresetIndexAsync();
    }

    public async UniTask<bool> RenamePresetAsync(int index, string newName)
    {
        var preset = GetPreset(index);
        if (preset == null) return false;

        preset.name = newName;
        return await SavePresetAsync(index);
    }

    public async UniTask<bool> SetPresetCharacterAsync(int presetIndex, string characterId)
    {
        var preset = GetPreset(presetIndex);
        if (preset == null) return false;

        if (!CurrentUser.characters.ContainsKey(characterId))
        {
            Debug.LogWarning($"[DB] 보유하지 않은 캐릭터: {characterId}");
            return false;
        }

        preset.characterId = characterId;
        return await SavePresetAsync(presetIndex);
    }

    public async UniTask<bool> RemoveSkillFromPresetAsync(int presetIndex, string skillId)
    {
        var preset = GetPreset(presetIndex);
        if (preset == null) return false;

        if (!preset.skillIds.Remove(skillId))
            return false;

        return await SavePresetAsync(presetIndex);
    }

    public async UniTask<bool> SetPresetEquipmentAsync(int presetIndex, string equipmentUid)
    {
        var preset = GetPreset(presetIndex);
        if (preset == null) return false;

        if (!CurrentUser.inventory.TryGetValue(equipmentUid, out var equipment))
        {
            Debug.LogWarning($"[DB] 보유하지 않은 장비: {equipmentUid}");
            return false;
        }

        switch (equipment.type)
        {
            case 0: preset.weaponUid = equipmentUid; break;
            case 1: preset.armorUid = equipmentUid; break;
            case 2: preset.accessoryUid = equipmentUid; break;
        }

        return await SavePresetAsync(presetIndex);
    }

    public async UniTask<bool> RemovePresetEquipmentAsync(int presetIndex, int equipmentType)
    {
        var preset = GetPreset(presetIndex);
        if (preset == null) return false;

        switch (equipmentType)
        {
            case 0: preset.weaponUid = null; break;
            case 1: preset.armorUid = null; break;
            case 2: preset.accessoryUid = null; break;
        }

        return await SavePresetAsync(presetIndex);
    }

    public async UniTask<bool> CopyPresetAsync(int sourceIndex, int targetIndex)
    {
        var source = GetPreset(sourceIndex);
        var target = GetPreset(targetIndex);

        if (source == null || target == null) return false;

        if (target.isLocked)
        {
            Debug.LogWarning("[DB] 잠긴 프리셋");
            return false;
        }

        target.characterId = source.characterId;
        target.skillIds = new List<string>(source.skillIds);
        target.weaponUid = source.weaponUid;
        target.armorUid = source.armorUid;
        target.accessoryUid = source.accessoryUid;

        return await SavePresetAsync(targetIndex);
    }

    public async UniTask<bool> ResetPresetAsync(int index)
    {
        var preset = GetPreset(index);
        if (preset == null) return false;

        if (preset.isLocked)
        {
            Debug.LogWarning("[DB] 잠긴 프리셋");
            return false;
        }

        preset.characterId = null;
        preset.skillIds.Clear();
        preset.weaponUid = null;
        preset.armorUid = null;
        preset.accessoryUid = null;

        return await SavePresetAsync(index);
    }

    public async UniTask<bool> TogglePresetLockAsync(int index)
    {
        var preset = GetPreset(index);
        if (preset == null) return false;

        preset.isLocked = !preset.isLocked;
        return await SavePresetAsync(index);
    }

    #endregion
}