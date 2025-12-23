using System.Collections.Generic;
using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 시작 조건
    /// </summary>
    public enum TutorialTriggerType
    {
        OnLobbyEnter,       // 로비 입장 시
        OnStageStart,       // 스테이지 시작 시
        OnStageClear,       // 스테이지 클리어 시
        OnLevelUp,          // 인게임 레벨업 시
        OnCondition,        // 특정 조건 만족 시
    }

    /// <summary>
    /// 튜토리얼 시퀀스 (여러 스텝을 포함하는 SO)
    /// </summary>
    [CreateAssetMenu(fileName = "NewTutorialSequence", menuName = "Tutorial/Sequence")]
    public class TutorialSequence : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("시퀀스 고유 ID")]
        public string sequenceId;

        [Tooltip("시퀀스 설명 (에디터용)")]
        [TextArea(2, 4)]
        public string description;

        [Header("시작 조건")]
        [Tooltip("튜토리얼이 시작되는 조건")]
        public TutorialTriggerType triggerType = TutorialTriggerType.OnStageStart;

        [Tooltip("특정 스테이지에서 시작 (701, 702, ...)")]
        public int triggerStageId;

        [Tooltip("특정 레벨에서 시작 (OnLevelUp용)")]
        public int triggerLevel;

        [Tooltip("조건 키 (OnCondition용)")]
        public string triggerConditionKey;

        [Header("스텝 목록")]
        [Tooltip("이 시퀀스에 포함된 튜토리얼 스텝들")]
        public List<TutorialStep> steps = new List<TutorialStep>();

        [Header("설정")]
        [Tooltip("스킵 가능 여부 (강제 튜토리얼은 false)")]
        public bool canSkip = false;

        [Tooltip("이 시퀀스 완료가 필수인지 (다음 진행에 필요)")]
        public bool isRequired = true;

        [Tooltip("우선순위 (낮을수록 먼저 실행)")]
        public int priority = 0;

        /// <summary>
        /// 총 스텝 수
        /// </summary>
        public int StepCount => steps?.Count ?? 0;

        /// <summary>
        /// 특정 인덱스의 스텝 가져오기
        /// </summary>
        public TutorialStep GetStep(int index)
        {
            if (steps == null || index < 0 || index >= steps.Count)
                return null;
            return steps[index];
        }

        /// <summary>
        /// 체크포인트 인덱스 목록 가져오기
        /// </summary>
        public List<int> GetCheckpointIndices()
        {
            var checkpoints = new List<int>();

            if (steps == null) return checkpoints;

            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i].isCheckpoint)
                {
                    checkpoints.Add(i);
                }
            }

            return checkpoints;
        }

        /// <summary>
        /// 특정 인덱스 이전의 가장 가까운 체크포인트 찾기
        /// </summary>
        public int GetNearestCheckpoint(int currentIndex)
        {
            if (steps == null) return 0;

            int nearestCheckpoint = 0;

            for (int i = 0; i < steps.Count && i < currentIndex; i++)
            {
                if (steps[i].isCheckpoint)
                {
                    nearestCheckpoint = i;
                }
            }

            return nearestCheckpoint;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 유효성 검사
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(sequenceId))
            {
                sequenceId = name;
            }
        }
#endif
    }
}
