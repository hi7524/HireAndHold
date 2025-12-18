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

        SyncPresetsToPlayData();

        PlayData.SyncFromDatabase();

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
            //Debug.Log("[DB] 전체 저장 완료");
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
                highestStage = 701,
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
                enhanceStone = 1000,
                skillPoint = 0
            },
            activePresetIndex = 0,
            settings = new UserSettings()
        };

        int[] initialUnitIds = { 11101, 11119, 11107, 11110, 11113 };

        foreach (int id in initialUnitIds)
        {
            string key = id.ToString();
            if (!userData.characters.ContainsKey(key))
                userData.characters[key] = new OwnedCharacter(key, 1);
        }

        for (int i = 0; i < MAX_PRESET_COUNT; i++)
        {
            string key = $"preset_{i}";
            userData.partyPresets[key] = new PartyPreset(i);
        }

        var firstPreset = userData.partyPresets["preset_0"];
        for (int i = 0; i < 5 && i < initialUnitIds.Length; i++)
        {
            firstPreset.characterId[i] = initialUnitIds[i].ToString();
        }

        return userData;
    }



    public void SyncPresetsToPlayData()
    {
        if (CurrentUser == null)
            return;

        PlayData.currentSelectedPreset = CurrentUser.activePresetIndex;

        for (int p = 0; p < MAX_PRESET_COUNT; p++)
        {
            string key = $"preset_{p}";

            if (!CurrentUser.partyPresets.TryGetValue(key, out var preset)
                || preset.characterId == null
                || preset.characterId.Length < 5)
            {
                // 빈 프리셋으로 처리
                for (int s = 0; s < 5; s++)
                {
                    PlayData.selectedDeckUnitIds[p, s] = 0;
                    PlayData.selectedDeckUnitIconAddresses[p, s] = "";
                }

                continue;
            }

            // 정상 프리셋 로드
            for (int s = 0; s < 5; s++)
            {
                // 캐릭터 ID 읽기
                int unitId = 0;
                string value = preset.characterId[s];

                if (!string.IsNullOrEmpty(value))
                    int.TryParse(value, out unitId);

                PlayData.selectedDeckUnitIds[p, s] = unitId;

                // 아이콘 주소 읽기
                if (preset.iconAddress != null && preset.iconAddress.Length > s)
                    PlayData.selectedDeckUnitIconAddresses[p, s] = preset.iconAddress[s] ?? "";
                else
                    PlayData.selectedDeckUnitIconAddresses[p, s] = "";
            }
        }
    }


    public async UniTask<bool> SavePresetFromPlayDataAsync(int index)
    {
        var preset = GetPreset(index);
        if (preset == null)
            return false;

        if (preset.characterId == null || preset.characterId.Length != 5)
            preset.characterId = new string[5];

        if (preset.iconAddress == null || preset.iconAddress.Length != 5)
            preset.iconAddress = new string[5];

        for (int s = 0; s < 5; s++)
        {
            int unitId = PlayData.selectedDeckUnitIds[index, s];

            preset.characterId[s] = unitId == 0 ? null : unitId.ToString();
            preset.iconAddress[s] = PlayData.selectedDeckUnitIconAddresses[index, s];
        }

        preset.lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return await SavePresetAsync(index);
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

        try
        {
            Debug.Log("[GoldTest] Increment 시작");

            bool success = await database.IncrementValueAsync(path, amount);

            Debug.Log($"[GoldTest] Increment 결과 = {success}");

            if (success)
            {
                CurrentUser.currency.gold += amount;
                PlayData.SetGoldImmediate(CurrentUser.currency.gold);
            }

            var (value, ok) = await database.GetDataAsync<object>(path);
            Debug.Log($"[GoldCheck] value={value}, type={value?.GetType()}");

            return success;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GoldTest ERROR] {e}");
            return false;
        }
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

    public async UniTask<bool> AddEnhanceStoneAsync(int amount)
    {
        string path = $"users/{UserId}/currency/enhanceStone";
        bool success = await database.IncrementValueAsync(path, amount);

        if (success)
        {
            CurrentUser.currency.enhanceStone += amount;
            PlayData.SetEnhanceStoneImmediate(CurrentUser.currency.enhanceStone);
            PlayData.NotifyCurrencyChanged();
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

    #region 경험치 및 레벨 관리

    /// <summary>
    /// 유저 경험치 증가 (트랜잭션)
    /// </summary>
    public async UniTask<bool> AddExpAsync(int amount)
    {
        string path = $"users/{UserId}/profile/exp";

        bool success = await database.IncrementValueAsync(path, amount);

        if (success)
        {
            CurrentUser.profile.exp += amount;

            int expForNextLevel = CurrentUser.profile.level * 100;
            while (CurrentUser.profile.exp >= expForNextLevel)
            {
                CurrentUser.profile.exp -= expForNextLevel;
                CurrentUser.profile.level++;
                expForNextLevel = CurrentUser.profile.level * 100;
            }

            await SaveProfileAsync();

   
            PlayData.SetProfileImmediate(
                CurrentUser.profile.level,
                CurrentUser.profile.exp
            );
        }

        return success;
    }


    #endregion

    #region 캐릭터 관리

    public async UniTask<bool> AddCharacterAsync(string characterId, int star = 1)
    {
        if (CurrentUser.characters.ContainsKey(characterId))
        {
            // 이미 보유 - 조각 1개로 변환
            if (int.TryParse(characterId, out int unitId))
            {
                var unitData = DataTableManager.UnitTable?.Get(unitId);
                if (unitData != null && unitData.FRAGMENT_ITEM_ID > 0)
                {
                    return await AddItemAsync(unitData.FRAGMENT_ITEM_ID, 1);
                }
            }
            return false;
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

    public async UniTask SetEnforceLevelAsync(string characterId, int newLevel)
    {
        if (CurrentUser == null)
        {
            Debug.LogError("CurrentUser is null! Enforce 저장 실패");
            return;
        }

        if (!CurrentUser.characters.ContainsKey(characterId))
        {
            Debug.LogError($"Character {characterId} 를 찾을 수 없음");
            return;
        }

        CurrentUser.characters[characterId].enforceLevel = newLevel;

        await SaveCharacterAsync(characterId);
        Debug.Log($"[DB] 강화레벨 저장 완료: ID={characterId}, enforce={newLevel}");
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

    #region 아이템 관리

    /// <summary>
    /// 아이템 수량 증감 (트랜잭션)
    /// </summary>
    public async UniTask<bool> AddItemAsync(int itemId, int amount)
    {
        string key = itemId.ToString();
        string path = $"users/{UserId}/items/{key}";

        bool success = await database.IncrementValueAsync(path, amount);

        if (success)
        {
            if (!CurrentUser.items.ContainsKey(key))
                CurrentUser.items[key] = 0;

            CurrentUser.items[key] += amount;

            if (CurrentUser.items[key] <= 0)
                CurrentUser.items.Remove(key);

            int newCount = GetItemCount(itemId);
            PlayData.SetItemCountImmediate(itemId, newCount);
            PlayData.NotifyCurrencyChanged();
        }

        return success;
    }


    /// <summary>
    /// 아이템 수량 조회
    /// </summary>
    public int GetItemCount(int itemId)
    {
        if (CurrentUser?.items == null) return 0;

        string key = itemId.ToString();
        return CurrentUser.items.TryGetValue(key, out int count) ? count : 0;
    }

    /// <summary>
    /// 아이템 보유 여부 확인
    /// </summary>
    public bool HasEnoughItem(int itemId, int amount)
    {
        return GetItemCount(itemId) >= amount;
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

        int stageNumber = int.Parse(stageId);

        if (stageNumber > CurrentUser.profile.highestStage)
        {
            CurrentUser.profile.highestStage = stageNumber;
            await SaveProfileAsync();

            PlayData.SetLastClearedStageImmediate(stageNumber);
        }

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

        // 배열 길이 보장
        if (preset.characterId == null || preset.characterId.Length != 5)
            preset.characterId = new string[5];


        preset.characterId[0] = characterId;

        preset.lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

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
