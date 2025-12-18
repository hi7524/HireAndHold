using System.Collections.Generic;
using UnityEngine;
using System;

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
    private static int cachedSummonTicket = 0;

    //유저 프로필 캐싱
    private static int cachedLevel = 1;
    private static int cachedExp = 0;
    private static string cachedNickname = "";

    public static int LastClearedStage { get; private set; }


    //캐릭터 데이터 캐싱
    private static Dictionary<string, CachedCharacter> cachedCharacters = new Dictionary<string, CachedCharacter>();

    // 아이템 캐시
    private static Dictionary<int, int> cachedItems = new Dictionary<int, int>();

    //초기화 플래그
    private static bool isInitialized = false;

    //재화 프로퍼티
    public static long Gold => cachedGold;
    public static int Diamond => cachedDiamond;
    public static int Stamina => cachedStamina;
    public static int EnhanceStone => cachedEnhanceStone;
    public static int SummonTicket => cachedSummonTicket;

    // 유저 프로필 프로퍼티
    public static int Level => cachedLevel;
    public static int Exp => cachedExp;
    public static string Nickname => cachedNickname;

    //초기화 상태
    public static bool IsInitialized => isInitialized;

    // 던전 선택 정보 (ID)
    public static int OreDungeonID { get; private set; }
    public static event Action OnProfileChanged;

    public static event Action OnCurrencyChanged;

    public static void NotifyCurrencyChanged()
    {
        OnCurrencyChanged?.Invoke();
    }

    public static void SetLastClearedStageImmediate(int stage)
    {
        LastClearedStage = stage;
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
        cachedSummonTicket = user.currency.summonTicket;


        cachedLevel = user.profile.level;
        cachedExp = user.profile.exp;
        cachedNickname = user.profile.nickname;


        SyncCharactersFromDatabase();

        // 아이템 동기화
        SyncItemsFromDatabase();
        SyncSelectedUnitIdsFromActivePreset();

        isInitialized = true;
        Debug.Log("데이터 동기화 완료");
        Debug.Log($"골드: {cachedGold}, 다이아: {cachedDiamond}, 강화석: {cachedEnhanceStone}");
        Debug.Log($"선택된 유닛: {string.Join(", ", selectedUnitIds)}");
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

        Debug.Log($"[PlayData] selectedUnitIds 동기화: {string.Join(", ", selectedUnitIds)}");
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

        Debug.Log($"캐릭터 {cachedCharacters.Count}개 동기화 완료");
    }

    public static void SetNicknameImmediate(string nickname)
    {
        cachedNickname = nickname;
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

        Debug.Log($"아이템 {cachedItems.Count}종 동기화 완료");
    }

    //재화 변경 로컬 캐시 + DB 동기화

    public static async void AddStamina(int amount)
    {
        cachedStamina += amount;
        await DatabaseManager.Instance.AddStaminaAsync(amount);
        Debug.Log($" 스태미나 변경: {amount:+#;-#;0} (현재: {cachedStamina})");
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
        cachedSummonTicket = 0;
        cachedLevel = 1;
        cachedExp = 0;
        cachedNickname = "";
        cachedCharacters.Clear();
        cachedItems.Clear();
        isInitialized = false;

        Debug.Log("캐시 초기화");
    }

    // 현재 선택된 던전 ID 저장
    public static void SetSelectedOreDungeonId(int id)
    {
        OreDungeonID = id;
        Debug.Log($"현재 선택된 던전 ID: {OreDungeonID}");
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
}
