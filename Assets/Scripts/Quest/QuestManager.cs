using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GameData;
using UnityEngine;

/// <summary>
/// 퀘스트 시스템 매니저 (static)
/// 일일/주간 퀘스트 조건 체크, 진행도 관리, 보상 지급, 리셋을 담당
/// </summary>
public static class QuestManager
{
    // 퀘스트 완료 이벤트
    public static event Action<int> OnQuestCompleted;
    // 퀘스트 진행도 변경 이벤트
    public static event Action<int, int> OnQuestProgressChanged;
    // 퀘스트 보상 수령 이벤트
    public static event Action<int> OnQuestRewardClaimed;
    // 퀘스트 리셋 이벤트
    public static event Action OnQuestsReset;

    // 일일 리셋 시간 (서버 시간 기준, 0시)
    private static readonly TimeSpan DailyResetTime = TimeSpan.Zero;
    // 주간 리셋 요일 (월요일)
    private static readonly DayOfWeek WeeklyResetDay = DayOfWeek.Monday;

    /// <summary>
    /// 퀘스트 초기화 및 리셋 체크
    /// 로그인 시 호출 필요
    /// </summary>
    public static async UniTask InitializeAsync()
    {
        await CheckAndResetQuestsAsync();
    }

    /// <summary>
    /// 퀘스트 리셋 체크 및 실행
    /// </summary>
    public static async UniTask CheckAndResetQuestsAsync()
    {
        if (!DataTableManager.IsInitialized) return;

        var allQuests = DataTableManager.QuestTable?.GetAll();
        if (allQuests == null) return;

        bool anyReset = false;
        var now = DateTimeOffset.UtcNow;

        foreach (var quest in allQuests)
        {
            var progress = GetProgress(quest.Quest_ID);
            if (progress == null) continue;

            bool needsReset = false;

            if (quest.IsDaily)
            {
                needsReset = NeedsDailyReset(progress.lastResetTime, now);
            }
            else if (quest.IsWeekly)
            {
                needsReset = NeedsWeeklyReset(progress.lastResetTime, now);
            }

            if (needsReset)
            {
                await ResetQuestProgressAsync(quest.Quest_ID);
                anyReset = true;
            }
        }

        if (anyReset)
        {
            OnQuestsReset?.Invoke();
        }
    }

    /// <summary>
    /// 일일 리셋이 필요한지 체크
    /// </summary>
    private static bool NeedsDailyReset(long lastResetTimestamp, DateTimeOffset now)
    {
        if (lastResetTimestamp == 0) return false;

        var lastReset = DateTimeOffset.FromUnixTimeSeconds(lastResetTimestamp);
        var todayReset = now.Date;
        var lastResetDate = lastReset.Date;

        return todayReset > lastResetDate;
    }

    /// <summary>
    /// 주간 리셋이 필요한지 체크
    /// </summary>
    private static bool NeedsWeeklyReset(long lastResetTimestamp, DateTimeOffset now)
    {
        if (lastResetTimestamp == 0) return false;

        var lastReset = DateTimeOffset.FromUnixTimeSeconds(lastResetTimestamp);

        // 이번 주 월요일 계산
        int daysUntilMonday = ((int)now.DayOfWeek - (int)WeeklyResetDay + 7) % 7;
        var thisWeekMonday = now.Date.AddDays(-daysUntilMonday);

        // 마지막 리셋 주 월요일 계산
        int lastResetDaysUntilMonday = ((int)lastReset.DayOfWeek - (int)WeeklyResetDay + 7) % 7;
        var lastResetWeekMonday = lastReset.Date.AddDays(-lastResetDaysUntilMonday);

        return thisWeekMonday > lastResetWeekMonday;
    }

    /// <summary>
    /// 특정 퀘스트 리셋
    /// </summary>
    private static async UniTask ResetQuestProgressAsync(int questId)
    {
        var progress = new QuestProgress(questId);
        progress.lastResetTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await DatabaseManager.Instance.SaveQuestProgressAsync(questId, progress);
    }

    /// <summary>
    /// 퀘스트 진행도 업데이트 (조건 키와 값으로)
    /// </summary>
    public static async UniTask UpdateProgressAsync(string conditionKey, int value, bool isAbsolute = false)
    {
        if (!DataTableManager.IsInitialized) return;

        var quests = DataTableManager.QuestTable?.GetByConditionKey(conditionKey);
        if (quests == null) return;

        foreach (var quest in quests)
        {
            await UpdateQuestProgressAsync(quest.Quest_ID, value, isAbsolute);
        }
    }

    /// <summary>
    /// 특정 퀘스트의 진행도 업데이트
    /// </summary>
    public static async UniTask UpdateQuestProgressAsync(int questId, int value, bool isAbsolute = false)
    {
        var questData = DataTableManager.QuestTable?.Get(questId);
        if (questData == null) return;

        var progress = GetProgress(questId);
        if (progress == null)
        {
            progress = new QuestProgress(questId);
            progress.lastResetTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        // 이미 완료된 퀘스트는 스킵
        if (progress.isCompleted) return;

        // 진행도 업데이트
        if (isAbsolute)
        {
            progress.currentValue = value;
        }
        else
        {
            progress.currentValue += value;
        }

        // 완료 체크
        if (progress.currentValue >= questData.Condition_Value)
        {
            progress.currentValue = questData.Condition_Value;
            progress.isCompleted = true;
            progress.completedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // 저장
            await DatabaseManager.Instance.SaveQuestProgressAsync(questId, progress);

            // 이벤트 발생
            OnQuestCompleted?.Invoke(questId);
        }
        else
        {
            // 저장
            await DatabaseManager.Instance.SaveQuestProgressAsync(questId, progress);

            // 진행도 변경 이벤트
            OnQuestProgressChanged?.Invoke(questId, progress.currentValue);
        }
    }

    /// <summary>
    /// 퀘스트 보상 수령
    /// </summary>
    public static async UniTask<bool> ClaimRewardAsync(int questId)
    {
        var questData = DataTableManager.QuestTable?.Get(questId);
        if (questData == null) return false;

        var progress = GetProgress(questId);
        if (progress == null || !progress.isCompleted || progress.isRewarded)
        {
            return false;
        }

        // 보상 지급
        bool rewardSuccess = await GiveRewardAsync(questData);
        if (!rewardSuccess) return false;

        // 보상 수령 완료 처리
        progress.isRewarded = true;
        progress.rewardedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await DatabaseManager.Instance.SaveQuestProgressAsync(questId, progress);

        OnQuestRewardClaimed?.Invoke(questId);

        return true;
    }

    /// <summary>
    /// 보상 지급
    /// </summary>
    private static async UniTask<bool> GiveRewardAsync(QuestData data)
    {
        // REWARD_TYPE: 1=골드, 2=아이템/재화
        // REWARD_ID: 0=골드, 5102=다이아, 5103=소환티켓, 5201=강화석

        if (data.Reward_Type == 1)
        {
            // 골드 보상
            return await DatabaseManager.Instance.AddGoldAsync(data.Reward_Value);
        }
        else if (data.Reward_Type == 2)
        {
            // 아이템/재화 보상
            switch (data.Reward_ID)
            {
                case 5102: // 다이아
                    return await DatabaseManager.Instance.AddDiamondAsync(data.Reward_Value);
                case 5103: // 소환 티켓
                    return await DatabaseManager.Instance.AddItemAsync(5103, data.Reward_Value);
                case 5201: // 강화석
                    return await DatabaseManager.Instance.AddEnhanceStoneAsync(data.Reward_Value);
                default:
                    // 일반 아이템
                    return await DatabaseManager.Instance.AddItemAsync(data.Reward_ID, data.Reward_Value);
            }
        }

        return false;
    }

    /// <summary>
    /// 보상 지급 - 로컬만 (낙관적 업데이트용)
    /// </summary>
    private static void GiveRewardLocal(QuestData data)
    {
        if (data.Reward_Type == 1)
        {
            // 골드 보상
            PlayData.SetGoldImmediate(PlayData.Gold + data.Reward_Value);
        }
        else if (data.Reward_Type == 2)
        {
            switch (data.Reward_ID)
            {
                case 5102: // 다이아
                    PlayData.SetDiamondImmediate(PlayData.Diamond + data.Reward_Value);
                    break;
                case 5201: // 강화석
                    PlayData.SetEnhanceStoneImmediate(PlayData.EnhanceStone + data.Reward_Value);
                    break;
                default:
                    // 일반 아이템 (소환 티켓 등)
                    PlayData.SetItemCountImmediate(data.Reward_ID,
                        PlayData.GetItemCount(data.Reward_ID) + data.Reward_Value);
                    break;
            }
        }
    }

    /// <summary>
    /// 보상 지급 - Firebase만 (낙관적 업데이트용)
    /// </summary>
    private static UniTask GiveRewardFirebaseAsync(QuestData data)
    {
        if (data.Reward_Type == 1)
        {
            return DatabaseManager.Instance.AddGoldAsync(data.Reward_Value);
        }
        else if (data.Reward_Type == 2)
        {
            switch (data.Reward_ID)
            {
                case 5102:
                    return DatabaseManager.Instance.AddDiamondAsync(data.Reward_Value);
                case 5201:
                    return DatabaseManager.Instance.AddEnhanceStoneAsync(data.Reward_Value);
                default:
                    return DatabaseManager.Instance.AddItemAsync(data.Reward_ID, data.Reward_Value);
            }
        }
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// 일괄 보상 수령 (낙관적 업데이트) - 일일 퀘스트
    /// </summary>
    public static int ClaimAllDailyRewardsOptimistic(out UniTask saveTask)
    {
        var dailyQuests = DataTableManager.QuestTable?.GetDailyQuests();
        return ClaimRewardsOptimisticInternal(dailyQuests, out saveTask);
    }

    /// <summary>
    /// 일괄 보상 수령 (낙관적 업데이트) - 주간 퀘스트
    /// </summary>
    public static int ClaimAllWeeklyRewardsOptimistic(out UniTask saveTask)
    {
        var weeklyQuests = DataTableManager.QuestTable?.GetWeeklyQuests();
        return ClaimRewardsOptimisticInternal(weeklyQuests, out saveTask);
    }

    /// <summary>
    /// 일괄 보상 수령 내부 구현
    /// </summary>
    private static int ClaimRewardsOptimisticInternal(IEnumerable<QuestData> quests, out UniTask saveTask)
    {
        if (quests == null)
        {
            saveTask = UniTask.CompletedTask;
            return 0;
        }

        var claimable = new List<QuestData>();
        foreach (var quest in quests)
        {
            var progress = GetProgress(quest.Quest_ID);
            if (progress != null && progress.isCompleted && !progress.isRewarded)
            {
                claimable.Add(quest);
            }
        }

        if (claimable.Count == 0)
        {
            saveTask = UniTask.CompletedTask;
            return 0;
        }

        var firebaseTasks = new List<UniTask>();

        foreach (var data in claimable)
        {
            var progress = GetProgress(data.Quest_ID);
            if (progress == null) continue;

            // 로컬 즉시 업데이트
            GiveRewardLocal(data);
            progress.isRewarded = true;
            progress.rewardedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Firebase 작업 수집
            firebaseTasks.Add(GiveRewardFirebaseAsync(data));
            firebaseTasks.Add(DatabaseManager.Instance.SaveQuestProgressAsync(data.Quest_ID, progress));
        }

        // UI 갱신
        PlayData.NotifyCurrencyChanged();

        // 이벤트 발생
        foreach (var data in claimable)
        {
            OnQuestRewardClaimed?.Invoke(data.Quest_ID);
        }

        // Firebase 저장 작업 반환
        saveTask = UniTask.WhenAll(firebaseTasks);
        PendingSaveManager.Track(saveTask);

        return claimable.Count;
    }

    /// <summary>
    /// 퀘스트 진행도 조회
    /// </summary>
    public static QuestProgress GetProgress(int questId)
    {
        return DatabaseManager.Instance?.GetQuestProgress(questId);
    }

    /// <summary>
    /// 모든 퀘스트 진행도 조회
    /// </summary>
    public static List<QuestProgress> GetAllProgress()
    {
        return DatabaseManager.Instance?.GetAllQuestProgress() ?? new List<QuestProgress>();
    }

    /// <summary>
    /// 수령 가능한 퀘스트 개수
    /// </summary>
    public static int GetClaimableCount()
    {
        return DatabaseManager.Instance?.GetClaimableQuestCount() ?? 0;
    }

    /// <summary>
    /// 수령 가능한 일일 퀘스트 개수
    /// </summary>
    public static int GetClaimableDailyCount()
    {
        var dailyQuests = DataTableManager.QuestTable?.GetDailyQuests();
        if (dailyQuests == null) return 0;

        int count = 0;
        foreach (var quest in dailyQuests)
        {
            var progress = GetProgress(quest.Quest_ID);
            if (progress != null && progress.isCompleted && !progress.isRewarded)
                count++;
        }
        return count;
    }

    /// <summary>
    /// 수령 가능한 주간 퀘스트 개수
    /// </summary>
    public static int GetClaimableWeeklyCount()
    {
        var weeklyQuests = DataTableManager.QuestTable?.GetWeeklyQuests();
        if (weeklyQuests == null) return 0;

        int count = 0;
        foreach (var quest in weeklyQuests)
        {
            var progress = GetProgress(quest.Quest_ID);
            if (progress != null && progress.isCompleted && !progress.isRewarded)
                count++;
        }
        return count;
    }

    /// <summary>
    /// 완료되었지만 보상 미수령인 퀘스트 목록
    /// </summary>
    public static List<QuestData> GetClaimableQuests()
    {
        var result = new List<QuestData>();
        var allProgress = GetAllProgress();

        foreach (var progress in allProgress)
        {
            if (progress.isCompleted && !progress.isRewarded)
            {
                var data = DataTableManager.QuestTable?.Get(progress.questId);
                if (data != null)
                    result.Add(data);
            }
        }

        return result;
    }

    #region 디버그

    /// <summary>
    /// 모든 퀘스트 상태 출력 (디버그용)
    /// </summary>
    public static void DebugPrintAllQuests()
    {
        var allQuests = DataTableManager.QuestTable?.GetExposedQuests();
        if (allQuests == null) return;

        Debug.Log("===== [Quest] 전체 퀘스트 상태 =====");
        foreach (var data in allQuests)
        {
            var progress = GetProgress(data.Quest_ID);
            int current = progress?.currentValue ?? 0;
            bool completed = progress?.isCompleted ?? false;
            bool rewarded = progress?.isRewarded ?? false;

            string typeStr = data.IsDaily ? "일일" : "주간";
            string status = rewarded ? "보상수령" : (completed ? "완료" : "진행중");
            Debug.Log($"[{data.Quest_ID}] [{typeStr}] {data.Condition_Key}: {current}/{data.Condition_Value} ({status})");
        }
        Debug.Log($"===== 수령가능: 일일 {GetClaimableDailyCount()}개 / 주간 {GetClaimableWeeklyCount()}개 =====");
    }

    /// <summary>
    /// 테스트용: 특정 조건 강제 완료
    /// </summary>
    public static async UniTask DebugCompleteAsync(string conditionKey)
    {
        var quests = DataTableManager.QuestTable?.GetByConditionKey(conditionKey);
        if (quests == null) return;

        foreach (var data in quests)
        {
            await UpdateQuestProgressAsync(data.Quest_ID, data.Condition_Value, true);
        }
    }

    /// <summary>
    /// 테스트용: 모든 퀘스트 강제 리셋
    /// </summary>
    public static async UniTask DebugResetAllAsync()
    {
        var allQuests = DataTableManager.QuestTable?.GetAll();
        if (allQuests == null) return;

        foreach (var quest in allQuests)
        {
            await ResetQuestProgressAsync(quest.Quest_ID);
        }

        OnQuestsReset?.Invoke();
    }

    #endregion

    #region 조건별 업데이트 헬퍼 메서드

    public static async UniTask UpdateLoginDaysAsync(int days)
    {
        await UpdateProgressAsync("LOGIN_DAYS", 1, true);
    }

    public static async UniTask UpdateLoginDaysCountAsync(int count)
    {
        await UpdateProgressAsync("LOGIN_DAYS_COUNT", count);
    }

    public static async UniTask AddStageClearAsync(int count = 1)
    {
        await UpdateProgressAsync("STAGE_CLEAR", count);
        await UpdateProgressAsync("STAGE_CLEAR_COUNT", count);
    }

    public static async UniTask AddMonsterKillAsync(int count = 1)
    {
        await UpdateProgressAsync("MONSTER_KILL", count);
    }

    public static async UniTask AddBossMonsterKillAsync(int count = 1)
    {
        await UpdateProgressAsync("BOSS_MONSTER_KILL", count);
    }

    public static async UniTask AddStoneDungeonClearAsync(int count = 1)
    {
        await UpdateProgressAsync("STONE_DUNGEON_CLEAR", count);
    }

    public static async UniTask AddGoldDungeonClearAsync(int count = 1)
    {
        await UpdateProgressAsync("GOLD_DUNGEON_CLEAR", count);
    }

    public static async UniTask AddDiceDungeonClearAsync(int count = 1)
    {
        await UpdateProgressAsync("DICE_DUNGEON_CLEAR", count);
    }

    public static async UniTask AddNormalUpgradeSuccessAsync(int count = 1)
    {
        await UpdateProgressAsync("UNIT_UPGRADE_NORMAL_SUCCESS", 1, true);
    }

    public static async UniTask AddHeroUpgradeSuccessAsync(int count = 1)
    {
        await UpdateProgressAsync("UNIT_UPGRADE_HERO_SUCCESS", count);
    }

    public static async UniTask AddGachaNormalAsync(int count = 1)
    {
        await UpdateProgressAsync("GACHA_NORMAL", count);
    }

    public static async UniTask AddGachaPremiumAsync(int count = 1)
    {
        await UpdateProgressAsync("GACHA_PREMIUM", count);
    }

    #endregion
}
