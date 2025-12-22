using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GameData;
using UnityEngine;

/// <summary>
/// 업적 시스템 매니저 (static)
/// 업적 조건 체크, 진행도 관리, 보상 지급을 담당
/// </summary>
public static class AchievementManager
{
    // 업적 완료 이벤트
    public static event Action<int> OnAchievementCompleted;
    // 업적 진행도 변경 이벤트
    public static event Action<int, int> OnAchievementProgressChanged;
    // 업적 보상 수령 이벤트
    public static event Action<int> OnAchievementRewardClaimed;

    /// <summary>
    /// 업적 진행도 업데이트 (조건 키와 값으로)
    /// </summary>
    public static async UniTask UpdateProgressAsync(string conditionKey, int value, bool isAbsolute = false)
    {
        if (!DataTableManager.IsInitialized) return;

        var achievements = DataTableManager.AchievementTable?.GetByConditionKey(conditionKey);
        if (achievements == null) return;

        foreach (var achievement in achievements)
        {
            await UpdateAchievementProgressAsync(achievement.Achievements_ID, value, isAbsolute);
        }
    }

    /// <summary>
    /// 특정 업적의 진행도 업데이트
    /// </summary>
    public static async UniTask UpdateAchievementProgressAsync(int achievementId, int value, bool isAbsolute = false)
    {
        var achievementData = DataTableManager.AchievementTable?.Get(achievementId);
        if (achievementData == null) return;

        var progress = GetProgress(achievementId);
        if (progress == null)
        {
            progress = new AchievementProgress(achievementId);
        }

        // 이미 완료된 업적은 스킵
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
        if (progress.currentValue >= achievementData.Condition_Value)
        {
            progress.currentValue = achievementData.Condition_Value;
            progress.isCompleted = true;
            progress.completedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // 저장
            await DatabaseManager.Instance.SaveAchievementProgressAsync(achievementId, progress);

            // 이벤트 발생
            OnAchievementCompleted?.Invoke(achievementId);

            Debug.Log($"[Achievement] 완료! ID:{achievementId}, 조건:{achievementData.Condition_Key}");
        }
        else
        {
            // 저장
            await DatabaseManager.Instance.SaveAchievementProgressAsync(achievementId, progress);

            // 진행도 변경 이벤트
            OnAchievementProgressChanged?.Invoke(achievementId, progress.currentValue);

            Debug.Log($"[Achievement] 진행도 업데이트: ID:{achievementId}, {progress.currentValue}/{achievementData.Condition_Value}");
        }
    }

    /// <summary>
    /// 업적 보상 수령
    /// </summary>
    public static async UniTask<bool> ClaimRewardAsync(int achievementId)
    {
        var achievementData = DataTableManager.AchievementTable?.Get(achievementId);
        if (achievementData == null) return false;

        var progress = GetProgress(achievementId);
        if (progress == null || !progress.isCompleted || progress.isRewarded)
        {
            return false;
        }

        // 보상 지급
        bool rewardSuccess = await GiveRewardAsync(achievementData);
        if (!rewardSuccess) return false;

        // 보상 수령 완료 처리
        progress.isRewarded = true;
        progress.rewardedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await DatabaseManager.Instance.SaveAchievementProgressAsync(achievementId, progress);

        OnAchievementRewardClaimed?.Invoke(achievementId);

        return true;
    }

    /// <summary>
    /// 보상 지급
    /// </summary>
    private static async UniTask<bool> GiveRewardAsync(AchievementData data)
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
    /// 업적 진행도 조회
    /// </summary>
    public static AchievementProgress GetProgress(int achievementId)
    {
        return DatabaseManager.Instance?.GetAchievementProgress(achievementId);
    }

    /// <summary>
    /// 모든 업적 진행도 조회
    /// </summary>
    public static List<AchievementProgress> GetAllProgress()
    {
        return DatabaseManager.Instance?.GetAllAchievementProgress() ?? new List<AchievementProgress>();
    }

    /// <summary>
    /// 수령 가능한 업적 개수
    /// </summary>
    public static int GetClaimableCount()
    {
        return DatabaseManager.Instance?.GetClaimableAchievementCount() ?? 0;
    }

    /// <summary>
    /// 완료되었지만 보상 미수령인 업적 목록
    /// </summary>
    public static List<AchievementData> GetClaimableAchievements()
    {
        var result = new List<AchievementData>();
        var allProgress = GetAllProgress();

        foreach (var progress in allProgress)
        {
            if (progress.isCompleted && !progress.isRewarded)
            {
                var data = DataTableManager.AchievementTable?.Get(progress.achievementId);
                if (data != null)
                    result.Add(data);
            }
        }

        return result;
    }

    #region 디버그

    /// <summary>
    /// 모든 업적 상태 출력 (디버그용)
    /// </summary>
    public static void DebugPrintAllAchievements()
    {
        var allAchievements = DataTableManager.AchievementTable?.GetExposedAchievements();
        if (allAchievements == null) return;

        Debug.Log("===== [Achievement] 전체 업적 상태 =====");
        foreach (var data in allAchievements)
        {
            var progress = GetProgress(data.Achievements_ID);
            int current = progress?.currentValue ?? 0;
            bool completed = progress?.isCompleted ?? false;
            bool rewarded = progress?.isRewarded ?? false;

            string status = rewarded ? "보상수령" : (completed ? "완료" : "진행중");
            Debug.Log($"[{data.Achievements_ID}] {data.Condition_Key}: {current}/{data.Condition_Value} ({status})");
        }
        Debug.Log($"===== 수령가능: {GetClaimableCount()}개 =====");
    }

    /// <summary>
    /// 테스트용: 특정 조건 강제 완료
    /// </summary>
    public static async UniTask DebugCompleteAsync(string conditionKey)
    {
        var achievements = DataTableManager.AchievementTable?.GetByConditionKey(conditionKey);
        if (achievements == null) return;

        foreach (var data in achievements)
        {
            await UpdateAchievementProgressAsync(data.Achievements_ID, data.Condition_Value, true);
        }
    }

    #endregion

    #region 조건별 업데이트 헬퍼 메서드

    public static async UniTask UpdateLoginDaysAsync(int days)
    {
        await UpdateProgressAsync("LOGIN_DAYS", days, true);
    }

    public static async UniTask CompleteTutorialAsync()
    {
        await UpdateProgressAsync("TUTORIAL_CLEAR", 1, true);
    }

    public static async UniTask UpdateStageMaxClearAsync(int stageNumber)
    {
        await UpdateProgressAsync("STAGE_MAX_CLEAR", stageNumber, true);
    }

    public static async UniTask AddMonsterKillAsync(int count = 1)
    {
        await UpdateProgressAsync("MONSTER_KILL", count);
    }

    public static async UniTask CompleteBarrierNoDamageAsync()
    {
        await UpdateProgressAsync("BARRIER_NO_DAMAGE_CLEAR", 1, true);
    }

    public static async UniTask CompleteDeckFullAsync()
    {
        await UpdateProgressAsync("DECK_FULL_FIRST", 1, true);
    }

    public static async UniTask UpdateUnitCollectCountAsync(int count)
    {
        await UpdateProgressAsync("UNIT_COLLECT_COUNT", count, true);
    }

    public static async UniTask CompleteFirstCombineAsync()
    {
        await UpdateProgressAsync("UNIT_COMBINE_FIRST", 1, true);
    }

    public static async UniTask AddCombineCountAsync(int count = 1)
    {
        await UpdateProgressAsync("UNIT_COMBINE_COUNT", count);
    }

    public static async UniTask AddCombineStar2Async(int count = 1)
    {
        await UpdateProgressAsync("UNIT_COMBINE_GET_STAR2", count);
    }

    public static async UniTask AddCombineStar3Async(int count = 1)
    {
        await UpdateProgressAsync("UNIT_COMBINE_GET_STAR3", count);
    }

    public static async UniTask AddNormalUpgradeSuccessAsync(int count = 1)
    {
        await UpdateProgressAsync("UNIT_UPGRADE_NORMAL_SUCCESS", 1, true);
        await UpdateProgressAsync("UNIT_UPGRADE_NORMAL_SUCCESS_COUNT", count);
    }

    public static async UniTask CompleteNormalUpgradeMaxAsync()
    {
        await UpdateProgressAsync("UNIT_UPGRADE_NORMAL_MAX_LEVEL", 1, true);
    }

    public static async UniTask AddHeroUpgradeSuccessAsync(int count = 1)
    {
        await UpdateProgressAsync("UNIT_UPGRADE_HERO_SUCCESS", count);
        await UpdateProgressAsync("UNIT_UPGRADE_HERO_SUCCESS_COUNT", count);
    }

    public static async UniTask CompleteHeroUpgradeMaxAsync()
    {
        await UpdateProgressAsync("UNIT_UPGRADE_HERO_MAX_LEVEL", 1, true);
    }

    public static async UniTask AddDungeonClearAsync(int count = 1)
    {
        await UpdateProgressAsync("DUNGEON_CLEAR", count);
    }

    public static async UniTask AddStoneDungeonClearAsync(int count = 1)
    {
        await UpdateProgressAsync("STONE_DUNGEON_CLEAR", count);
    }

    public static async UniTask AddStoneGetAsync(int amount)
    {
        await UpdateProgressAsync("STONE_GET", amount);
    }

    public static async UniTask AddStoneBonusBigSuccessAsync(int count = 1)
    {
        await UpdateProgressAsync("STONE_BONUS_BIG_SUCCESS", count);
    }

    public static async UniTask AddGoldDungeonClearAsync(int count = 1)
    {
        await UpdateProgressAsync("GOLD_DUNGEON_CLEAR", count);
    }

    public static async UniTask AddGoldGetAsync(long amount)
    {
        await UpdateProgressAsync("GOLD_GET", (int)amount);
    }

    public static async UniTask AddDiceDungeonClearAsync(int count = 1)
    {
        await UpdateProgressAsync("DICE_DUNGEON_CLEAR", count);
    }

    public static async UniTask CompleteDiceHighRewardAsync()
    {
        await UpdateProgressAsync("DICE_HIGH_REWARD", 1, true);
    }

    public static async UniTask CompleteGachaNormalAsync()
    {
        await UpdateProgressAsync("GACHA_NORMAL", 1, true);
    }

    public static async UniTask CompleteGachaPremiumAsync()
    {
        await UpdateProgressAsync("GACHA_PREMIUM", 1, true);
    }

    public static async UniTask CompleteGachaNormal10Async()
    {
        await UpdateProgressAsync("GACHA_NORMAL_10", 10, true);
    }

    public static async UniTask CompleteGachaPremium10Async()
    {
        await UpdateProgressAsync("GACHA_PREMIUM_10", 10, true);
    }

    public static async UniTask AddGachaFreeAsync(int count = 1)
    {
        await UpdateProgressAsync("GACHA_FREE", count);
    }

    public static async UniTask CompleteGachaGetUniqueAsync()
    {
        await UpdateProgressAsync("GACHA_GET_UNIQUE", 1, true);
    }

    public static async UniTask CompleteGachaGetLegendAsync()
    {
        await UpdateProgressAsync("GACHA_GET_LEGEND", 1, true);
    }

    public static async UniTask CompleteGachaGetEpicAsync()
    {
        await UpdateProgressAsync("GACHA_GET_EPIC", 1, true);
    }

    public static async UniTask AddGachaTotalAsync(int count = 1)
    {
        await UpdateProgressAsync("GACHA_TOTAL", count);
    }

    #endregion
}
