using System;

namespace Tutorial
{
    /// <summary>
    /// 게임 이벤트 허브 - 게임 로직과 튜토리얼 시스템을 분리
    /// 게임 로직에서 이벤트를 발생시키면 TutorialManager가 구독해서 처리
    /// </summary>
    public static class GameEvents
    {
        #region 경험치/레벨업 이벤트

        /// <summary>
        /// 최초 경험치 획득 시 발생
        /// </summary>
        public static event Action OnFirstExpGained;

        public static void RaiseFirstExpGained()
        {
            OnFirstExpGained?.Invoke();
        }

        #endregion

        #region 버프 이벤트

        /// <summary>
        /// 버프 활성화 시 발생 (버프 이름 전달)
        /// </summary>
        public static event Action<string> OnBuffActivated;

        /// <summary>
        /// 버프 완료 시 발생 (모든 칸 채움 등)
        /// </summary>
        public static event Action<string> OnBuffCompleted;

        public static void RaiseBuffActivated(string buffName)
        {
            OnBuffActivated?.Invoke(buffName);
        }

        public static void RaiseBuffCompleted(string conditionKey)
        {
            OnBuffCompleted?.Invoke(conditionKey);
        }

        #endregion

        #region 드래그 이벤트

        /// <summary>
        /// 드래그 완료 시 발생 (소스, 타겟 이름 전달)
        /// </summary>
        public static event Action<string, string> OnDragCompleted;

        public static void RaiseDragCompleted(string sourceName, string targetName)
        {
            OnDragCompleted?.Invoke(sourceName, targetName);
        }

        #endregion

        #region 스테이지 제약 조건 조회

        /// <summary>
        /// 스테이지별 보상 옵션 조회 (TutorialOverrides에서 등록)
        /// 반환값: RewardOptions (패시브 스킬 비활성화, 방벽 회복만 등)
        /// </summary>
        public static Func<int, RewardOptions> GetRewardOptions;

        /// <summary>
        /// 고정 유닛 순서 사용 여부 조회
        /// </summary>
        public static Func<int, bool> ShouldUseFixedUnitOrder;

        #endregion

        #region 정적 참조 (Find 대체)

        /// <summary>
        /// StageManager 참조 (씬 로드 시 자동 등록)
        /// </summary>
        public static StageManager StageManager { get; set; }

        /// <summary>
        /// GameManager 참조 (씬 로드 시 자동 등록)
        /// </summary>
        public static GameManager GameManager { get; set; }

        /// <summary>
        /// PlayerStageGold 참조 (씬 로드 시 자동 등록)
        /// </summary>
        public static PlayerStageGold PlayerGold { get; set; }

        #endregion

        #region 초기화

        /// <summary>
        /// 모든 이벤트 구독 해제 (테스트용)
        /// </summary>
        public static void ClearAllEvents()
        {
            OnFirstExpGained = null;
            OnBuffActivated = null;
            OnBuffCompleted = null;
            OnDragCompleted = null;
            GetRewardOptions = null;
            ShouldUseFixedUnitOrder = null;
        }

        /// <summary>
        /// 모든 정적 참조 해제 (씬 전환 시)
        /// </summary>
        public static void ClearAllReferences()
        {
            StageManager = null;
            GameManager = null;
            PlayerGold = null;
        }

        #endregion
    }

    /// <summary>
    /// 스테이지별 보상 옵션
    /// </summary>
    public struct RewardOptions
    {
        /// <summary>
        /// 패시브 스킬 뽑기 비활성화 여부
        /// </summary>
        public bool DisablePassiveSkillDraw;

        /// <summary>
        /// 방벽 회복 스킬만 제공 여부
        /// </summary>
        public bool ShieldRegenOnly;

        /// <summary>
        /// 기본값 (제한 없음)
        /// </summary>
        public static RewardOptions Default => new RewardOptions
        {
            DisablePassiveSkillDraw = false,
            ShieldRegenOnly = false
        };
    }
}
