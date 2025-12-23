using System.Collections.Generic;

namespace Tutorial
{
    /// <summary>
    /// Firebase에 저장되는 튜토리얼 진행 상황 데이터
    /// </summary>
    [System.Serializable]
    public class TutorialProgress
    {
        /// <summary>
        /// 전체 튜토리얼 완료 여부
        /// </summary>
        public bool isCompleted;

        /// <summary>
        /// 완료한 시퀀스 ID 목록
        /// </summary>
        public List<string> completedSequences = new List<string>();

        /// <summary>
        /// 현재 진행 중인 시퀀스 ID (없으면 null 또는 빈 문자열)
        /// </summary>
        public string currentSequenceId;

        /// <summary>
        /// 현재 시퀀스에서 진행 중인 스텝 인덱스
        /// </summary>
        public int currentStepIndex;

        /// <summary>
        /// 마지막 체크포인트 인덱스 (게임 종료 시 여기부터 재시작)
        /// </summary>
        public int lastCheckpointIndex;

        /// <summary>
        /// 특정 시퀀스가 완료되었는지 확인
        /// </summary>
        public bool IsSequenceCompleted(string sequenceId)
        {
            return completedSequences != null && completedSequences.Contains(sequenceId);
        }

        /// <summary>
        /// 시퀀스 완료 처리
        /// </summary>
        public void CompleteSequence(string sequenceId)
        {
            if (completedSequences == null)
                completedSequences = new List<string>();

            if (!completedSequences.Contains(sequenceId))
            {
                completedSequences.Add(sequenceId);
            }

            // 현재 진행 중인 시퀀스였다면 초기화
            if (currentSequenceId == sequenceId)
            {
                currentSequenceId = null;
                currentStepIndex = 0;
                lastCheckpointIndex = 0;
            }
        }

        /// <summary>
        /// 새 시퀀스 시작
        /// </summary>
        public void StartSequence(string sequenceId)
        {
            currentSequenceId = sequenceId;
            currentStepIndex = 0;
            lastCheckpointIndex = 0;
        }

        /// <summary>
        /// 체크포인트에서 재시작 (게임 재접속 시)
        /// </summary>
        public void RestoreFromCheckpoint()
        {
            currentStepIndex = lastCheckpointIndex;
        }

        /// <summary>
        /// 체크포인트 갱신
        /// </summary>
        public void UpdateCheckpoint(int stepIndex)
        {
            lastCheckpointIndex = stepIndex;
        }

        /// <summary>
        /// 다음 스텝으로 진행
        /// </summary>
        public void AdvanceStep()
        {
            currentStepIndex++;
        }

        /// <summary>
        /// 진행 상황 초기화 (테스트/디버그용)
        /// </summary>
        public void Reset()
        {
            isCompleted = false;
            completedSequences?.Clear();
            currentSequenceId = null;
            currentStepIndex = 0;
            lastCheckpointIndex = 0;
        }
    }
}
