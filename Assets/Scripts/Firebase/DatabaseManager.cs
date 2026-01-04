using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using GameData;

public class DatabaseManager : MonoBehaviour
{
    private const int ENHANCE_STONE_ITEM_ID = 5201;

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

        await DataTableManager.InitAsync();

        isInitialized = true;
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
        string path = $"users/{UserId}";
        var (data, success) = await database.GetDataAsync<UserData>(path);
        if (success && data != null)
        {
            CurrentUser = data;

            // 실제 출석 일수 기반 업적 연동 (totalClaimCount 사용)
            int loginDays = CurrentUser.dailyReward?.totalClaimCount ?? 0;
            if (loginDays > 0)
            {
                await AchievementManager.UpdateLoginDaysAsync(loginDays);
            }

            // 마지막 로그인 시간 갱신
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            CurrentUser.profile.lastLoginTime = now;
            await SaveProfileAsync();
        }
        else
        {
            CurrentUser = CreateNewUserData();
            bool saveResult = await SaveAllAsync();
            // 신규 가입은 아직 출석 체크 전이므로 업적 갱신하지 않음
            // 출석 체크(ClaimDailyRewardAsync) 시 업적이 갱신됨
        }

        SyncPresetsToPlayData();

        PlayData.SyncFromDatabase();

        // 전역 메일 로드
        await LoadGlobalMailsAsync();

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
        }

        return success;
    }

    private UserData CreateNewUserData()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var userData = new UserData
        {
            presetSlotUnlocks = new PresetSlotUnlockData(),
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
                totalPower = 0,
            },
            currency = new UserCurrency
            {
                gold = 0,
                diamond = 0,
                stamina = 50,
                maxStamina = 120,
                lastStaminaTime = now,
                enhanceStone = 0,
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

        // 기본 프리셋에 처음 2개 유닛 설정
        var firstPreset = userData.partyPresets["preset_0"];
        for (int i = 0; i < 2 && i < initialUnitIds.Length; i++)
        {
            int unitId = initialUnitIds[i];
            firstPreset.characterId[i] = unitId.ToString();

            // 유닛 아이콘 주소도 설정
            var unitData = DataTableManager.UnitTable?.Get(unitId);
            if (unitData != null && !string.IsNullOrEmpty(unitData.UNIT_ICON))
            {
                firstPreset.iconAddress[i] = unitData.UNIT_ICON;
            }
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

            // 모든 슬롯을 0으로 초기화
            for (int s = 0; s < 5; s++)
            {
                PlayData.selectedDeckUnitIds[p, s] = 0;
                PlayData.selectedDeckUnitIconAddresses[p, s] = "";
            }

            if (!CurrentUser.partyPresets.TryGetValue(key, out var preset) || preset.characterId == null)
            {
                // 프리셋이 없으면 빈 상태 유지
                Debug.LogWarning($"[DatabaseManager SyncPresetsToPlayData] 프리셋 {p} - Firebase에 데이터 없음");
                continue;
            }

            // 정상 프리셋 로드 - 해제된 슬롯만 순서대로 채움
            int arrayLength = preset.characterId.Length;

            int arrayIndex = 0;
            for (int s = 0; s < 5; s++)
            {
                if (!IsSlotUnlocked(p, s))
                    continue;

                if (arrayIndex >= arrayLength)
                    break;

                // 배열에서 데이터 읽기
                int unitId = 0;
                if (!string.IsNullOrEmpty(preset.characterId[arrayIndex]))
                {
                    int.TryParse(preset.characterId[arrayIndex], out unitId);
                }

                PlayData.selectedDeckUnitIds[p, s] = unitId;

                // 아이콘 주소 읽기
                if (preset.iconAddress != null && arrayIndex < preset.iconAddress.Length)
                    PlayData.selectedDeckUnitIconAddresses[p, s] = preset.iconAddress[arrayIndex] ?? "";

                arrayIndex++;
            }
        }
    }


    public async UniTask<bool> SavePresetFromPlayDataAsync(int index)
    {
        var preset = GetPreset(index);
        if (preset == null)
        {
            Debug.LogError($"[DatabaseManager SavePresetFromPlayDataAsync] 프리셋 {index} null!");
            return false;
        }

        // 해제된 슬롯 개수 계산
        int unlockedSlots = 0;
        for (int s = 0; s < 5; s++)
        {
            if (IsSlotUnlocked(index, s))
                unlockedSlots++;
        }

        // 배열 크기를 해제된 슬롯 개수로 설정
        preset.characterId = new string[unlockedSlots];
        preset.iconAddress = new string[unlockedSlots];

        // 해제된 슬롯만 저장
        int arrayIndex = 0;
        for (int s = 0; s < 5; s++)
        {
            if (!IsSlotUnlocked(index, s))
                continue;

            int unitId = PlayData.selectedDeckUnitIds[index, s];
            preset.characterId[arrayIndex] = unitId == 0 ? null : unitId.ToString();
            preset.iconAddress[arrayIndex] = PlayData.selectedDeckUnitIconAddresses[index, s];
            arrayIndex++;
        }

        preset.lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        bool success = await SavePresetAsync(index);

        if (!success)
            Debug.LogError($"[DatabaseManager SavePresetFromPlayDataAsync] 프리셋 {index} Firebase 저장 실패!");

        return success;
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
        bool success = await database.SetDataAsync(path, preset);

        // 업적 연동: 덱 5자리 모두 채움
        if (success && IsDeckFull(preset))
            await AchievementManager.CompleteDeckFullAsync();

        return success;
    }

    private bool IsDeckFull(PartyPreset preset)
    {
        if (preset?.characterId == null) return false;

        for (int i = 0; i < preset.characterId.Length; i++)
        {
            if (string.IsNullOrEmpty(preset.characterId[i]))
                return false;
        }
        return true;
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
            bool success = await database.IncrementValueAsync(path, amount);
            if (success)
            {
                CurrentUser.currency.gold += amount;
                PlayData.SetGoldImmediate(CurrentUser.currency.gold);
                PlayData.NotifyCurrencyChanged();
            }

            var (value, ok) = await database.GetDataAsync<object>(path);
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
            PlayData.SetDiamondImmediate(CurrentUser.currency.diamond);
            PlayData.NotifyCurrencyChanged();
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

            // 업적 연동: 강화석 획득 (양수일 때만)
            if (amount > 0)
                await AchievementManager.AddStoneGetAsync(amount);
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

        bool success = await SaveCurrencyAsync();
        if (success)
        {
            PlayData.SetStaminaImmediate(newValue);
            PlayData.NotifyCurrencyChanged();
        }
        return success;
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
        bool success = await SaveCharacterAsync(characterId);

        // 업적 연동: 유닛 수집
        if (success)
        {
            int totalUnitCount = CurrentUser.characters.Count;
            await AchievementManager.UpdateUnitCollectCountAsync(totalUnitCount);
        }

        return success;
    }

    /// <summary>
    /// 캐릭터 추가 - 로컬 캐시만 업데이트 (Firebase 저장 없음)
    /// </summary>
    public bool AddCharacterLocal(string characterId, int star = 1)
    {
        if (CurrentUser.characters.ContainsKey(characterId))
        {
            // 이미 보유 - 조각으로 변환은 호출자가 처리
            return false;
        }

        CurrentUser.characters[characterId] = new OwnedCharacter(characterId, star);
        return true;
    }

    /// <summary>
    /// 캐릭터 추가 - Firebase에만 저장 (로컬 캐시는 이미 업데이트된 상태)
    /// </summary>
    public async UniTask<bool> AddCharacterFirebaseAsync(string characterId, int star = 1)
    {
        bool success = await SaveCharacterAsync(characterId);

        // 업적 연동: 유닛 수집 (백그라운드)
        if (success)
        {
            int totalUnitCount = CurrentUser.characters.Count;
            AchievementManager.UpdateUnitCollectCountAsync(totalUnitCount).Forget();
        }

        return success;
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
    }


    public OwnedCharacter GetCharacter(string characterId)
    {
        if (CurrentUser == null || CurrentUser.characters == null)
            return null;
        return CurrentUser.characters.TryGetValue(characterId, out var character) ? character : null;
    }

    public List<OwnedCharacter> GetAllCharacters()
    {
        if (CurrentUser == null || CurrentUser.characters == null)
            return new List<OwnedCharacter>();
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

    public async UniTask<bool> RecordStageClearAsync(
     string stageId,
     int score,
     float clearTime,
     int starRating)
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

        PlayData.SetLastClearedStageImmediate(stageNumber);
        if (stageNumber > CurrentUser.profile.highestStage)
        {
            CurrentUser.profile.highestStage = stageNumber;
            await SaveProfileAsync();
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

        if (!CurrentUser.partyPresets.TryGetValue(key, out var preset))
        {
            // 프리셋이 없으면 새로 생성 (기본 2개 슬롯)
            preset = new PartyPreset
            {
                name = $"프리셋 {index + 1}",
                characterId = new string[2],
                iconAddress = new string[2],
                isLocked = false,
                lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            CurrentUser.partyPresets[key] = preset;
            Debug.Log($"[DatabaseManager GetPreset] 프리셋 {index} 새로 생성 (2개 슬롯)");
        }

        return preset;
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

    #region 우편함 관리

    /// <summary>
    /// 모든 메일 가져오기
    /// </summary>
    public List<MailData> GetAllMails()
    {
        if (CurrentUser?.mails == null)
            return new List<MailData>();

        var mails = new List<MailData>(CurrentUser.mails.Values);
        // 최신순 정렬
        mails.Sort((a, b) => b.createdAt.CompareTo(a.createdAt));
        return mails;
    }

    /// <summary>
    /// 유효한 메일만 가져오기 (만료되지 않은)
    /// </summary>
    public List<MailData> GetValidMails()
    {
        var mails = GetAllMails();
        mails.RemoveAll(m => m.IsExpired());
        return mails;
    }

    /// <summary>
    /// 읽지 않은 메일 개수
    /// </summary>
    public int GetUnreadMailCount()
    {
        if (CurrentUser?.mails == null) return 0;

        int count = 0;
        foreach (var mail in CurrentUser.mails.Values)
        {
            if (!mail.isRead && !mail.IsExpired())
                count++;
        }
        return count;
    }

    /// <summary>
    /// 수령 가능한 메일 개수
    /// </summary>
    public int GetClaimableMailCount()
    {
        if (CurrentUser?.mails == null) return 0;

        int count = 0;
        foreach (var mail in CurrentUser.mails.Values)
        {
            if (!mail.isClaimed && !mail.IsExpired() && mail.reward != null && mail.reward.HasReward())
                count++;
        }
        return count;
    }

    /// <summary>
    /// 메일 읽음 처리
    /// </summary>
    public async UniTask<bool> MarkMailAsReadAsync(string mailId)
    {
        if (CurrentUser?.mails == null || !CurrentUser.mails.TryGetValue(mailId, out var mail))
            return false;

        if (mail.isRead) return true;

        mail.isRead = true;

        string path = $"users/{UserId}/mails/{mailId}/isRead";
        bool success = await database.SetDataAsync(path, true);

        if (success)
        {
            PlayData.NotifyMailsChanged();
        }

        return success;
    }

    /// <summary>
    /// 메일 보상 수령 (낙관적 업데이트)
    /// </summary>
    public UniTask<bool> ClaimMailRewardAsync(string mailId)
    {
        if (CurrentUser?.mails == null || !CurrentUser.mails.TryGetValue(mailId, out var mail))
        {
            Debug.LogWarning($"[Mail] 메일을 찾을 수 없음: {mailId}");
            return UniTask.FromResult(false);
        }

        if (mail.isClaimed)
        {
            Debug.LogWarning($"[Mail] 이미 수령한 메일: {mailId}");
            return UniTask.FromResult(false);
        }

        if (mail.IsExpired())
        {
            Debug.LogWarning($"[Mail] 만료된 메일: {mailId}");
            return UniTask.FromResult(false);
        }

        var reward = mail.reward;

        // 수령 완료 표시 (즉시)
        mail.isClaimed = true;
        mail.isRead = true;

        if (reward == null || !reward.HasReward())
        {
            Debug.LogWarning($"[Mail] 보상 없는 메일: {mailId}");
            // Firebase 저장 백그라운드
            string claimPath = $"users/{UserId}/mails/{mailId}/isClaimed";
            database.SetDataAsync(claimPath, true).Forget();
            PlayData.NotifyMailsChanged();
            return UniTask.FromResult(true);
        }

        // 로컬 캐시 즉시 업데이트
        if (reward.gold > 0)
            PlayData.SetGoldImmediate(PlayData.Gold + reward.gold);

        if (reward.diamond > 0)
            PlayData.SetDiamondImmediate(PlayData.Diamond + reward.diamond);

        if (reward.stamina > 0)
            PlayData.SetStaminaImmediate(PlayData.Stamina + reward.stamina);

        if (reward.enhanceStone > 0)
            PlayData.SetEnhanceStoneImmediate(PlayData.EnhanceStone + reward.enhanceStone);

        if (reward.items != null && reward.items.Count > 0)
        {
            foreach (var item in reward.items)
            {
                if (item.Key == ENHANCE_STONE_ITEM_ID)
                {
                    PlayData.SetEnhanceStoneImmediate(PlayData.EnhanceStone + item.Value);
                }
                else
                {
                    PlayData.SetItemCountImmediate(item.Key, PlayData.GetItemCount(item.Key) + item.Value);
                }
            }
        }

        PlayData.NotifyMailsChanged();
        PlayData.NotifyCurrencyChanged();

        // Firebase 저장은 백그라운드로 병렬 처리
        var saveTasks = new System.Collections.Generic.List<UniTask>();

        if (reward.gold > 0)
            saveTasks.Add(AddGoldAsync(reward.gold));

        if (reward.diamond > 0)
            saveTasks.Add(AddDiamondAsync(reward.diamond));

        if (reward.stamina > 0)
            saveTasks.Add(AddStaminaAsync(reward.stamina));

        if (reward.enhanceStone > 0)
            saveTasks.Add(AddEnhanceStoneAsync(reward.enhanceStone));

        if (reward.items != null && reward.items.Count > 0)
        {
            foreach (var item in reward.items)
            {
                if (item.Key == ENHANCE_STONE_ITEM_ID)
                {
                    saveTasks.Add(AddEnhanceStoneAsync(item.Value));
                }
                else
                {
                    saveTasks.Add(AddItemAsync(item.Key, item.Value));
                }
            }
        }

        string path = $"users/{UserId}/mails/{mailId}";
        saveTasks.Add(database.SetDataAsync(path, mail));

        // 모든 Firebase 저장을 병렬로 백그라운드 처리
        var saveTask = UniTask.WhenAll(saveTasks);
        PendingSaveManager.Track(saveTask);

        return UniTask.FromResult(true);
    }

    /// <summary>
    /// 모든 메일 일괄 수령 (낙관적 업데이트 - 즉시 반환)
    /// </summary>
    public UniTask<int> ClaimAllMailRewardsAsync()
    {
        var mails = GetValidMails();
        int claimedCount = 0;

        foreach (var mail in mails)
        {
            if (!mail.isClaimed && mail.reward != null && mail.reward.HasReward())
            {
                // ClaimMailRewardAsync가 이제 동기적으로 로컬 캐시 업데이트 후 즉시 반환
                ClaimMailRewardAsync(mail.mailId);
                claimedCount++;
            }
        }

        return UniTask.FromResult(claimedCount);
    }

    /// <summary>
    /// 메일 삭제
    /// </summary>
    public async UniTask<bool> DeleteMailAsync(string mailId)
    {
        if (CurrentUser?.mails == null || !CurrentUser.mails.ContainsKey(mailId))
            return false;

        CurrentUser.mails.Remove(mailId);

        string path = $"users/{UserId}/mails/{mailId}";
        bool success = await database.DeleteDataAsync(path);

        if (success)
        {
            PlayData.NotifyMailsChanged();
        }

        return success;
    }

    /// <summary>
    /// 수령 완료된 메일 일괄 삭제
    /// </summary>
    public async UniTask<int> DeleteClaimedMailsAsync()
    {
        if (CurrentUser?.mails == null) return 0;

        var toDelete = new List<string>();
        foreach (var kvp in CurrentUser.mails)
        {
            if (kvp.Value.isClaimed)
                toDelete.Add(kvp.Key);
        }

        int deletedCount = 0;
        foreach (var mailId in toDelete)
        {
            bool success = await DeleteMailAsync(mailId);
            if (success) deletedCount++;
        }

        return deletedCount;
    }

    /// <summary>
    /// 만료된 메일 정리
    /// </summary>
    public async UniTask<int> CleanExpiredMailsAsync()
    {
        if (CurrentUser?.mails == null) return 0;

        var toDelete = new List<string>();
        foreach (var kvp in CurrentUser.mails)
        {
            if (kvp.Value.IsExpired())
                toDelete.Add(kvp.Key);
        }

        int deletedCount = 0;
        foreach (var mailId in toDelete)
        {
            bool success = await DeleteMailAsync(mailId);
            if (success) deletedCount++;
        }

        return deletedCount;
    }

    /// <summary>
    /// 메일 발송 (개인 우편함에 추가)
    /// </summary>
    public async UniTask<bool> SendMailAsync(string title, string content, MailReward reward, int expireDays = 30)
    {
        if (CurrentUser == null || string.IsNullOrEmpty(UserId))
            return false;

        if (CurrentUser.mails == null)
            CurrentUser.mails = new Dictionary<string, MailData>();

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string mailId = $"mail_{now}_{UnityEngine.Random.Range(1000, 9999)}";

        var mail = new MailData
        {
            mailId = mailId,
            title = title,
            content = content,
            reward = reward,
            isRead = false,
            isClaimed = false,
            createdAt = now,
            expireAt = now + (expireDays * 24L * 60L * 60L * 1000L) // 밀리초 단위
        };

        CurrentUser.mails[mailId] = mail;

        string path = $"users/{UserId}/mails/{mailId}";
        bool success = await database.SetDataAsync(path, mail);

        if (success)
        {
            Debug.Log($"[Mail] 메일 발송 완료: {title}");
            PlayData.NotifyMailsChanged();
        }

        return success;
    }

    /// <summary>
    /// 패키지 내용물을 우편으로 발송
    /// </summary>
    public async UniTask<bool> SendPackageMailAsync(int packageId, int packageAmount = 1)
    {
        var packageData = DataTableManager.PackageTable?.Get(packageId);
        if (packageData == null)
        {
            Debug.LogError($"[Mail] 패키지 데이터를 찾을 수 없음: {packageId}");
            return false;
        }

        // 패키지 이름 가져오기
        string packageName = DataTableManager.GetString(packageData.PACKAGE_NAME) ?? $"패키지 {packageId}";

        // 보상 구성
        var reward = new MailReward();

        for (int i = 0; i < packageAmount; i++)
        {
            // ITEM_ID1
            if (packageData.ITEM_ID1 > 0 && packageData.ITEM1_AMOUNT > 0)
            {
                AddRewardItem(reward, packageData.ITEM_ID1, packageData.ITEM1_AMOUNT);
            }

            // ITEM_ID2
            if (packageData.ITEM_ID2 > 0 && packageData.ITEM2_AMOUNT > 0)
            {
                AddRewardItem(reward, packageData.ITEM_ID2, packageData.ITEM2_AMOUNT);
            }

            // ITEM_ID3
            if (packageData.ITEM_ID3 > 0 && packageData.ITEM3_AMOUNT > 0)
            {
                AddRewardItem(reward, packageData.ITEM_ID3, packageData.ITEM3_AMOUNT);
            }
        }

        // 메일 발송
        string title = packageName;
        string content = "구매하신 패키지 상품입니다.";

        return await SendMailAsync(title, content, reward);
    }

    /// <summary>
    /// 보상에 아이템 추가 (골드/다이아/일반 아이템 분류)
    /// </summary>
    private void AddRewardItem(MailReward reward, int itemId, int amount)
    {
        switch (itemId)
        {
            case 0:
                reward.gold += amount;
                break;
            case 5107:
                reward.diamond += amount;
                break;
            case 5201:
                reward.enhanceStone += amount;
                break;
            default:
                if (reward.items == null)
                    reward.items = new Dictionary<int, int>();

                if (reward.items.ContainsKey(itemId))
                    reward.items[itemId] += amount;
                else
                    reward.items[itemId] = amount;
                break;
        }
    }

    #endregion

    #region 전역 메일 관리

    // 전역 메일 캐시
    private Dictionary<string, GlobalMailData> cachedGlobalMails = new Dictionary<string, GlobalMailData>();
    private bool isGlobalMailsLoaded = false;

    /// <summary>
    /// 전역 메일 로드 (로그인 시 호출)
    /// </summary>
    public async UniTask LoadGlobalMailsAsync()
    {
        string path = "globalMails";
        var (data, success) = await database.GetDataAsync<Dictionary<string, GlobalMailData>>(path);

        if (success && data != null)
        {
            cachedGlobalMails = data;
            Debug.Log($"[GlobalMail] 전역 메일 {cachedGlobalMails.Count}개 로드 완료");
        }
        else
        {
            cachedGlobalMails = new Dictionary<string, GlobalMailData>();
            Debug.Log("[GlobalMail] 전역 메일 없음");
        }

        isGlobalMailsLoaded = true;
    }

    /// <summary>
    /// 전역 메일 목록 가져오기 (유효한 것만)
    /// </summary>
    public List<GlobalMailData> GetValidGlobalMails()
    {
        var mails = new List<GlobalMailData>();

        foreach (var mail in cachedGlobalMails.Values)
        {
            if (!mail.IsExpired())
                mails.Add(mail);
        }

        // 최신순 정렬
        mails.Sort((a, b) => b.createdAt.CompareTo(a.createdAt));
        return mails;
    }

    /// <summary>
    /// 전역 메일 수령 여부 확인
    /// </summary>
    public bool IsGlobalMailClaimed(string mailId)
    {
        if (CurrentUser?.claimedGlobalMails == null) return false;
        return CurrentUser.claimedGlobalMails.ContainsKey(mailId);
    }

    /// <summary>
    /// 수령하지 않은 전역 메일 개수
    /// </summary>
    public int GetUnclaimedGlobalMailCount()
    {
        int count = 0;
        foreach (var mail in cachedGlobalMails.Values)
        {
            if (!mail.IsExpired() && !IsGlobalMailClaimed(mail.mailId))
                count++;
        }
        return count;
    }

    /// <summary>
    /// 전역 메일 보상 수령 (낙관적 업데이트)
    /// </summary>
    public UniTask<bool> ClaimGlobalMailRewardAsync(string mailId)
    {
        if (!cachedGlobalMails.TryGetValue(mailId, out var globalMail))
        {
            Debug.LogWarning($"[GlobalMail] 메일을 찾을 수 없음: {mailId}");
            return UniTask.FromResult(false);
        }

        if (IsGlobalMailClaimed(mailId))
        {
            Debug.LogWarning($"[GlobalMail] 이미 수령한 메일: {mailId}");
            return UniTask.FromResult(false);
        }

        if (globalMail.IsExpired())
        {
            Debug.LogWarning($"[GlobalMail] 만료된 메일: {mailId}");
            return UniTask.FromResult(false);
        }

        // 수령 기록 즉시 저장 (로컬)
        if (CurrentUser.claimedGlobalMails == null)
            CurrentUser.claimedGlobalMails = new Dictionary<string, bool>();

        CurrentUser.claimedGlobalMails[mailId] = true;

        var reward = globalMail.reward;
        if (reward != null && reward.HasReward())
        {
            // 로컬 캐시 즉시 업데이트
            if (reward.gold > 0)
                PlayData.SetGoldImmediate(PlayData.Gold + reward.gold);

            if (reward.diamond > 0)
                PlayData.SetDiamondImmediate(PlayData.Diamond + reward.diamond);

            if (reward.stamina > 0)
                PlayData.SetStaminaImmediate(PlayData.Stamina + reward.stamina);

            if (reward.enhanceStone > 0)
                PlayData.SetEnhanceStoneImmediate(PlayData.EnhanceStone + reward.enhanceStone);

            if (reward.items != null && reward.items.Count > 0)
            {
                foreach (var item in reward.items)
                {
                    if (item.Key == ENHANCE_STONE_ITEM_ID)
                    {
                        PlayData.SetEnhanceStoneImmediate(PlayData.EnhanceStone + item.Value);
                    }
                    else
                    {
                        PlayData.SetItemCountImmediate(item.Key, PlayData.GetItemCount(item.Key) + item.Value);
                    }
                }
            }

            PlayData.NotifyCurrencyChanged();
        }

        Debug.Log($"[GlobalMail] 전역 메일 수령 완료: {mailId}");
        PlayData.NotifyMailsChanged();

        // Firebase 저장은 백그라운드로 병렬 처리
        var saveTasks = new System.Collections.Generic.List<UniTask>();

        if (reward != null && reward.HasReward())
        {
            if (reward.gold > 0)
                saveTasks.Add(AddGoldAsync(reward.gold));

            if (reward.diamond > 0)
                saveTasks.Add(AddDiamondAsync(reward.diamond));

            if (reward.stamina > 0)
                saveTasks.Add(AddStaminaAsync(reward.stamina));

            if (reward.enhanceStone > 0)
                saveTasks.Add(AddEnhanceStoneAsync(reward.enhanceStone));

            if (reward.items != null && reward.items.Count > 0)
            {
                foreach (var item in reward.items)
                {
                    if (item.Key == ENHANCE_STONE_ITEM_ID)
                    {
                        saveTasks.Add(AddEnhanceStoneAsync(item.Value));
                    }
                    else
                    {
                        saveTasks.Add(AddItemAsync(item.Key, item.Value));
                    }
                }
            }
        }

        string path = $"users/{UserId}/claimedGlobalMails/{mailId}";
        saveTasks.Add(database.SetDataAsync(path, true));

        // 모든 Firebase 저장을 병렬로 백그라운드 처리
        var saveTask = UniTask.WhenAll(saveTasks);
        PendingSaveManager.Track(saveTask);

        return UniTask.FromResult(true);
    }

    /// <summary>
    /// 모든 전역 메일 일괄 수령 (낙관적 업데이트 - 즉시 반환)
    /// </summary>
    public UniTask<int> ClaimAllGlobalMailRewardsAsync()
    {
        int claimedCount = 0;

        foreach (var mail in cachedGlobalMails.Values)
        {
            if (!mail.IsExpired() && !IsGlobalMailClaimed(mail.mailId))
            {
                ClaimGlobalMailRewardAsync(mail.mailId);
                claimedCount++;
            }
        }

        return UniTask.FromResult(claimedCount);
    }

    /// <summary>
    /// 전체 메일 가져오기 (개인 + 전역 통합)
    /// </summary>
    public List<MailData> GetAllMailsWithGlobal()
    {
        var allMails = new List<MailData>();

        // 개인 메일 추가
        if (CurrentUser?.mails != null)
        {
            foreach (var mail in CurrentUser.mails.Values)
            {
                if (!mail.IsExpired())
                    allMails.Add(mail);
            }
        }

        // 전역 메일 추가 (MailData 형태로 변환)
        foreach (var globalMail in cachedGlobalMails.Values)
        {
            if (!globalMail.IsExpired())
            {
                bool isClaimed = IsGlobalMailClaimed(globalMail.mailId);
                allMails.Add(globalMail.ToMailData(isClaimed));
            }
        }

        // 최신순 정렬
        allMails.Sort((a, b) => b.createdAt.CompareTo(a.createdAt));
        return allMails;
    }

    /// <summary>
    /// 읽지 않은 메일 개수 (개인 + 전역)
    /// </summary>
    public int GetTotalUnreadMailCount()
    {
        return GetUnreadMailCount() + GetUnclaimedGlobalMailCount();
    }

    /// <summary>
    /// 수령 가능한 메일 개수 (개인 + 전역)
    /// </summary>
    public int GetTotalClaimableMailCount()
    {
        int personalClaimable = GetClaimableMailCount();
        int globalClaimable = 0;

        foreach (var mail in cachedGlobalMails.Values)
        {
            if (!mail.IsExpired() && !IsGlobalMailClaimed(mail.mailId) &&
                mail.reward != null && mail.reward.HasReward())
            {
                globalClaimable++;
            }
        }

        return personalClaimable + globalClaimable;
    }

    /// <summary>
    /// 전역 메일인지 확인
    /// </summary>
    public bool IsGlobalMail(string mailId)
    {
        return cachedGlobalMails.ContainsKey(mailId);
    }
    #endregion
    #region 일일 보상 관리

    /// <summary>
    /// 월별 초기화 체크 및 실행
    /// </summary>
    private void CheckAndResetMonthlyReward()
    {
        if (CurrentUser?.dailyReward == null)
            return;

        string thisMonth = DateTime.Now.ToString("yyyy-MM");

        // 현재 월과 다르면 초기화
        if (CurrentUser.dailyReward.currentMonth != thisMonth)
        {
            Debug.Log($"[DB] 월 변경 감지: {CurrentUser.dailyReward.currentMonth} -> {thisMonth}, 출석 데이터 초기화");

            // UI 표시용 날짜 목록 초기화
            CurrentUser.dailyReward.claimedDates.Clear();
            // lastClaimDate는 유지 (자정 지나면 다시 받을 수 있도록)
            CurrentUser.dailyReward.currentMonth = thisMonth;

            // Firebase에 저장
            SaveDailyRewardAsync().Forget();
        }
    }

    /// <summary>
    /// 오늘 출석 체크했는지 확인
    /// </summary>
    public bool HasClaimedToday()
    {
        if (CurrentUser?.dailyReward == null)
            return false;

        CheckAndResetMonthlyReward();

        string today = DateTime.Now.ToString("yyyy-MM-dd");
        return CurrentUser.dailyReward.lastClaimDate == today;
    }

    /// <summary>
    /// 특정 날짜에 출석 체크했는지 확인
    /// </summary>
    public bool HasClaimedOnDate(DateTime date)
    {
        if (CurrentUser?.dailyReward == null)
            return false;

        CheckAndResetMonthlyReward();

        string dateStr = date.ToString("yyyy-MM-dd");
        return CurrentUser.dailyReward.claimedDates.Contains(dateStr);
    }

    /// <summary>
    /// 오늘 일일 보상 받기
    /// </summary>
    public async UniTask<bool> ClaimDailyRewardAsync()
    {
        CheckAndResetMonthlyReward();

        if (HasClaimedToday())
        {
            Debug.LogWarning("[DB] 오늘 이미 출석 체크함");
            return false;
        }

        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string thisMonth = DateTime.Now.ToString("yyyy-MM");

        // claimedDates에 추가
        if (!CurrentUser.dailyReward.claimedDates.Contains(today))
        {
            CurrentUser.dailyReward.claimedDates.Add(today);
        }

        CurrentUser.dailyReward.lastClaimDate = today;
        CurrentUser.dailyReward.currentMonth = thisMonth;
        CurrentUser.dailyReward.totalClaimCount++;

        bool saved = await SaveDailyRewardAsync();

        // 출석 체크 성공 시 로그인 업적 갱신
        if (saved)
        {
            await AchievementManager.UpdateLoginDaysAsync(CurrentUser.dailyReward.totalClaimCount);
        }

        return saved;
    }

    /// <summary>
    /// 일일 보상 데이터 저장
    /// </summary>
    public async UniTask<bool> SaveDailyRewardAsync()
    {
        string path = $"users/{UserId}/dailyReward";
        return await database.SetDataAsync(path, CurrentUser.dailyReward);
    }

    /// <summary>
    /// 총 출석 일수 조회
    /// </summary>
    public int GetTotalClaimCount()
    {
        return CurrentUser?.dailyReward?.totalClaimCount ?? 0;
    }

    #endregion

    #region 던전 입장권 관리

    /// <summary>
    /// 던전 입장권 자정 체크 및 리셋
    /// </summary>
    public async UniTask CheckAndResetDungeonEntriesAsync(int dungeonId)
    {
        if (CurrentUser?.dungeonEntries == null)
            return;

        string dungeonKey = dungeonId.ToString();
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        // 던전 설정 테이블에서 최대 입장권 개수 가져오기
        var dungeonSetting = DataTableManager.DungeonSettingTable?.Get(dungeonId);
        if (dungeonSetting == null)
        {
            Debug.LogError($"[DB] 던전 설정을 찾을 수 없음: {dungeonId}");
            return;
        }

        int maxEntries = dungeonSetting.Basic_Num_Of_Entries;
        int entryItemId = dungeonSetting.Conditions_Of_Entry_ItemID;

        // 던전 입장권 데이터가 없으면 새로 생성
        if (!CurrentUser.dungeonEntries.ContainsKey(dungeonKey))
        {
            Debug.Log($"[DB] 던전 {dungeonId} 입장권 데이터 최초 생성 - 최대 입장권: {maxEntries}");

            CurrentUser.dungeonEntries[dungeonKey] = new DungeonEntryData(dungeonId, maxEntries);
            await SaveDungeonEntryAsync(dungeonId);

            // 최초 생성 시 입장권 아이템도 최대치로 지급
            if (entryItemId > 0)
            {
                await SetItemCountAsync(entryItemId, maxEntries);
                Debug.Log($"[DB] 던전 입장권 아이템 {entryItemId} 최초 지급: {maxEntries}개");
            }
            return;
        }

        var entryData = CurrentUser.dungeonEntries[dungeonKey];

        // 날짜가 바뀌었으면 입장권을 최대치로 리셋
        if (entryData.lastResetDate != today)
        {
            Debug.Log($"[DB] 던전 {dungeonId} 입장권 리셋: {entryData.lastResetDate} -> {today}");

            entryData.currentEntries = maxEntries;
            entryData.lastResetDate = today;

            await SaveDungeonEntryAsync(dungeonId);

            // 입장권 아이템도 최대치로 설정
            if (entryItemId > 0)
            {
                // 현재 아이템 개수 확인
                int currentItemCount = GetItemCount(entryItemId);

                // 최대치보다 적으면 최대치로 설정
                if (currentItemCount < maxEntries)
                {
                    await SetItemCountAsync(entryItemId, maxEntries);
                    Debug.Log($"[DB] 던전 입장권 아이템 {entryItemId} 리셋: {currentItemCount} -> {maxEntries}");
                }
            }
        }
    }

    /// <summary>
    /// 던전 입장권 개수 조회
    /// </summary>
    public int GetDungeonEntries(int dungeonId)
    {
        if (CurrentUser?.dungeonEntries == null)
            return 0;

        string dungeonKey = dungeonId.ToString();

        if (!CurrentUser.dungeonEntries.ContainsKey(dungeonKey))
        {
            // 던전 설정에서 기본 입장권 개수 가져오기
            var dungeonSetting = DataTableManager.DungeonSettingTable?.Get(dungeonId);
            if (dungeonSetting == null)
                return 0;

            return dungeonSetting.Basic_Num_Of_Entries;
        }

        return CurrentUser.dungeonEntries[dungeonKey].currentEntries;
    }

    /// <summary>
    /// 던전 입장권 충분한지 확인
    /// </summary>
    public bool HasEnoughDungeonEntries(int dungeonId, int amount = 1)
    {
        return GetDungeonEntries(dungeonId) >= amount;
    }

    /// <summary>
    /// 던전 입장권 소모
    /// </summary>
    public async UniTask<bool> ConsumeDungeonEntryAsync(int dungeonId, int amount = 1)
    {
        // 자정 체크 및 리셋
        await CheckAndResetDungeonEntriesAsync(dungeonId);

        // 입장권 아이템 기준으로 체크 (구매한 열쇠도 포함)
        var dungeonSetting = DataTableManager.DungeonSettingTable?.Get(dungeonId);
        int entryItemId = dungeonSetting?.Conditions_Of_Entry_ItemID ?? 0;

        if (entryItemId <= 0 || !HasEnoughItem(entryItemId, amount))
        {
            Debug.LogWarning($"[DB] 던전 입장권 부족: {dungeonId}, 아이템ID: {entryItemId}");
            return false;
        }

        string dungeonKey = dungeonId.ToString();

        // dungeonEntries 데이터가 있으면 같이 차감 (선택적)
        if (CurrentUser.dungeonEntries != null && CurrentUser.dungeonEntries.ContainsKey(dungeonKey))
        {
            var entryData = CurrentUser.dungeonEntries[dungeonKey];
            entryData.currentEntries -= amount;
            await SaveDungeonEntryAsync(dungeonId);
            Debug.Log($"[DB] 던전 {dungeonId} dungeonEntries 소모: {amount}개, 남은 개수: {entryData.currentEntries}");
        }

        // 입장권 아이템 소모 (핵심)
        bool itemConsumed = await ConsumeItemAsync(entryItemId, amount);

        if (itemConsumed)
        {
            Debug.Log($"[DB] 던전 {dungeonId} 입장권 아이템 {entryItemId} 소모: {amount}개");
        }

        return itemConsumed;
    }

    /// <summary>
    /// 던전 입장권 데이터 저장
    /// </summary>
    private async UniTask<bool> SaveDungeonEntryAsync(int dungeonId)
    {
        if (CurrentUser == null || string.IsNullOrEmpty(UserId))
            return false;

        string dungeonKey = dungeonId.ToString();
        if (!CurrentUser.dungeonEntries.ContainsKey(dungeonKey))
            return false;

        string path = $"users/{UserId}/dungeonEntries/{dungeonKey}";
        return await database.SetDataAsync(path, CurrentUser.dungeonEntries[dungeonKey]);
    }

    /// <summary>
    /// 아이템 개수 설정 (덮어쓰기)
    /// </summary>
    private async UniTask<bool> SetItemCountAsync(int itemId, int count)
    {
        if (CurrentUser == null || string.IsNullOrEmpty(UserId))
            return false;

        string itemKey = itemId.ToString();
        CurrentUser.items[itemKey] = count;

        string path = $"users/{UserId}/items/{itemKey}";
        return await database.SetDataAsync(path, count);
    }

    /// <summary>
    /// 아이템 소모
    /// </summary>
    private async UniTask<bool> ConsumeItemAsync(int itemId, int amount)
    {
        if (!HasEnoughItem(itemId, amount))
        {
            Debug.LogWarning($"[DB] 아이템 부족: {itemId}, 필요: {amount}");
            return false;
        }

        string itemKey = itemId.ToString();
        int currentCount = GetItemCount(itemId);
        int newCount = currentCount - amount;

        CurrentUser.items[itemKey] = newCount;

        string path = $"users/{UserId}/items/{itemKey}";
        return await database.SetDataAsync(path, newCount);
    }

    #endregion

    #region 던전 스테이지 해금

    /// <summary>
    /// 최고 클리어 던전 스테이지 조회
    /// </summary>
    public int GetHighestDungeonStage()
    {
        return CurrentUser?.profile?.highestDungeonStage ?? 0;
    }

    /// <summary>
    /// 던전 스테이지가 해금되었는지 확인
    /// </summary>
    public bool IsDungeonStageUnlocked(int stage)
    {
        // 스테이지 1은 항상 해금
        if (stage <= 1)
            return true;

        // 최고 클리어 스테이지 + 1까지 해금
        int highestStage = GetHighestDungeonStage();
        return stage <= highestStage + 1;
    }

    /// <summary>
    /// 던전 스테이지 클리어 시 최고 스테이지 업데이트
    /// </summary>
    public async UniTask<bool> UpdateHighestDungeonStageAsync(int clearedStage)
    {
        if (CurrentUser?.profile == null)
            return false;

        int currentHighest = CurrentUser.profile.highestDungeonStage;

        // 현재 최고 기록보다 높으면 업데이트
        if (clearedStage > currentHighest)
        {
            CurrentUser.profile.highestDungeonStage = clearedStage;

            string path = $"users/{UserId}/profile/highestDungeonStage";
            bool success = await database.SetDataAsync(path, clearedStage);

            if (success)
            {
                Debug.Log($"[DB] 최고 던전 스테이지 업데이트: {currentHighest} -> {clearedStage}");
            }

            return success;
        }

        return true; // 업데이트 필요 없음
    }

    #endregion

    #region 업적 관리

    /// <summary>
    /// 업적 진행도 저장
    /// </summary>
    public async UniTask<bool> SaveAchievementProgressAsync(int achievementId, AchievementProgress progress)
    {
        if (CurrentUser == null || string.IsNullOrEmpty(UserId))
            return false;

        string key = achievementId.ToString();

        // 로컬 캐시 업데이트
        if (CurrentUser.achievements == null)
            CurrentUser.achievements = new Dictionary<string, AchievementProgress>();

        CurrentUser.achievements[key] = progress;

        // Firebase 저장
        string path = $"users/{UserId}/achievements/{key}";
        bool success = await database.SetDataAsync(path, progress);

        if (success)
        {
            PlayData.NotifyAchievementsChanged();
        }

        return success;
    }

    /// <summary>
    /// 업적 진행도 조회
    /// </summary>
    public AchievementProgress GetAchievementProgress(int achievementId)
    {
        if (CurrentUser?.achievements == null) return null;

        string key = achievementId.ToString();
        return CurrentUser.achievements.TryGetValue(key, out var progress) ? progress : null;
    }

    /// <summary>
    /// 모든 업적 진행도 조회
    /// </summary>
    public List<AchievementProgress> GetAllAchievementProgress()
    {
        if (CurrentUser?.achievements == null)
            return new List<AchievementProgress>();

        return new List<AchievementProgress>(CurrentUser.achievements.Values);
    }

    /// <summary>
    /// 수령 가능한 업적 개수
    /// </summary>
    public int GetClaimableAchievementCount()
    {
        if (CurrentUser?.achievements == null) return 0;

        int count = 0;
        foreach (var progress in CurrentUser.achievements.Values)
        {
            if (progress.isCompleted && !progress.isRewarded)
                count++;
        }
        return count;
    }

    /// <summary>
    /// 완료된 업적 개수
    /// </summary>
    public int GetCompletedAchievementCount()
    {
        if (CurrentUser?.achievements == null) return 0;

        int count = 0;
        foreach (var progress in CurrentUser.achievements.Values)
        {
            if (progress.isCompleted)
                count++;
        }
        return count;
    }

    #endregion

    #region 퀘스트 관리

    /// <summary>
    /// 퀘스트 진행도 저장
    /// </summary>
    public async UniTask<bool> SaveQuestProgressAsync(int questId, QuestProgress progress)
    {
        if (CurrentUser == null || string.IsNullOrEmpty(UserId))
            return false;

        string key = questId.ToString();

        // 로컬 캐시 업데이트
        if (CurrentUser.quests == null)
            CurrentUser.quests = new Dictionary<string, QuestProgress>();

        CurrentUser.quests[key] = progress;

        // Firebase 저장
        string path = $"users/{UserId}/quests/{key}";
        bool success = await database.SetDataAsync(path, progress);

        if (success)
        {
            PlayData.NotifyQuestsChanged();
        }

        return success;
    }

    /// <summary>
    /// 퀘스트 진행도 조회
    /// </summary>
    public QuestProgress GetQuestProgress(int questId)
    {
        if (CurrentUser?.quests == null) return null;

        string key = questId.ToString();
        return CurrentUser.quests.TryGetValue(key, out var progress) ? progress : null;
    }

    /// <summary>
    /// 모든 퀘스트 진행도 조회
    /// </summary>
    public List<QuestProgress> GetAllQuestProgress()
    {
        if (CurrentUser?.quests == null)
            return new List<QuestProgress>();

        return new List<QuestProgress>(CurrentUser.quests.Values);
    }

    /// <summary>
    /// 수령 가능한 퀘스트 개수
    /// </summary>
    public int GetClaimableQuestCount()
    {
        if (CurrentUser?.quests == null) return 0;

        int count = 0;
        foreach (var progress in CurrentUser.quests.Values)
        {
            if (progress.isCompleted && !progress.isRewarded)
                count++;
        }
        return count;
    }

    /// <summary>
    /// 완료된 퀘스트 개수
    /// </summary>
    public int GetCompletedQuestCount()
    {
        if (CurrentUser?.quests == null) return 0;

        int count = 0;
        foreach (var progress in CurrentUser.quests.Values)
        {
            if (progress.isCompleted)
                count++;
        }
        return count;
    }

    #endregion

    #region 튜토리얼 관리

    /// <summary>
    /// 튜토리얼 진행 상황 저장
    /// </summary>
    public async UniTask<bool> SaveTutorialProgressAsync()
    {
        if (CurrentUser == null || string.IsNullOrEmpty(UserId))
            return false;

        if (CurrentUser.tutorial == null)
            CurrentUser.tutorial = new TutorialProgressData();

        string path = $"users/{UserId}/tutorial";
        bool success = await database.SetDataAsync(path, CurrentUser.tutorial);

        return success;
    }

    /// <summary>
    /// 튜토리얼 시퀀스 시작
    /// </summary>
    public async UniTask<bool> StartTutorialSequenceAsync(string sequenceId)
    {
        if (CurrentUser?.tutorial == null)
            CurrentUser.tutorial = new TutorialProgressData();

        CurrentUser.tutorial.currentSequenceId = sequenceId;
        CurrentUser.tutorial.currentStepIndex = 0;
        CurrentUser.tutorial.lastCheckpointIndex = 0;

        return await SaveTutorialProgressAsync();
    }

    /// <summary>
    /// 튜토리얼 스텝 진행 (체크포인트일 때만 저장)
    /// </summary>
    public async UniTask<bool> UpdateTutorialStepAsync(int stepIndex, bool isCheckpoint)
    {
        if (CurrentUser?.tutorial == null)
            return false;

        CurrentUser.tutorial.currentStepIndex = stepIndex;

        if (isCheckpoint)
        {
            CurrentUser.tutorial.lastCheckpointIndex = stepIndex;
            return await SaveTutorialProgressAsync();
        }

        return true;
    }

    /// <summary>
    /// 튜토리얼 시퀀스 완료
    /// </summary>
    public async UniTask<bool> CompleteTutorialSequenceAsync(string sequenceId)
    {
        if (CurrentUser?.tutorial == null)
            CurrentUser.tutorial = new TutorialProgressData();

        if (CurrentUser.tutorial.completedSequences == null)
            CurrentUser.tutorial.completedSequences = new List<string>();

        if (!CurrentUser.tutorial.completedSequences.Contains(sequenceId))
        {
            CurrentUser.tutorial.completedSequences.Add(sequenceId);
        }

        // 현재 진행 중인 시퀀스 초기화
        CurrentUser.tutorial.currentSequenceId = null;
        CurrentUser.tutorial.currentStepIndex = 0;
        CurrentUser.tutorial.lastCheckpointIndex = 0;

        return await SaveTutorialProgressAsync();
    }

    /// <summary>
    /// 튜토리얼 시퀀스 취소 (완료 처리하지 않고 진행 상태만 초기화)
    /// </summary>
    public async UniTask<bool> CancelTutorialSequenceAsync(string sequenceId)
    {
        if (CurrentUser?.tutorial == null)
            return false;

        // 현재 진행 중인 시퀀스인 경우에만 초기화
        if (CurrentUser.tutorial.currentSequenceId == sequenceId)
        {
            CurrentUser.tutorial.currentSequenceId = null;
            CurrentUser.tutorial.currentStepIndex = 0;
            CurrentUser.tutorial.lastCheckpointIndex = 0;

            return await SaveTutorialProgressAsync();
        }

        return true;
    }

    /// <summary>
    /// 튜토리얼 시퀀스 완료 상태 취소 (완료 목록에서 제거)
    /// </summary>
    public async UniTask<bool> UncompleteSequenceAsync(string sequenceId)
    {
        if (CurrentUser?.tutorial == null)
            return false;

        if (CurrentUser.tutorial.completedSequences != null &&
            CurrentUser.tutorial.completedSequences.Contains(sequenceId))
        {
            CurrentUser.tutorial.completedSequences.Remove(sequenceId);
            Debug.Log($"[DatabaseManager] 튜토리얼 시퀀스 완료 취소: {sequenceId}");
            return await SaveTutorialProgressAsync();
        }

        return true;
    }

    /// <summary>
    /// 전체 튜토리얼 완료 처리
    /// </summary>
    public async UniTask<bool> CompleteTutorialAsync()
    {
        if (CurrentUser?.tutorial == null)
            CurrentUser.tutorial = new TutorialProgressData();

        CurrentUser.tutorial.isCompleted = true;

        // 업적 연동
        await AchievementManager.CompleteTutorialAsync();

        return await SaveTutorialProgressAsync();
    }

    /// <summary>
    /// 튜토리얼 진행 상황 가져오기
    /// </summary>
    public TutorialProgressData GetTutorialProgress()
    {
        if (CurrentUser?.tutorial == null)
            CurrentUser.tutorial = new TutorialProgressData();

        return CurrentUser.tutorial;
    }

    /// <summary>
    /// 특정 시퀀스가 완료되었는지 확인
    /// </summary>
    public bool IsTutorialSequenceCompleted(string sequenceId)
    {
        return CurrentUser?.tutorial?.IsSequenceCompleted(sequenceId) ?? false;
    }

    /// <summary>
    /// 전체 튜토리얼이 완료되었는지 확인
    /// </summary>
    public bool IsTutorialCompleted()
    {
        return CurrentUser?.tutorial?.isCompleted ?? false;
    }

    /// <summary>
    /// 튜토리얼 진행 상황 초기화 (디버그용)
    /// </summary>
    public async UniTask<bool> ResetTutorialProgressAsync()
    {
        if (CurrentUser == null)
            return false;

        CurrentUser.tutorial = new TutorialProgressData();
        return await SaveTutorialProgressAsync();
    }

    #endregion

    #region 프리셋 슬롯 잠금 관리

    /// <summary>
    /// 슬롯 잠금 해제 데이터 저장
    /// </summary>
    public async UniTask<bool> SaveSlotUnlocksAsync()
    {
        if (CurrentUser == null || string.IsNullOrEmpty(UserId))
            return false;

        if (CurrentUser.presetSlotUnlocks == null)
            CurrentUser.presetSlotUnlocks = new PresetSlotUnlockData();

        string path = $"users/{UserId}/presetSlotUnlocks";
        bool success = await database.SetDataAsync(path, CurrentUser.presetSlotUnlocks);

        if (success)
        {
            Debug.Log("[DB] 슬롯 잠금 해제 데이터 저장 완료");
        }

        return success;
    }

    /// <summary>
    /// 특정 슬롯이 해제되었는지 확인
    /// </summary>
    public bool IsSlotUnlocked(int presetIndex, int slotIndex)
    {
        if (CurrentUser?.presetSlotUnlocks == null)
            return slotIndex < 2; // 0, 1번 슬롯은 항상 해제

        return CurrentUser.presetSlotUnlocks.IsSlotUnlocked(presetIndex, slotIndex);
    }

    #endregion

    #region 상점 구매 기록 관리

    /// <summary>
    /// 구매 기록 가져오기 (없으면 생성)
    /// </summary>
    public PurchaseRecordData GetPurchaseRecord(int sellingId)
    {
        if (CurrentUser?.purchaseRecords == null)
            CurrentUser.purchaseRecords = new Dictionary<string, PurchaseRecordData>();

        string key = sellingId.ToString();
        if (!CurrentUser.purchaseRecords.TryGetValue(key, out var record))
        {
            record = new PurchaseRecordData(sellingId);
            CurrentUser.purchaseRecords[key] = record;
        }

        return record;
    }

    /// <summary>
    /// 전체 구매 횟수 조회
    /// </summary>
    public int GetTotalPurchaseCount(int sellingId)
    {
        var record = GetPurchaseRecord(sellingId);
        return record.totalCount;
    }

    /// <summary>
    /// 오늘 구매 횟수 조회
    /// </summary>
    public int GetTodayPurchaseCount(int sellingId)
    {
        var record = GetPurchaseRecord(sellingId);
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        // 날짜가 바뀌었으면 리셋
        if (record.lastPurchaseDate != today)
        {
            record.dailyCount = 0;
        }

        return record.dailyCount;
    }

    /// <summary>
    /// 이번 주 구매 횟수 조회
    /// </summary>
    public int GetWeeklyPurchaseCount(int sellingId)
    {
        var record = GetPurchaseRecord(sellingId);
        string currentWeekStart = GetWeekStartDate(DateTime.Now);

        // 주가 바뀌었으면 리셋
        if (record.weekStartDate != currentWeekStart)
        {
            record.weeklyCount = 0;
            record.weekStartDate = currentWeekStart;
        }

        return record.weeklyCount;
    }

    /// <summary>
    /// 이번 달 구매 횟수 조회
    /// </summary>
    public int GetMonthlyPurchaseCount(int sellingId)
    {
        var record = GetPurchaseRecord(sellingId);
        string currentMonth = DateTime.Now.ToString("yyyy-MM");

        // 월이 바뀌었으면 리셋
        if (record.monthKey != currentMonth)
        {
            record.monthlyCount = 0;
            record.monthKey = currentMonth;
        }

        return record.monthlyCount;
    }

    /// <summary>
    /// 구매 기록 추가 (로컬 캐시만 - 낙관적 업데이트용)
    /// </summary>
    public void AddPurchaseRecordLocal(int sellingId)
    {
        if (CurrentUser == null)
            return;

        var record = GetPurchaseRecord(sellingId);
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string currentWeekStart = GetWeekStartDate(DateTime.Now);
        string currentMonth = DateTime.Now.ToString("yyyy-MM");

        // 날짜 변경 체크 및 리셋
        if (record.lastPurchaseDate != today)
        {
            record.dailyCount = 0;
        }
        if (record.weekStartDate != currentWeekStart)
        {
            record.weeklyCount = 0;
            record.weekStartDate = currentWeekStart;
        }
        if (record.monthKey != currentMonth)
        {
            record.monthlyCount = 0;
            record.monthKey = currentMonth;
        }

        // 카운트 증가
        record.totalCount++;
        record.dailyCount++;
        record.weeklyCount++;
        record.monthlyCount++;
        record.lastPurchaseDate = today;

        Debug.Log($"[DB] 구매 기록 로컬 저장: {sellingId}, 전체: {record.totalCount}, 오늘: {record.dailyCount}");
    }

    /// <summary>
    /// 구매 기록 추가 (Firebase 저장)
    /// </summary>
    public async UniTask<bool> AddPurchaseRecordAsync(int sellingId)
    {
        if (CurrentUser == null || string.IsNullOrEmpty(UserId))
            return false;

        var record = GetPurchaseRecord(sellingId);

        // Firebase 저장
        string key = sellingId.ToString();
        string path = $"users/{UserId}/purchaseRecords/{key}";
        bool success = await database.SetDataAsync(path, record);

        if (success)
        {
            Debug.Log($"[DB] 구매 기록 Firebase 저장: {sellingId}");
        }

        return success;
    }

    /// <summary>
    /// 주의 시작 날짜 계산 (월요일 기준)
    /// </summary>
    private string GetWeekStartDate(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime monday = date.AddDays(-diff).Date;
        return monday.ToString("yyyy-MM-dd");
    }

    #endregion
}
