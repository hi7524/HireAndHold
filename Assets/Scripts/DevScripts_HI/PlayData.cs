using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CachedCharacter
{
    public string id;
    public int star;
    public int level;
    public int exp;
    public int awakening;

    public int enforceLevel;
    public int heroEnforceLevel;
}

public static class PlayData
{
    //public static Dictionary<int, float> unitFragments = new();

    public static HashSet<int> selectedUnitIds = new HashSet<int> { 11119, 11104, 11122, 11128, 11113 };

    //편성된 덱 배열

    public static int currentSelectedPreset = 0;

    public static int[,] selectedDeckUnitIds = new int[5, 5];
    public static string[,] selectedDeckUnitIconAddresses = new string[5, 5];

    //재화

    private static long cachedGold = 0;
    private static int cachedDiamond = 0;
    private static int cachedStamina = 0;
    private static int cachedEnhanceStone = 0;

    //유저 프로필 캐싱
    private static int cachedLevel = 1;
    private static int cachedExp = 0;
    private static string cachedNickname = "";


    //캐릭터 데이터 캐싱
    private static Dictionary<string, CachedCharacter> cachedCharacters = new Dictionary<string, CachedCharacter>();

    // 아이템 캐시
    private static Dictionary<int, int> cachedItems = new Dictionary<int, int>();

    // 던전 입장권 캐시
    private static Dictionary<int, int> cachedDungeonEntries = new Dictionary<int, int>();

    //초기화 플래그
    private static bool isInitialized = false;

    //재화 프로퍼티
    public static long Gold => cachedGold;
    public static int Diamond => cachedDiamond;
    public static int Stamina => cachedStamina;
    public static int EnhanceStone => cachedEnhanceStone;

    // 유저 프로필 프로퍼티
    public static int Level => cachedLevel;
    public static int Exp => cachedExp;
    public static string Nickname => cachedNickname;

    private static string cachedProfileIconAddress;
    public static string ProfileIconAddress => cachedProfileIconAddress;


    //초기화 상태
    public static bool IsInitialized => isInitialized;

    // 던전 선택 정보 (ID)
    public static int OreDungeonID { get; private set; }
    public static event Action OnProfileChanged;

    public static event Action OnCurrencyChanged;
    public static event Action OnMailsChanged;
    public static event Action OnAchievementsChanged;
    public static event Action OnQuestsChanged;
    public static event Action<string> OnCharacterUpdated;

    public static int LastClearedStageId { get; private set; }

    public static int LastClearedStageNumber
    {
        get
        {
            const int STAGE_ID_START = 701;
            return Mathf.Max(1, LastClearedStageId - STAGE_ID_START + 1);
        }
    }



    public static void NotifyCurrencyChanged()
    {
        OnCurrencyChanged?.Invoke();
    }
    public static void SetLastClearedStageImmediate(int stageId)
    {
        LastClearedStageId = stageId;
        OnProfileChanged?.Invoke();
    }

    //DatabaseManager에서 데이터 동기화
    public static void SyncFromDatabase()
    {
        if (DatabaseManager.Instance?.CurrentUser == null)
        {
            Debug.LogWarning("DatabaseManager 또는 CurrentUser가 null입니다.");
            return;
        }

        var user = DatabaseManager.Instance.CurrentUser;


        cachedGold = user.currency.gold;
        cachedDiamond = user.currency.diamond;
        cachedStamina = user.currency.stamina;
        cachedEnhanceStone = user.currency.enhanceStone;
        LastClearedStageId = user.profile.highestStage;

        cachedLevel = user.profile.level;
        cachedExp = user.profile.exp;
        cachedNickname = user.profile.nickname;


        SyncCharactersFromDatabase();

        // 아이템 동기화
        SyncItemsFromDatabase();
        SyncSelectedUnitIdsFromActivePreset();
        cachedProfileIconAddress = user.profile.profileIconAddress;


        isInitialized = true;
        OnProfileChanged?.Invoke();
    }

    private static void SyncSelectedUnitIdsFromActivePreset()
    {
        selectedUnitIds.Clear();

        int activePreset = currentSelectedPreset;

        for (int i = 0; i < 5; i++)
        {
            int unitId = selectedDeckUnitIds[activePreset, i];

            if (unitId != 0)
            {
                selectedUnitIds.Add(unitId);
            }
        }
    }

    // 캐릭터 데이터 동기화
    public static void SyncCharactersFromDatabase()
    {
        cachedCharacters.Clear();

        var characters = DatabaseManager.Instance.GetAllCharacters();
        foreach (var character in characters)
        {
            cachedCharacters[character.id] = new CachedCharacter
            {
                id = character.id,
                star = character.star,
                level = character.level,
                exp = character.exp,
                awakening = character.awakening,
                enforceLevel = character.enforceLevel,
                heroEnforceLevel = character.heroEnforceLevel
            };
        }
    }

    public static void SetNicknameImmediate(string nickname)
    {
        cachedNickname = nickname;
        OnProfileChanged?.Invoke();
    }

    public static void SetProfileIconImmediate(string address)
    {
        cachedProfileIconAddress = address;
        OnProfileChanged?.Invoke();
    }


    // 아이템 데이터 동기화
    public static void SyncItemsFromDatabase()
    {
        cachedItems.Clear();

        var user = DatabaseManager.Instance.CurrentUser;
        if (user?.items == null) return;

        foreach (var kvp in user.items)
        {
            if (int.TryParse(kvp.Key, out int itemId))
            {
                cachedItems[itemId] = kvp.Value;
            }
        }
    }

    //재화 변경 로컬 캐시 + DB 동기화

    public static async void AddStamina(int amount)
    {
        cachedStamina += amount;
        await DatabaseManager.Instance.AddStaminaAsync(amount);
    }


    //재화 즉시 설정 (동기화 없이) 
    public static void SetGoldImmediate(long value)
    {
        cachedGold = value;
    }

    public static void SetEnhanceStoneImmediate(int value)
    {
        cachedEnhanceStone = value;
    }

    public static void SetDiamondImmediate(int value)
    {
        cachedDiamond = value;
    }

    public static void SetStaminaImmediate(int value)
    {
        cachedStamina = value;
    }

    // 재화 체크
    public static bool HasEnoughGold(long amount)
    {
        return cachedGold >= amount;
    }

    public static bool HasEnoughDiamond(int amount)
    {
        return cachedDiamond >= amount;
    }

    public static bool HasEnoughEnhanceStone(int amount)
    {
        return cachedEnhanceStone >= amount;
    }

    // 아이템 관련
    public static int GetItemCount(int itemId)
    {
        return cachedItems.TryGetValue(itemId, out int count) ? count : 0;
    }

    public static bool HasEnoughItem(int itemId, int amount)
    {
        return GetItemCount(itemId) >= amount;
    }

    public static void SetItemCountImmediate(int itemId, int count)
    {
        if (count <= 0)
        {
            cachedItems.Remove(itemId);
        }
        else
        {
            cachedItems[itemId] = count;
        }
    }

    // 던전 입장권 관련
    public static void SyncDungeonEntriesFromDatabase()
    {
        cachedDungeonEntries.Clear();

        var user = DatabaseManager.Instance.CurrentUser;
        if (user?.dungeonEntries == null) return;

        foreach (var kvp in user.dungeonEntries)
        {
            if (int.TryParse(kvp.Key, out int dungeonId))
            {
                cachedDungeonEntries[dungeonId] = kvp.Value.currentEntries;
            }
        }
    }

    public static int GetDungeonEntries(int dungeonId)
    {
        return cachedDungeonEntries.TryGetValue(dungeonId, out int count) ? count : 0;
    }

    public static bool HasEnoughDungeonEntries(int dungeonId, int amount = 1)
    {
        return GetDungeonEntries(dungeonId) >= amount;
    }

    public static void SetDungeonEntriesImmediate(int dungeonId, int count)
    {
        if (count <= 0)
        {
            cachedDungeonEntries.Remove(dungeonId);
        }
        else
        {
            cachedDungeonEntries[dungeonId] = count;
        }
    }

    //캐릭터 정보 가져오기
    public static CachedCharacter GetCharacter(string characterId)
    {
        return cachedCharacters.TryGetValue(characterId, out var character) ? character : null;
    }

    public static List<CachedCharacter> GetAllCharacters()
    {
        return new List<CachedCharacter>(cachedCharacters.Values);
    }

    public static bool HasCharacter(string characterId)
    {
        return cachedCharacters.ContainsKey(characterId);
    }

    //캐릭터 업데이트
    public static async void UpdateCharacterLevel(string characterId, int newLevel)
    {
        if (cachedCharacters.TryGetValue(characterId, out var character))
        {
            character.level = newLevel;
            await DatabaseManager.Instance.SaveCharacterAsync(characterId);
        }
    }

    public static bool IsPresetCompletelyEmpty(int presetIndex)
    {
        for (int i = 0; i < 5; i++)
        {
            if (selectedDeckUnitIds[presetIndex, i] != 0)
                return false;
        }
        return true;
    }

    //public static void SetLastClearedStageImmediate(int stageId)
    //{
    //    LastClearedStage = stageId;
    //    OnProfileChanged?.Invoke();
    //}

    public static bool IsAnyPresetSaved()
    {
        for (int p = 0; p < 5; p++)
        {
            for (int i = 0; i < 5; i++)
            {
                if (selectedDeckUnitIds[p, i] != 0)
                    return true;
            }
        }
        return false;
    }


    // 데이터 초기화
    public static void Clear()
    {
        cachedGold = 0;
        cachedDiamond = 0;
        cachedStamina = 0;
        cachedEnhanceStone = 0;
        cachedLevel = 1;
        cachedExp = 0;
        cachedNickname = "";
        cachedCharacters.Clear();
        cachedItems.Clear();
        isInitialized = false;
    }

    // 현재 선택된 던전 ID 저장
    public static void SetSelectedOreDungeonId(int id)
    {
        OreDungeonID = id;
    }

    public static void SetProfileImmediate(int level, int exp)
    {
        cachedLevel = level;
        cachedExp = exp;
        NotifyProfileChanged();
    }

    public static void NotifyProfileChanged()
    {
        OnProfileChanged?.Invoke();
    }

    // 메일 관련
    public static void NotifyMailsChanged()
    {
        OnMailsChanged?.Invoke();
    }

    public static int GetUnreadMailCount()
    {
        return DatabaseManager.Instance?.GetUnreadMailCount() ?? 0;
    }

    public static int GetClaimableMailCount()
    {
        return DatabaseManager.Instance?.GetClaimableMailCount() ?? 0;
    }

    // 업적 관련
    public static void NotifyAchievementsChanged()
    {
        OnAchievementsChanged?.Invoke();
    }

    public static int GetClaimableAchievementCount()
    {
        return DatabaseManager.Instance?.GetClaimableAchievementCount() ?? 0;
    }

    public static int GetCompletedAchievementCount()
    {
        return DatabaseManager.Instance?.GetCompletedAchievementCount() ?? 0;
    }

    // 퀘스트 관련
    public static void NotifyQuestsChanged()
    {
        OnQuestsChanged?.Invoke();
    }

    public static int GetClaimableQuestCount()
    {
        return DatabaseManager.Instance?.GetClaimableQuestCount() ?? 0;
    }

    public static int GetCompletedQuestCount()
    {
        return DatabaseManager.Instance?.GetCompletedQuestCount() ?? 0;
    }

    public static void NotifyCharacterUpdated(string characterId)
    {
        OnCharacterUpdated?.Invoke(characterId);
    }

    public static bool IsPresetEmptyOnUnlockedSlots(int presetIndex)
    {

        if (selectedDeckUnitIds == null)
            return true;


        var user = DatabaseManager.Instance?.CurrentUser;
        if (user == null)
            return true;

        var unlocks = user.presetSlotUnlocks;

        for (int slot = 0; slot < 5; slot++)
        {
            bool isUnlocked =
                slot < 2 ||                       
                (unlocks != null && unlocks.IsSlotUnlocked(presetIndex, slot));

            if (!isUnlocked)
                continue;

            if (selectedDeckUnitIds[presetIndex, slot] != 0)
                return false;
        }

        return true;
    }




}
