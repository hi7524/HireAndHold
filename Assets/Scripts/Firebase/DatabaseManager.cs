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

            // 로그인 일수 계산 및 업적 연동
            long lastLogin = CurrentUser.profile.lastLoginTime;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long createdAt = CurrentUser.profile.createdAt;

            // 계정 생성 후 경과 일수 (1일차부터 시작)
            int daysSinceCreated = (int)((now - createdAt) / 86400) + 1;
            await AchievementManager.UpdateLoginDaysAsync(daysSinceCreated);

            // 마지막 로그인 시간 갱신
            CurrentUser.profile.lastLoginTime = now;
            await SaveProfileAsync();
        }
        else
        {
            CurrentUser = CreateNewUserData();
            bool saveResult = await SaveAllAsync();
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
        bool success = await SaveCharacterAsync(characterId);

        // 업적 연동: 유닛 수집
        if (success)
        {
            int totalUnitCount = CurrentUser.characters.Count;
            await AchievementManager.UpdateUnitCollectCountAsync(totalUnitCount);
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
    /// 메일 보상 수령
    /// </summary>
    public async UniTask<bool> ClaimMailRewardAsync(string mailId)
    {
        if (CurrentUser?.mails == null || !CurrentUser.mails.TryGetValue(mailId, out var mail))
        {
            Debug.LogWarning($"[Mail] 메일을 찾을 수 없음: {mailId}");
            return false;
        }

        if (mail.isClaimed)
        {
            Debug.LogWarning($"[Mail] 이미 수령한 메일: {mailId}");
            return false;
        }

        if (mail.IsExpired())
        {
            Debug.LogWarning($"[Mail] 만료된 메일: {mailId}");
            return false;
        }

        var reward = mail.reward;
        if (reward == null || !reward.HasReward())
        {
            Debug.LogWarning($"[Mail] 보상 없는 메일: {mailId}");
            // 보상이 없어도 수령 처리
            mail.isClaimed = true;
            string claimPath = $"users/{UserId}/mails/{mailId}/isClaimed";
            return await database.SetDataAsync(claimPath, true);
        }

        // 보상 지급
        if (reward.gold > 0)
            await AddGoldAsync(reward.gold);

        if (reward.diamond > 0)
            await AddDiamondAsync(reward.diamond);

        if (reward.stamina > 0)
            await AddStaminaAsync(reward.stamina);

        if (reward.enhanceStone > 0)
            await AddEnhanceStoneAsync(reward.enhanceStone);

        if (reward.items != null && reward.items.Count > 0)
        {
            foreach (var item in reward.items)
            {
                // 강화석(5201)은 currency.enhanceStone으로 처리
                if (item.Key == ENHANCE_STONE_ITEM_ID)
                {
                    await AddEnhanceStoneAsync(item.Value);
                }
                else
                {
                    await AddItemAsync(item.Key, item.Value);
                }
            }
        }

        // 수령 완료 표시
        mail.isClaimed = true;
        mail.isRead = true;

        string path = $"users/{UserId}/mails/{mailId}";
        bool success = await database.SetDataAsync(path, mail);

        if (success)
        {
            PlayData.NotifyMailsChanged();
        }

        return success;
    }

    /// <summary>
    /// 모든 메일 일괄 수령
    /// </summary>
    public async UniTask<int> ClaimAllMailRewardsAsync()
    {
        var mails = GetValidMails();
        int claimedCount = 0;

        foreach (var mail in mails)
        {
            if (!mail.isClaimed && mail.reward != null && mail.reward.HasReward())
            {
                bool success = await ClaimMailRewardAsync(mail.mailId);
                if (success) claimedCount++;
            }
        }

        return claimedCount;
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
    /// 전역 메일 보상 수령
    /// </summary>
    public async UniTask<bool> ClaimGlobalMailRewardAsync(string mailId)
    {
        if (!cachedGlobalMails.TryGetValue(mailId, out var globalMail))
        {
            Debug.LogWarning($"[GlobalMail] 메일을 찾을 수 없음: {mailId}");
            return false;
        }

        if (IsGlobalMailClaimed(mailId))
        {
            Debug.LogWarning($"[GlobalMail] 이미 수령한 메일: {mailId}");
            return false;
        }

        if (globalMail.IsExpired())
        {
            Debug.LogWarning($"[GlobalMail] 만료된 메일: {mailId}");
            return false;
        }

        var reward = globalMail.reward;
        if (reward != null && reward.HasReward())
        {
            // 보상 지급
            if (reward.gold > 0)
                await AddGoldAsync(reward.gold);

            if (reward.diamond > 0)
                await AddDiamondAsync(reward.diamond);

            if (reward.stamina > 0)
                await AddStaminaAsync(reward.stamina);

            if (reward.enhanceStone > 0)
                await AddEnhanceStoneAsync(reward.enhanceStone);

            if (reward.items != null && reward.items.Count > 0)
            {
                foreach (var item in reward.items)
                {
                    // 강화석(5201)은 currency.enhanceStone으로 처리
                    if (item.Key == ENHANCE_STONE_ITEM_ID)
                    {
                        await AddEnhanceStoneAsync(item.Value);
                    }
                    else
                    {
                        await AddItemAsync(item.Key, item.Value);
                    }
                }
            }
        }

        // 수령 기록 저장
        if (CurrentUser.claimedGlobalMails == null)
            CurrentUser.claimedGlobalMails = new Dictionary<string, bool>();

        CurrentUser.claimedGlobalMails[mailId] = true;

        string path = $"users/{UserId}/claimedGlobalMails/{mailId}";
        bool success = await database.SetDataAsync(path, true);

        if (success)
        {
            Debug.Log($"[GlobalMail] 전역 메일 수령 완료: {mailId}");
            PlayData.NotifyMailsChanged();
        }

        return success;
    }

    /// <summary>
    /// 모든 전역 메일 일괄 수령
    /// </summary>
    public async UniTask<int> ClaimAllGlobalMailRewardsAsync()
    {
        int claimedCount = 0;

        foreach (var mail in cachedGlobalMails.Values)
        {
            if (!mail.IsExpired() && !IsGlobalMailClaimed(mail.mailId))
            {
                bool success = await ClaimGlobalMailRewardAsync(mail.mailId);
                if (success) claimedCount++;
            }
        }

        return claimedCount;
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

        return await SaveDailyRewardAsync();
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
}
