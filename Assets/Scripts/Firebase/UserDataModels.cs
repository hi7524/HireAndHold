using System;
using System.Collections.Generic;

namespace GameData
{
    #region 유저 전체 데이터

    [Serializable]
    public class UserData
    {
        public UserProfile profile;
        public UserCurrency currency;
        public Dictionary<string, OwnedCharacter> characters;
        public Dictionary<string, OwnedEquipment> inventory;
        public Dictionary<string, StageProgress> stageProgress;
        public Dictionary<string, PartyPreset> partyPresets;
        public Dictionary<string, int> items;
        public int activePresetIndex;
        public UserSettings settings;

        public UserData()
        {
            characters = new Dictionary<string, OwnedCharacter>();
            inventory = new Dictionary<string, OwnedEquipment>();
            stageProgress = new Dictionary<string, StageProgress>();
            partyPresets = new Dictionary<string, PartyPreset>();
            items = new Dictionary<string, int>();
        }
    }

    #endregion

    #region 프로필

    [Serializable]
    public class UserProfile
    {
        public string oderId;
        public string nickname;
        public int level;
        public int exp;
        public long lastLoginTime;
        public long createdAt;
        public int totalPlayTime;
        public int highestStage;
        public int totalPower;          // 총 전투력
    }

    #endregion

    #region 재화

    [Serializable]
    public class UserCurrency
    {
        public long gold;
        public int diamond;
        public int stamina;
        public int maxStamina;
        public long lastStaminaTime;
        public int summonTicket;
        public int enhanceStone;
        public int skillPoint;          // 스킬 포인트
    }

    #endregion

    #region 캐릭터

    [Serializable]
    public class OwnedCharacter
    {
        public string id;
        public int star;
        public int level;
        public int exp;
        public int awakening;
        public bool isLocked;
        public long obtainedAt;
        public int enforceLevel;
        public int heroEnforceLevel;

        public OwnedCharacter() { }

        public OwnedCharacter(string id, int star = 1)
        {
            this.id = id;
            this.star = star;
            this.level = 1;
            this.exp = 0;
            this.awakening = 0;
            this.isLocked = false;
            this.obtainedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.enforceLevel = 0;
            this.heroEnforceLevel = 0;
        }
    }

    #endregion

    #region 장비

    public enum EquipmentType
    {
        Weapon = 0,
        Armor = 1,
        Accessory = 2
    }

    [Serializable]
    public class OwnedEquipment
    {
        public string uid;
        public string baseId;
        public int type;
        public int grade;
        public int level;
        public int star;
        public string equippedTo;
        public bool isLocked;
        public long obtainedAt;

        public OwnedEquipment() { }

        public OwnedEquipment(string baseId, int type, int grade)
        {
            this.uid = $"{baseId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            this.baseId = baseId;
            this.type = type;
            this.grade = grade;
            this.level = 1;
            this.star = 0;
            this.equippedTo = null;
            this.isLocked = false;
            this.obtainedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    #endregion

    #region 스테이지

    [Serializable]
    public class StageProgress
    {
        public bool isCleared;
        public int starRating;
        public int playCount;

        public StageProgress()
        {
            isCleared = false;
            starRating = 0;
            playCount = 0;
        }
    }

    #endregion

    #region 파티 프리셋
    [Serializable]
    public class PartyPreset
    {
        public int index;
        public string name;

        public string[] characterId = new string[5];
        public string[] iconAddress = new string[5];

        public List<string> skillIds;

        public string weaponUid;
        public string armorUid;
        public string accessoryUid;
        public bool isLocked;
        public long lastModified;

        public PartyPreset()
        {
            characterId = new string[5];  // [null, null, null, null, null]
            iconAddress = new string[5];

            skillIds = new List<string>();
            weaponUid = null;
            armorUid = null;
            accessoryUid = null;
            isLocked = false;
            lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public PartyPreset(int index)
        {
            this.index = index;
            this.name = $"파티 {index + 1}";

            characterId = new string[5];
            iconAddress = new string[5];

            skillIds = new List<string>();
            weaponUid = null;
            armorUid = null;
            accessoryUid = null;
            isLocked = false;
            lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
    #endregion


    #region 설정

    [Serializable]
    public class UserSettings
    {
        public float bgmVolume;
        public float sfxVolume;
        public bool pushNotification;
        public string language;
        public int graphicQuality;

        public UserSettings()
        {
            bgmVolume = 1f;
            sfxVolume = 1f;
            pushNotification = true;
            language = "ko";
            graphicQuality = 2;
        }
    }

    #endregion

    #region 우편함

    [Serializable]
    public class MailData
    {
        public string mailId;
        public string title;
        public string content;
        public MailReward reward;
        public bool isRead;
        public bool isClaimed;
        public long createdAt;
        public long expireAt;
    }

    [Serializable]
    public class MailReward
    {
        public long gold;
        public int diamond;
        public int stamina;
        public List<string> itemIds;

        public MailReward()
        {
            itemIds = new List<string>();
        }
    }

    #endregion
}
