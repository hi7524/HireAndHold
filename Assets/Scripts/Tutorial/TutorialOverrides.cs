using System.Collections.Generic;
using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 스테이지별 제약 조건 관리
    /// 모든 스테이지 제약 조건을 한 곳에서 관리
    /// </summary>
    public static class TutorialOverrides
    {
        #region 스테이지별 제약 조건 정의

        /// <summary>
        /// 패시브 스킬 뽑기 비활성화 스테이지
        /// </summary>
        private static readonly HashSet<int> DisablePassiveDrawStages = new HashSet<int>
        {
            701, // 1스테이지 튜토리얼
            702, // 2스테이지 튜토리얼
            703  // 3스테이지 튜토리얼
        };

        /// <summary>
        /// 고정 유닛 순서 사용 스테이지
        /// </summary>
        private static readonly HashSet<int> FixedUnitOrderStages = new HashSet<int>
        {
            701  // 1스테이지: 엘렌, 타론, 리브 고정
        };

        /// <summary>
        /// 방벽 회복 스킬만 제공하는 스테이지
        /// </summary>
        private static readonly HashSet<int> ShieldRegenOnlyStages = new HashSet<int>
        {
            701, // 1스테이지 튜토리얼
            702  // 2스테이지 튜토리얼
        };

        /// <summary>
        /// 버프 활성화 시 튜토리얼 조건 알림하는 스테이지
        /// </summary>
        private static readonly HashSet<int> BuffTutorialStages = new HashSet<int>
        {
            703  // 3스테이지: 버프 튜토리얼
        };

        #endregion

        #region 초기화

        /// <summary>
        /// TutorialManager 시작 시 호출 - GameEvents에 함수 등록
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            GameEvents.GetRewardOptions = GetRewardOptions;
            GameEvents.ShouldUseFixedUnitOrder = ShouldUseFixedUnitOrder;
        }

        #endregion

        #region 제약 조건 조회 메서드

        /// <summary>
        /// 스테이지별 보상 옵션 반환
        /// </summary>
        public static RewardOptions GetRewardOptions(int stageId)
        {
            return new RewardOptions
            {
                DisablePassiveSkillDraw = DisablePassiveDrawStages.Contains(stageId),
                ShieldRegenOnly = ShieldRegenOnlyStages.Contains(stageId)
            };
        }

        /// <summary>
        /// 고정 유닛 순서 사용 여부
        /// </summary>
        public static bool ShouldUseFixedUnitOrder(int stageId)
        {
            return FixedUnitOrderStages.Contains(stageId);
        }

        /// <summary>
        /// 버프 활성화 시 튜토리얼 알림 여부
        /// </summary>
        public static bool ShouldNotifyBuffTutorial(int stageId)
        {
            return BuffTutorialStages.Contains(stageId);
        }

        /// <summary>
        /// 튜토리얼 스테이지인지 확인
        /// </summary>
        public static bool IsTutorialStage(int stageId)
        {
            return stageId >= 701 && stageId <= 704;
        }

        #endregion

        #region 편의 메서드

        /// <summary>
        /// 새 스테이지 추가 시 여기에만 추가하면 됨
        /// 예: 705 스테이지 추가 시
        /// </summary>
        public static void AddTutorialStage(int stageId, bool disablePassive, bool fixedOrder, bool shieldOnly, bool buffTutorial)
        {
            if (disablePassive) DisablePassiveDrawStages.Add(stageId);
            if (fixedOrder) FixedUnitOrderStages.Add(stageId);
            if (shieldOnly) ShieldRegenOnlyStages.Add(stageId);
            if (buffTutorial) BuffTutorialStages.Add(stageId);
        }

        #endregion
    }
}
