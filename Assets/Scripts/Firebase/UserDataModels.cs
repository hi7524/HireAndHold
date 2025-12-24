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
        public Dictionary<string, MailData> mails;
        public Dictionary<string, bool> claimedGlobalMails; // 수령한 전역 메일 ID
        public Dictionary<string, AchievementProgress> achievements; // 업적 진행도
        public int activePresetIndex;
        public UserSettings settings;
        public DailyRewardData dailyReward;
        public TutorialProgressData tutorial; // 튜토리얼 진행 상황
        public Dictionary<string, DungeonEntryData> dungeonEntries; // 던전 입장권

        public UserData()
        {
            characters = new Dictionary<string, OwnedCharacter>();
            inventory = new Dictionary<string, OwnedEquipment>();
            stageProgress = new Dictionary<string, StageProgress>();
            partyPresets = new Dictionary<string, PartyPreset>();
            items = new Dictionary<string, int>();
            mails = new Dictionary<string, MailData>();
            claimedGlobalMails = new Dictionary<string, bool>();
            achievements = new Dictionary<string, AchievementProgress>();
            dailyReward = new DailyRewardData();
            tutorial = new TutorialProgressData();
            dungeonEntries = new Dictionary<string, DungeonEntryData>();
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

        public string profileIconAddress;
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

    #region 일일 보상

    [Serializable]
    public class DailyRewardData
    {
        public List<string> claimedDates;      // 보상 받은 날짜 목록 (yyyy-MM-dd)
        public string lastClaimDate;           // 마지막으로 보상 받은 날짜
        public int totalClaimCount;            // 총 출석 횟수
        public string currentMonth;            // 현재 월 (yyyy-MM)

        public DailyRewardData()
        {
            claimedDates = new List<string>();
            lastClaimDate = null;
            totalClaimCount = 0;
            currentMonth = null;
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

        public MailData()
        {
            reward = new MailReward();
        }

        public bool IsExpired()
        {
            if (expireAt <= 0) return false;
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > expireAt;
        }
    }

    [Serializable]
    public class MailReward
    {
        public long gold;
        public int diamond;
        public int stamina;
        public int enhanceStone;
        public Dictionary<int, int> items; // itemId -> count

        public MailReward()
        {
            items = new Dictionary<int, int>();
        }

        public bool HasReward()
        {
            return gold > 0 || diamond > 0 || stamina > 0 || enhanceStone > 0 || (items != null && items.Count > 0);
        }
    }

    /// <summary>
    /// 전역 메일 데이터 (모든 유저에게 공통으로 보내는 메일)
    /// Firebase 경로: globalMails/{mailId}
    /// </summary>
    [Serializable]
    public class GlobalMailData
    {
        public string mailId;
        public string title;
        public string content;
        public MailReward reward;
        public long createdAt;
        public long expireAt;

        public GlobalMailData()
        {
            reward = new MailReward();
        }

        public bool IsExpired()
        {
            if (expireAt <= 0) return false;
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > expireAt;
        }

        /// <summary>
        /// 전역 메일을 개인 메일 형태로 변환 (UI 표시용)
        /// </summary>
        public MailData ToMailData(bool isClaimed)
        {
            return new MailData
            {
                mailId = mailId,
                title = title,
                content = content,
                reward = reward,
                isRead = isClaimed, // 수령했으면 읽음 처리
                isClaimed = isClaimed,
                createdAt = createdAt,
                expireAt = expireAt
            };
        }
    }

    #endregion

    #region 업적

    [Serializable]
    public class AchievementProgress
    {
        public int achievementId;
        public int currentValue;        // 현재 진행값 (누적형인 경우)
        public bool isCompleted;        // 완료 여부
        public bool isRewarded;         // 보상 수령 여부
        public long completedAt;        // 완료 시간
        public long rewardedAt;         // 보상 수령 시간

        public AchievementProgress() { }

        public AchievementProgress(int id)
        {
            achievementId = id;
            currentValue = 0;
            isCompleted = false;
            isRewarded = false;
            completedAt = 0;
            rewardedAt = 0;
        }
    }

    #endregion

    #region 던전 입장권

    /// <summary>
    /// 던전 입장권 데이터
    /// Firebase 경로: users/{userId}/dungeonEntries/{dungeonId}
    /// </summary>
    [Serializable]
    public class DungeonEntryData
    {
        public int dungeonId;               // 던전 ID
        public int currentEntries;          // 현재 남은 입장권 개수
        public string lastResetDate;        // 마지막 리셋 날짜 (yyyy-MM-dd)

        public DungeonEntryData() { }

        public DungeonEntryData(int dungeonId, int maxEntries)
        {
            this.dungeonId = dungeonId;
            this.currentEntries = maxEntries;
            this.lastResetDate = DateTime.Now.ToString("yyyy-MM-dd");
        }
    }

    #endregion

    #region 튜토리얼

    [Serializable]
    public class TutorialProgressData
    {
        /// <summary>
        /// 전체 튜토리얼 완료 여부
        /// </summary>
        public bool isCompleted;

        /// <summary>
        /// 완료한 시퀀스 ID 목록
        /// </summary>
        public List<string> completedSequences;

        /// <summary>
        /// 현재 진행 중인 시퀀스 ID
        /// </summary>
        public string currentSequenceId;

        /// <summary>
        /// 현재 시퀀스에서 진행 중인 스텝 인덱스
        /// </summary>
        public int currentStepIndex;

        /// <summary>
        /// 마지막 체크포인트 인덱스
        /// </summary>
        public int lastCheckpointIndex;

        public TutorialProgressData()
        {
            isCompleted = false;
            completedSequences = new List<string>();
            currentSequenceId = null;
            currentStepIndex = 0;
            lastCheckpointIndex = 0;
        }

        /// <summary>
        /// 특정 시퀀스가 완료되었는지 확인
        /// </summary>
        public bool IsSequenceCompleted(string sequenceId)
        {
            return completedSequences != null && completedSequences.Contains(sequenceId);
        }
    }

    #endregion
}
