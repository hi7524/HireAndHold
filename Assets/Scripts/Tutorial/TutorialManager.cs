using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 진행을 관리하는 매니저
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        /// <summary>
        /// TutorialBlocker 접근용 (DragManager에서 사용)
        /// </summary>
        public TutorialBlocker TutorialBlocker => tutorialBlocker;

        [Header("시퀀스 목록")]
        [SerializeField] private List<TutorialSequence> sequences = new List<TutorialSequence>();

        [Header("UI 참조")]
        [SerializeField] private TutorialUI tutorialUI;
        [SerializeField] private TutorialBlocker tutorialBlocker;

        [Header("디버그")]
        [SerializeField] private bool debugMode = false;

        // 현재 상태
        private TutorialSequence currentSequence;
        private int currentStepIndex;
        private bool isPlaying;
        private bool isWaitingForAction;

        // 스텝 단위 조건 만족 플래그
        private HashSet<string> metConditions = new HashSet<string>();

        // 이벤트
        public event Action<TutorialSequence> OnSequenceStart;
        public event Action<TutorialSequence> OnSequenceComplete;
        public event Action<TutorialStep> OnStepStart;
        public event Action<TutorialStep> OnStepComplete;

        // 프로퍼티
        public bool IsPlaying => isPlaying;
        public TutorialSequence CurrentSequence => currentSequence;
        public TutorialStep CurrentStep => currentSequence?.GetStep(currentStepIndex);

        /// <summary>
        /// 현재 튜토리얼이 Pause를 요구하는 상태인지 (조건 대기 중이 아닐 때)
        /// conditionKey가 있는 스텝에서 조건 대기 중이면 게임 플레이가 필요하므로 false
        /// </summary>
        public bool RequiresPause
        {
            get
            {
                if (!isPlaying) return false;
                var step = CurrentStep;
                if (step == null) return false;

                // conditionKey가 있으면 게임 플레이가 필요한 상태 (pause 불필요)
                if (!string.IsNullOrEmpty(step.conditionKey)) return false;

                // pauseGame 플래그 확인
                return step.pauseGame;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // DontDestroyOnLoad는 루트 오브젝트에서만 동작
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);

            // 씬 로드 이벤트 구독
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

            // GameEvents 구독
            SubscribeToGameEvents();

            // 현재 씬에서 UI 찾기
            FindTutorialUI();
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeFromGameEvents();
        }

        #region GameEvents 구독

        /// <summary>
        /// GameEvents 구독
        /// </summary>
        private void SubscribeToGameEvents()
        {
            GameEvents.OnLevelUp += HandleLevelUp;
            GameEvents.OnBuffActivated += HandleBuffActivated;
            GameEvents.OnBuffCompleted += HandleBuffCompleted;
            GameEvents.OnFirstBuffActivated += HandleFirstBuffActivated;
            GameEvents.OnMidBossClear += HandleMidBossClear;
            GameEvents.OnDragCompleted += HandleDragCompleted;
        }

        /// <summary>
        /// GameEvents 구독 해제
        /// </summary>
        private void UnsubscribeFromGameEvents()
        {
            GameEvents.OnLevelUp -= HandleLevelUp;
            GameEvents.OnBuffActivated -= HandleBuffActivated;
            GameEvents.OnBuffCompleted -= HandleBuffCompleted;
            GameEvents.OnFirstBuffActivated -= HandleFirstBuffActivated;
            GameEvents.OnMidBossClear -= HandleMidBossClear;
            GameEvents.OnDragCompleted -= HandleDragCompleted;
        }

        /// <summary>
        /// 레벨업 이벤트 처리 - 스텝별 conditionKey 용
        /// </summary>
        private void HandleLevelUp(int level)
        {
            // LEVEL_2, LEVEL_3, LEVEL_4 등의 조건 알림
            string conditionKey = $"LEVEL_{level}";
            DebugLog($"레벨업 조건 알림: {conditionKey}");
            NotifyConditionMet(conditionKey);
        }

        /// <summary>
        /// 버프 활성화 이벤트 처리
        /// </summary>
        private void HandleBuffActivated(string buffName)
        {
            // 현재 스테이지가 버프 튜토리얼 스테이지인지 확인
            int currentStageId = GameEvents.StageManager != null ? GameEvents.StageManager.CurrentStageId : 0;

            if (TutorialOverrides.ShouldNotifyBuffTutorial(currentStageId))
            {
                NotifyConditionMet(TutorialConditions.STAGE_BUFF);
            }
        }

        /// <summary>
        /// 버프 완료 이벤트 처리
        /// </summary>
        private void HandleBuffCompleted(string conditionKey)
        {
            NotifyConditionMet(conditionKey);
        }

        /// <summary>
        /// 첫 버프 획득 이벤트 처리 (703 스테이지용)
        /// </summary>
        private void HandleFirstBuffActivated()
        {
            DebugLog("첫 버프 획득 조건 알림: FIRST_BUFF");
            NotifyConditionMet(TutorialConditions.FIRST_BUFF);
        }

        /// <summary>
        /// 중간 보스 클리어 이벤트 처리 (703 스테이지용)
        /// </summary>
        private void HandleMidBossClear()
        {
            DebugLog("중간 보스 클리어 조건 알림: MID_BOSS_CLEAR");
            NotifyConditionMet(TutorialConditions.MID_BOSS_CLEAR);
        }

        /// <summary>
        /// 드래그 완료 이벤트 처리
        /// </summary>
        private void HandleDragCompleted(string sourceName, string targetName)
        {
            // 현재 튜토리얼이 진행 중이면 드래그 완료 알림
            if (IsPlaying)
            {
                NotifyDragComplete(sourceName, targetName);
            }
        }

        #endregion

        /// <summary>
        /// 씬 로드 시 호출 - UI 재연결 및 튜토리얼 체크
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            DebugLog($"OnSceneLoaded: {scene.name}, isPlaying: {isPlaying}, currentSequence: {currentSequence?.sequenceId}");

            // 씬 전환 시 조건 플래그 초기화 (이전 스테이지 조건이 남아있지 않도록)
            metConditions.Clear();

            // 스테이지 튜토리얼 진행 중 로비로 돌아간 경우 튜토리얼 초기화
            if (isPlaying && currentSequence != null && scene.name == "02_Lobby")
            {
                // 스테이지 관련 튜토리얼인 경우 (OnStageStart, OnLevelUp, OnStageClear 등)
                if (currentSequence.triggerType == TutorialTriggerType.OnStageStart ||
                    currentSequence.triggerType == TutorialTriggerType.OnLevelUp ||
                    (currentSequence.triggerType == TutorialTriggerType.OnCondition && currentSequence.triggerStageId > 0))
                {
                    DebugLog($"스테이지 튜토리얼 진행 중 로비 복귀 - 튜토리얼 초기화: {currentSequence.sequenceId}");
                    CancelCurrentTutorial();
                }
            }

            FindTutorialUI();

            // 씬 로드 후 튜토리얼 자동 체크
            CheckTutorialOnSceneLoadAsync(scene.name).Forget();
        }

        /// <summary>
        /// 씬 로드 후 튜토리얼 자동 체크
        /// </summary>
        private async UniTaskVoid CheckTutorialOnSceneLoadAsync(string sceneName)
        {
            // 약간의 딜레이 (씬 초기화 대기)
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), ignoreTimeScale: true);

            // 03_Stage 씬인 경우에만 OnStageStart 튜토리얼 체크
            if (sceneName == "03_Stage")
            {
                if (GameEvents.StageManager != null)
                {
                    int stageId = GameEvents.StageManager.CurrentStageId;

                    // 튜토리얼 스테이지(701, 703)에서 OnStageStart 튜토리얼 시작
                    if (stageId == 701 || stageId == 703)
                    {
                        await CheckAndStartTutorialAsync(TutorialTriggerType.OnStageStart, stageId);
                    }
                }
            }
            // 02_Lobby 씬인 경우 OnLobbyEnter 튜토리얼 체크
            else if (sceneName == "02_Lobby")
            {
                bool lobbyTutorialCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted(TutorialSequenceIds.LobbyTutorial);

                // 로비 튜토리얼이 완료되지 않은 경우 시작
                if (!lobbyTutorialCompleted)
                {
                    await CheckAndStartTutorialAsync(TutorialTriggerType.OnLobbyEnter);
                }
            }
            // 04_OreDungeon 씬인 경우 던전 튜토리얼 체크
            else if (sceneName == "04_OreDungeon")
            {
                bool dungeonTutorialCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted(TutorialSequenceIds.DungeonTutorial);

                // 던전 튜토리얼이 완료되지 않은 경우 조건 알림
                if (!dungeonTutorialCompleted)
                {
                    NotifyConditionMet(TutorialConditions.DUNGEON_STAGE_FIRST_ENTER);
                }
            }
        }

        /// <summary>
        /// TutorialUI, TutorialBlocker 찾기
        /// </summary>
        private void FindTutorialUI()
        {
            tutorialUI = FindAnyObjectByType<TutorialUI>(FindObjectsInactive.Include);
            tutorialBlocker = FindAnyObjectByType<TutorialBlocker>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            InitializeAsync().Forget();
        }

        /// <summary>
        /// 초기화 - 진행 중인 튜토리얼 복원
        /// </summary>
        private async UniTaskVoid InitializeAsync()
        {
            await DatabaseManager.Instance.WaitForInitializationAsync();

            var progress = DatabaseManager.Instance.GetTutorialProgress();
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            Debug.Log($"[Tutorial] InitializeAsync - currentScene: {currentScene}, currentSequenceId: {progress.currentSequenceId}, lastCheckpointIndex: {progress.lastCheckpointIndex}");

            // 진행 중인 시퀀스가 있으면 복원
            if (!string.IsNullOrEmpty(progress.currentSequenceId))
            {
                var sequence = GetSequenceById(progress.currentSequenceId);
                if (sequence != null)
                {
                    Debug.Log($"[Tutorial] 시퀀스 찾음: {sequence.sequenceId}, triggerType: {sequence.triggerType}");

                    // 현재 씬과 시퀀스 triggerType이 맞는지 확인
                    bool shouldRestore = false;

                    switch (sequence.triggerType)
                    {
                        case TutorialTriggerType.OnStageStart:
                            // Stage 씬에서만 복원
                            shouldRestore = currentScene == "03_Stage";
                            break;
                        case TutorialTriggerType.OnStageClear:
                            // OnStageClear는 Stage에서 시작하지만 Lobby로 넘어갈 수 있음
                            // 둘 다 허용
                            shouldRestore = currentScene == "03_Stage" || currentScene == "02_Lobby";
                            break;
                        case TutorialTriggerType.OnLevelUp:
                            // Stage 씬에서만 복원
                            shouldRestore = currentScene == "03_Stage";
                            break;
                        case TutorialTriggerType.OnLobbyEnter:
                            // Lobby 씬에서만 복원
                            shouldRestore = currentScene == "02_Lobby";
                            break;
                        case TutorialTriggerType.OnCondition:
                            // OnCondition은 stageId로 판단
                            if (sequence.triggerStageId > 0)
                                shouldRestore = currentScene == "03_Stage";
                            else
                                shouldRestore = true;
                            break;
                        default:
                            shouldRestore = true;
                            break;
                    }

                    Debug.Log($"[Tutorial] shouldRestore: {shouldRestore}");

                    if (shouldRestore)
                    {
                        Debug.Log($"[Tutorial] 튜토리얼 복원 시작: {sequence.sequenceId}, 스텝: {progress.lastCheckpointIndex}");
                        int startIndex = progress.lastCheckpointIndex;
                        await StartSequenceFromStepAsync(sequence, startIndex);
                    }
                }
                else
                {
                    Debug.LogWarning($"[Tutorial] 시퀀스를 찾을 수 없음: {progress.currentSequenceId}");
                }
            }
            else
            {
                Debug.Log("[Tutorial] 복원할 진행 중인 시퀀스 없음");
            }
        }

        #region 시퀀스 관리

        /// <summary>
        /// ID로 시퀀스 찾기
        /// </summary>
        public TutorialSequence GetSequenceById(string sequenceId)
        {
            return sequences.Find(s => s.sequenceId == sequenceId);
        }

        /// <summary>
        /// 조건에 맞는 시퀀스 찾기
        /// </summary>
        public TutorialSequence FindSequenceByTrigger(TutorialTriggerType triggerType, int stageId = 0, int level = 0)
        {
            foreach (var sequence in sequences)
            {
                // 이미 완료된 시퀀스는 스킵
                if (DatabaseManager.Instance.IsTutorialSequenceCompleted(sequence.sequenceId))
                    continue;

                if (sequence.triggerType != triggerType)
                    continue;

                // 트리거 조건 확인
                switch (triggerType)
                {
                    case TutorialTriggerType.OnStageStart:
                    case TutorialTriggerType.OnStageClear:
                        if (sequence.triggerStageId == stageId)
                            return sequence;
                        break;

                    case TutorialTriggerType.OnLevelUp:
                        // 레벨 조건 체크
                        if (sequence.triggerLevel != level)
                            break;
                        // stageId가 지정되어 있으면 스테이지도 체크
                        if (sequence.triggerStageId > 0 && sequence.triggerStageId != stageId)
                            break;
                        return sequence;

                    case TutorialTriggerType.OnLobbyEnter:
                        // 로비 튜토리얼은 첫 진입 시 바로 실행
                        return sequence;

                    default:
                        return sequence;
                }
            }

            return null;
        }

        /// <summary>
        /// 튜토리얼 체크 및 시작 (외부에서 호출)
        /// </summary>
        public async UniTask<bool> CheckAndStartTutorialAsync(TutorialTriggerType triggerType, int stageId = 0, int level = 0)
        {
            Debug.Log($"[Tutorial] CheckAndStartTutorialAsync - triggerType: {triggerType}, stageId: {stageId}, level: {level}, isPlaying: {isPlaying}, sequences.Count: {sequences.Count}");

            if (isPlaying)
            {
                Debug.Log("[Tutorial] 튜토리얼 진행 중이라 스킵");
                return false;
            }

            // 전체 튜토리얼 완료됐으면 스킵
            if (DatabaseManager.Instance.IsTutorialCompleted())
            {
                Debug.Log("[Tutorial] 전체 튜토리얼 완료됨, 스킵");
                return false;
            }

            var sequence = FindSequenceByTrigger(triggerType, stageId, level);
            if (sequence == null)
            {
                Debug.Log($"[Tutorial] triggerType: {triggerType}, stageId: {stageId}에 맞는 시퀀스 없음. 등록된 시퀀스:");
                foreach (var seq in sequences)
                {
                    bool completed = DatabaseManager.Instance.IsTutorialSequenceCompleted(seq.sequenceId);
                    Debug.Log($"  - {seq.sequenceId}: triggerType={seq.triggerType}, stageId={seq.triggerStageId}, completed={completed}");
                }
                return false;
            }

            Debug.Log($"[Tutorial] 시퀀스 찾음: {sequence.sequenceId}");
            await StartSequenceAsync(sequence);
            return true;
        }

        /// <summary>
        /// 시퀀스 시작
        /// </summary>
        public async UniTask StartSequenceAsync(TutorialSequence sequence)
        {
            await StartSequenceFromStepAsync(sequence, 0);
        }

        /// <summary>
        /// 특정 스텝부터 시퀀스 시작
        /// </summary>
        public async UniTask StartSequenceFromStepAsync(TutorialSequence sequence, int startStepIndex)
        {
            if (sequence == null || isPlaying) return;

            currentSequence = sequence;
            currentStepIndex = startStepIndex;
            isPlaying = true;

            DebugLog($"시퀀스 시작: {sequence.sequenceId}, 스텝: {startStepIndex}");

            // Firebase에 시작 기록
            await DatabaseManager.Instance.StartTutorialSequenceAsync(sequence.sequenceId);

            OnSequenceStart?.Invoke(sequence);

            // 첫 스텝 시작
            await PlayCurrentStepAsync();
        }

        /// <summary>
        /// 시퀀스 완료
        /// </summary>
        private async UniTask CompleteSequenceAsync()
        {
            if (currentSequence == null) return;

            DebugLog($"시퀀스 완료: {currentSequence.sequenceId}");

            // Firebase에 완료 기록
            await DatabaseManager.Instance.CompleteTutorialSequenceAsync(currentSequence.sequenceId);

            OnSequenceComplete?.Invoke(currentSequence);

            // UI 정리
            if (tutorialUI != null)
            {
                tutorialUI.Hide();
            }
            if (tutorialBlocker != null)
            {
                tutorialBlocker.Unblock();
            }

            // 게임 재개
            ResumeGame();

            var completedSequence = currentSequence;
            currentSequence = null;
            currentStepIndex = 0;
            isPlaying = false;

            // 모든 시퀀스 완료 체크
            CheckAllSequencesCompleted();

            // 시퀀스 완료 후 대기 중인 조건 체크
            CheckPendingConditions();
        }

        /// <summary>
        /// 대기 중인 조건 체크 (시퀀스 완료 후 호출)
        /// </summary>
        private void CheckPendingConditions()
        {
            if (isPlaying) return;

            // metConditions에 저장된 조건들을 다시 체크
            foreach (var conditionKey in metConditions)
            {
                // 현재 스테이지 ID 확인
                int currentStageId = GameEvents.StageManager != null ? GameEvents.StageManager.CurrentStageId : 0;

                foreach (var sequence in sequences)
                {
                    if (DatabaseManager.Instance.IsTutorialSequenceCompleted(sequence.sequenceId))
                        continue;

                    if (sequence.triggerType == TutorialTriggerType.OnCondition &&
                        sequence.triggerConditionKey == conditionKey)
                    {
                        if (sequence.triggerStageId > 0 && sequence.triggerStageId != currentStageId)
                            continue;

                        DebugLog($"대기 중인 조건 {conditionKey}에 맞는 시퀀스 발견: {sequence.sequenceId}");
                        StartSequenceAsync(sequence).Forget();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 모든 시퀀스 완료 여부 체크
        /// </summary>
        private void CheckAllSequencesCompleted()
        {
            foreach (var sequence in sequences)
            {
                if (sequence.isRequired && !DatabaseManager.Instance.IsTutorialSequenceCompleted(sequence.sequenceId))
                {
                    return; // 아직 완료 안 된 필수 시퀀스가 있음
                }
            }

            // 모든 필수 시퀀스 완료
            DatabaseManager.Instance.CompleteTutorialAsync().Forget();
            DebugLog("전체 튜토리얼 완료!");
        }

        #endregion

        #region 스텝 진행

        // 씬 전환으로 시퀀스가 조기 완료되었는지 여부
        private bool sequenceCompletedEarly = false;

        /// <summary>
        /// 현재 스텝 재생
        /// </summary>
        private async UniTask PlayCurrentStepAsync()
        {
            var step = CurrentStep;
            Debug.Log($"[Tutorial] PlayCurrentStepAsync - stepIndex: {currentStepIndex}, stringId: {step?.stringId}, actionType: {step?.actionType}, conditionKey: {step?.conditionKey}, timeScale: {Time.timeScale}");

            if (step == null)
            {
                await CompleteSequenceAsync();
                return;
            }

            // conditionKey가 있고 WaitCondition이 아닌 경우: 조건 충족까지 대기 후 UI 표시
            // WaitCondition인 경우: UI 없이 조건 대기 후 바로 다음 스텝
            if (!string.IsNullOrEmpty(step.conditionKey))
            {
                DebugLog($"스텝 시작 전 조건 대기: {step.conditionKey}");

                // UI 숨기기
                if (tutorialUI != null)
                {
                    tutorialUI.Hide();
                }

                // 블로커 해제
                if (tutorialBlocker != null)
                {
                    tutorialBlocker.Unblock();
                }

                // 게임 재개 (조건 충족을 위해 플레이 필요)
                ResumeGame();

                await WaitForConditionAsync(step.conditionKey);

                DebugLog($"조건 충족됨: {step.conditionKey}");

                // WaitCondition은 조건 충족 후 바로 다음 스텝으로 (UI 표시 없이)
                if (step.actionType == TutorialActionType.WaitCondition)
                {
                    await CompleteCurrentStepAsync();
                    return;
                }
            }

            // 스텝 시작 전 대기 (UI 활성화 대기용)
            if (step.delayBeforeStep > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(step.delayBeforeStep), ignoreTimeScale: true);
            }

            // 게임 일시정지
            if (step.pauseGame)
            {
                PauseGame();
            }

            // 블로커 설정
            SetupBlocker(step);

            // UI 표시
            await ShowStepUIAsync(step);

            OnStepStart?.Invoke(step);

            // 액션 대기
            isWaitingForAction = true;
            sequenceCompletedEarly = false;

            await WaitForActionAsync(step);
            isWaitingForAction = false;

            // 씬 전환 버튼으로 조기 완료된 경우 스텝 완료 처리 스킵
            if (sequenceCompletedEarly)
            {
                DebugLog("씬 전환으로 시퀀스 조기 완료됨");
                return;
            }

            // 스텝 완료 처리
            await CompleteCurrentStepAsync();
        }

        /// <summary>
        /// 스텝 UI 표시
        /// </summary>
        private async UniTask ShowStepUIAsync(TutorialStep step)
        {
            if (tutorialUI == null) return;

            // stringId가 유효한 경우에만 대화창 표시
            if (step.stringId > 0)
            {
                // 텍스트 가져오기 (TutorialTable에서)
                string text = DataTableManager.TutorialTable?.GetText(step.stringId);

                // 텍스트가 있으면 대화창 표시
                if (!string.IsNullOrEmpty(text) && text != $"[{step.stringId}]")
                {
                    // 보이스 먼저 재생 (Addressable 캐시에서 로드)
                    // voiceKey가 없으면 TutorialTable에서 stringId로 자동 조회
                    string voiceKey = step.voiceKey;
                    if (string.IsNullOrEmpty(voiceKey))
                    {
                        voiceKey = DataTableManager.TutorialTable?.GetVoiceKey(step.stringId);
                    }

                    if (!string.IsNullOrEmpty(voiceKey))
                    {
                        var voiceClip = AddressablePreloader.Instance?.GetCachedTutorialVoice(voiceKey);
                        if (voiceClip != null)
                        {
                            SoundManager.Instance?.PlayVoice(voiceClip);
                        }
                        else
                        {
                            Debug.LogWarning($"[Tutorial] 튜토리얼 보이스 클립을 찾을 수 없음: {voiceKey}");
                        }
                    }

                    // 보이스 재생 후 텍스트 표시
                    await tutorialUI.ShowDialogAsync(text, step.dialogAnchor, step.dialogPosition, step.showCharacter);
                }
                else
                {
                    // 텍스트 없으면 대화창 숨기기
                    tutorialUI.HideDialog();
                }
            }
            else
            {
                // stringId가 0이면 대화창 숨기기
                tutorialUI.HideDialog();
            }

            // 하이라이트 표시
            bool hasHighlight = false;

            if (!string.IsNullOrEmpty(step.highlightTarget))
            {
                tutorialUI.ShowHighlight(step.highlightTarget, step.highlightOffset, step.highlightSize);
                hasHighlight = true;
            }
            else if (step.highlightUnitId > 0)
            {
                // UnitId 기반 하이라이트 (동적 생성 유닛용)
                tutorialUI.ShowHighlightByUnitId(step.highlightUnitId, step.highlightOffset, step.highlightSize);
                hasHighlight = true;
            }
            else if (step.highlightTilePositions != null && step.highlightTilePositions.Length > 0)
            {
                // 타일 좌표 기반 하이라이트 (그리드 셀용)
                tutorialUI.ShowHighlightAtTilePositions(step.highlightTilePositions, step.highlightOffset, step.highlightSize);
                hasHighlight = true;
            }

            if (!hasHighlight)
            {
                tutorialUI.HideHighlight();
            }

            // 두 번째 하이라이트 표시
            bool hasHighlight2 = false;

            if (!string.IsNullOrEmpty(step.highlightTarget2))
            {
                tutorialUI.ShowHighlight2(step.highlightTarget2, step.highlightOffset2, step.highlightSize2);
                hasHighlight2 = true;
            }
            else if (step.highlightTilePositions2 != null && step.highlightTilePositions2.Length > 0)
            {
                tutorialUI.ShowHighlightAtTilePositions2(step.highlightTilePositions2, step.highlightOffset2, step.highlightSize2);
                hasHighlight2 = true;
            }

            if (!hasHighlight2)
            {
                tutorialUI.HideHighlight2();
            }

            // 손가락 가이드 표시
            if (step.showDragGuide)
            {
                // 드래그 가이드 (하이라이트1 → 하이라이트2 이동)
                tutorialUI.ShowDragGuide(step.dragGuideOffset1, step.dragGuideOffset2);
            }
            else if (step.showHandGuide)
            {
                tutorialUI.ShowHandGuide(step.handGuideOffset);
            }
            else
            {
                tutorialUI.HideHandGuide();
            }
        }

        /// <summary>
        /// 블로커 설정
        /// </summary>
        private void SetupBlocker(TutorialStep step)
        {
            if (tutorialBlocker == null) return;

            tutorialBlocker.Block();

            // 액션 타입에 따라 허용 영역 설정
            switch (step.actionType)
            {
                case TutorialActionType.Touch:
                    // 대화창만 터치 가능
                    tutorialBlocker.AllowDialogOnly();
                    break;

                case TutorialActionType.TouchTarget:
                    // 대화창 + 특정 버튼
                    tutorialBlocker.AllowTarget(step.targetButtonName);
                    break;

                case TutorialActionType.DragToPosition:
                    // 드래그 소스와 타겟 허용
                    tutorialBlocker.AllowDrag(step.dragSourceName, step.dragTargetName, step.allowedTiles, step.allowedUnitIds);
                    break;

                case TutorialActionType.WaitAuto:
                case TutorialActionType.WaitCondition:
                    // 대화창만
                    tutorialBlocker.AllowDialogOnly();
                    break;
            }
        }

        /// <summary>
        /// 액션 대기
        /// </summary>
        private async UniTask WaitForActionAsync(TutorialStep step)
        {
            switch (step.actionType)
            {
                case TutorialActionType.Touch:
                    await WaitForTouchAsync();
                    break;

                case TutorialActionType.TouchTarget:
                    await WaitForTargetTouchAsync(step.targetButtonName);
                    break;

                case TutorialActionType.DragToPosition:
                    await WaitForDragAsync(step.dragSourceName, step.dragTargetName);
                    break;

                case TutorialActionType.WaitAuto:
                    await WaitForAutoAsync(step.autoAdvanceDelay);
                    break;

                case TutorialActionType.WaitCondition:
                    await WaitForConditionAsync(step.conditionKey);
                    break;
            }
        }

        /// <summary>
        /// 터치 대기
        /// </summary>
        private async UniTask WaitForTouchAsync()
        {
            // TutorialUI에서 터치 이벤트 대기
            if (tutorialUI != null)
            {
                await tutorialUI.WaitForTouchAsync();
            }
        }

        /// <summary>
        /// 특정 버튼 터치 대기
        /// </summary>
        private async UniTask WaitForTargetTouchAsync(string targetName)
        {
            if (tutorialBlocker != null)
            {
                // 씬 전환 버튼인 경우 특별 처리
                if (IsSceneTransitionButton(targetName))
                {
                    // 버튼 터치 대기
                    await tutorialBlocker.WaitForTargetTouchAsync(targetName);

                    // 씬 전환 전에 시퀀스 완료 처리 (await 없이 즉시 실행 - 씬 전환 전에 로컬 저장 보장)
                    DebugLog($"씬 전환 버튼 클릭됨: {targetName}, 시퀀스 즉시 완료 처리");

                    // 현재 시퀀스 완료 처리 (동기적으로 로컬 저장, Firebase는 비동기)
                    if (currentSequence != null)
                    {
                        string sequenceId = currentSequence.sequenceId;
                        DatabaseManager.Instance.CompleteTutorialSequenceAsync(sequenceId).Forget();
                    }

                    // UI 정리
                    if (tutorialUI != null)
                    {
                        tutorialUI.Hide();
                    }
                    tutorialBlocker.Unblock();

                    // 게임 재개
                    ResumeGame();

                    // 상태 초기화
                    var completedSequence = currentSequence;
                    currentSequence = null;
                    currentStepIndex = 0;
                    isPlaying = false;

                    OnSequenceComplete?.Invoke(completedSequence);

                    // 모든 시퀀스 완료 체크
                    CheckAllSequencesCompleted();

                    // 플래그 설정으로 CompleteCurrentStepAsync 실행 방지
                    sequenceCompletedEarly = true;
                }
                else
                {
                    await tutorialBlocker.WaitForTargetTouchAsync(targetName);
                }
            }
        }

        /// <summary>
        /// 씬 전환을 유발하는 버튼인지 확인
        /// </summary>
        private bool IsSceneTransitionButton(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName)) return false;

            // 씬 전환을 유발하는 버튼 목록
            return buttonName == "LobbyButton" ||      // Stage → Lobby
                   buttonName == "StartButton" ||      // Stage Select → Stage
                   buttonName == "HomeButton" ||       // Any → Lobby
                   buttonName == "DungeonEnterButton"; // Lobby → Dungeon
        }

        /// <summary>
        /// 드래그 대기
        /// </summary>
        private async UniTask WaitForDragAsync(string sourceName, string targetName)
        {
            if (tutorialBlocker != null)
            {
                await tutorialBlocker.WaitForDragCompleteAsync(sourceName, targetName);
            }
        }

        /// <summary>
        /// 자동 진행 대기
        /// </summary>
        private async UniTask WaitForAutoAsync(float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), ignoreTimeScale: true);
        }

        /// <summary>
        /// 조건 대기
        /// </summary>
        private async UniTask WaitForConditionAsync(string conditionKey)
        {
            // TODO: 조건 시스템 구현
            // 예: COMPLETE_BUFF, STAGE_CLEAR 등
            await UniTask.WaitUntil(() => CheckCondition(conditionKey));
        }

        /// <summary>
        /// 조건 체크
        /// </summary>
        private bool CheckCondition(string conditionKey)
        {
            // metConditions에서 조건 확인
            return metConditions.Contains(conditionKey);
        }

        /// <summary>
        /// 현재 스텝 완료 처리
        /// </summary>
        private async UniTask CompleteCurrentStepAsync()
        {
            var step = CurrentStep;
            if (step == null) return;

            DebugLog($"스텝 완료: {step.stringId}");

            // UI 먼저 숨기기 (딜레이 방지)
            if (tutorialUI != null)
            {
                tutorialUI.HideDialog();
                tutorialUI.HideHighlight();
                tutorialUI.HideHighlight2();
                tutorialUI.HideHandGuide();
            }

            // 보상 지급
            if (step.reward != null && step.reward.HasReward)
            {
                await GiveRewardAsync(step.reward);
            }

            // 체크포인트면 Firebase 저장
            bool isCheckpoint = step.isCheckpoint;
            await DatabaseManager.Instance.UpdateTutorialStepAsync(currentStepIndex, isCheckpoint);

            OnStepComplete?.Invoke(step);

            // 다음 스텝으로
            currentStepIndex++;

            if (currentStepIndex < currentSequence.StepCount)
            {
                await PlayCurrentStepAsync();
            }
            else
            {
                await CompleteSequenceAsync();
            }
        }

        /// <summary>
        /// 보상 지급
        /// </summary>
        private async UniTask GiveRewardAsync(TutorialReward reward)
        {
            switch (reward.rewardType)
            {
                case TutorialRewardType.Credit:
                    // 인게임 크레딧 지급
                    if (GameEvents.PlayerGold != null)
                    {
                        GameEvents.PlayerGold.AddCredit(reward.amount);
                    }
                    break;

                case TutorialRewardType.Gold:
                    await DatabaseManager.Instance.AddGoldAsync(reward.amount);
                    break;

                case TutorialRewardType.EnhanceStone:
                    await DatabaseManager.Instance.AddEnhanceStoneAsync(reward.amount);
                    break;

                case TutorialRewardType.SummonTicket:
                    // itemId가 지정되어 있으면 사용, 없으면 기본 일반 소환권(5102) 사용
                    int ticketId = reward.itemId > 0 ? reward.itemId : 5102;
                    await DatabaseManager.Instance.AddItemAsync(ticketId, reward.amount);
                    break;

                case TutorialRewardType.Item:
                    await DatabaseManager.Instance.AddItemAsync(reward.itemId, reward.amount);
                    break;
            }

            DebugLog($"보상 지급: {reward.rewardType} x{reward.amount}");
        }

        #endregion

        #region 게임 제어

        private void PauseGame()
        {
            // GameManager가 있으면 사용, 없으면 직접 TimeScale 제어
            if (GameEvents.GameManager != null)
            {
                GameEvents.GameManager.PauseGame();
            }
            else
            {
                Time.timeScale = 0f;
            }
        }

        private void ResumeGame()
        {
            if (GameEvents.GameManager != null)
            {
                GameEvents.GameManager.ResumeGame();
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        #endregion

        #region 외부 호출용

        /// <summary>
        /// 스테이지 변경 시 조건 초기화 (StageManager에서 호출)
        /// </summary>
        public void OnStageChanged()
        {
            metConditions.Clear();
        }

        /// <summary>
        /// 조건 만족 알림 (외부에서 호출)
        /// </summary>
        public void NotifyConditionMet(string conditionKey)
        {
            // 스텝 단위 조건 저장 (WaitCondition용)
            metConditions.Add(conditionKey);

            // 전체 튜토리얼 완료됐으면 스킵
            if (DatabaseManager.Instance.IsTutorialCompleted())
                return;

            // 현재 스테이지 ID 확인
            int currentStageId = GameEvents.StageManager != null ? GameEvents.StageManager.CurrentStageId : 0;

            // OnCondition 타입이고 conditionKey가 일치하는 시퀀스 찾기
            foreach (var sequence in sequences)
            {
                // 이미 완료된 시퀀스는 스킵
                if (DatabaseManager.Instance.IsTutorialSequenceCompleted(sequence.sequenceId))
                    continue;

                if (sequence.triggerType == TutorialTriggerType.OnCondition &&
                    sequence.triggerConditionKey == conditionKey)
                {
                    // triggerStageId가 지정되어 있으면 스테이지 ID도 체크
                    if (sequence.triggerStageId > 0 && sequence.triggerStageId != currentStageId)
                        continue;

                    StartSequenceAsync(sequence).Forget();
                    return;
                }
            }
        }

        /// <summary>
        /// 드래그 완료 알림 (외부에서 호출)
        /// </summary>
        public void NotifyDragComplete(string sourceName, string targetName)
        {
            tutorialBlocker?.NotifyDragComplete(sourceName, targetName);
        }

        /// <summary>
        /// 버튼 터치 알림 (외부에서 호출)
        /// </summary>
        public void NotifyButtonTouched(string buttonName)
        {
            tutorialBlocker?.NotifyButtonTouched(buttonName);
        }

        /// <summary>
        /// 스킵 (canSkip이 true인 경우만)
        /// </summary>
        public async UniTask SkipCurrentSequenceAsync()
        {
            if (!isPlaying || currentSequence == null) return;
            if (!currentSequence.canSkip) return;

            await CompleteSequenceAsync();
        }

        /// <summary>
        /// 현재 튜토리얼 취소 (씬 전환 등으로 인한 강제 종료)
        /// 완료 처리하지 않고 상태만 초기화하여 다음에 다시 시작할 수 있도록 함
        /// </summary>
        public void CancelCurrentTutorial()
        {
            if (!isPlaying || currentSequence == null) return;

            DebugLog($"튜토리얼 취소: {currentSequence.sequenceId}");

            // UI 정리
            if (tutorialUI != null)
            {
                tutorialUI.Hide();
            }
            if (tutorialBlocker != null)
            {
                tutorialBlocker.Unblock();
            }

            // 게임 재개
            ResumeGame();

            // Firebase 진행 상태 초기화 (완료 처리하지 않음)
            DatabaseManager.Instance.CancelTutorialSequenceAsync(currentSequence.sequenceId).Forget();

            // 상태 초기화
            currentSequence = null;
            currentStepIndex = 0;
            isPlaying = false;
            isWaitingForAction = false;
        }

        #endregion

        #region 디버그

        private void DebugLog(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[Tutorial] {message}");
            }
        }

        /// <summary>
        /// 디버그: 튜토리얼 초기화
        /// </summary>
        [ContextMenu("Reset Tutorial Progress")]
        public void DebugResetTutorial()
        {
            DatabaseManager.Instance.ResetTutorialProgressAsync().Forget();
        }

        /// <summary>
        /// 디버그: 특정 시퀀스 강제 시작
        /// </summary>
        public void DebugStartSequence(string sequenceId)
        {
            var sequence = GetSequenceById(sequenceId);
            if (sequence != null)
            {
                StartSequenceAsync(sequence).Forget();
            }
        }

        #endregion
    }
}
