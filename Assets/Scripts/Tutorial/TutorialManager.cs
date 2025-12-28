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

            // 현재 씬에서 UI 찾기
            FindTutorialUI();
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// 씬 로드 시 호출 - UI 재연결 및 튜토리얼 체크
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            DebugLog($"OnSceneLoaded: {scene.name}, isPlaying: {isPlaying}, currentSequence: {currentSequence?.sequenceId}");

            // 씬 전환 시 조건 플래그 초기화 (이전 스테이지 조건이 남아있지 않도록)
            metConditions.Clear();

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
                var stageManager = FindAnyObjectByType<StageManager>();
                if (stageManager != null)
                {
                    int stageId = stageManager.CurrentStageId;

                    // 튜토리얼 스테이지(701, 703, 704)에서 OnStageStart 튜토리얼 시작
                    if (stageId == 701 || stageId == 703 || stageId == 704)
                    {
                        await CheckAndStartTutorialAsync(TutorialTriggerType.OnStageStart, stageId);
                    }
                }
            }
            // 02_Lobby 씬인 경우 OnLobbyEnter 튜토리얼 체크
            else if (sceneName == "02_Lobby")
            {
                bool stage1ClearCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted("forced_03_stage1clear");
                bool lobbyTutorialCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted("forced_05_lobbytutorial");
                bool gachaCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted("forced_05_gacha");
                bool gachaPart2Completed = DatabaseManager.Instance.IsTutorialSequenceCompleted("forced_05_gacha_part2");
                bool enhanceCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted("enhance_tutorial");
                bool enhancePart2Completed = DatabaseManager.Instance.IsTutorialSequenceCompleted("enhance_part2");

                // Stage1Clear 튜토리얼이 완료된 경우에만 로비 튜토리얼 시작
                if (stage1ClearCompleted && !lobbyTutorialCompleted)
                {
                    await CheckAndStartTutorialAsync(TutorialTriggerType.OnLobbyEnter);
                }
                // 뽑기 파트1이 완료되었고 파트2가 완료되지 않은 경우 뽑기 파트2 시작
                else if (gachaCompleted && !gachaPart2Completed)
                {
                    await CheckAndStartTutorialAsync(TutorialTriggerType.OnLobbyEnter);
                }
                // 강화 파트1이 완료되었고 파트2가 완료되지 않은 경우 강화 파트2 시작
                else if (enhanceCompleted && !enhancePart2Completed)
                {
                    await CheckAndStartTutorialAsync(TutorialTriggerType.OnLobbyEnter);
                }
            }
            // 04_OreDungeon 씬인 경우 던전 파트2 튜토리얼 체크
            else if (sceneName == "04_OreDungeon")
            {
                bool dungeonTutorialCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted("dungeon_tutorial");
                bool dungeonPart2Completed = DatabaseManager.Instance.IsTutorialSequenceCompleted("dungeon_part2");

                // 던전 파트1이 완료되었고 파트2가 완료되지 않은 경우 던전 파트2 시작
                if (dungeonTutorialCompleted && !dungeonPart2Completed)
                {
                    NotifyConditionMet("DUNGEON_STAGE_FIRST_ENTER");
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

            // 진행 중인 시퀀스가 있으면 복원
            if (!string.IsNullOrEmpty(progress.currentSequenceId))
            {
                var sequence = GetSequenceById(progress.currentSequenceId);
                if (sequence != null)
                {
                    // 현재 씬과 시퀀스 triggerType이 맞는지 확인
                    string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
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
                            // 로비 튜토리얼은 Stage1Clear가 완료된 후에만 복원
                            if (sequence.sequenceId == "forced_05_lobbytutorial")
                            {
                                shouldRestore = currentScene == "02_Lobby" &&
                                    DatabaseManager.Instance.IsTutorialSequenceCompleted("forced_03_stage1clear");
                            }
                            else
                            {
                                shouldRestore = currentScene == "02_Lobby";
                            }
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

                    if (shouldRestore)
                    {
                        DebugLog($"튜토리얼 복원: {sequence.sequenceId}, 스텝: {progress.lastCheckpointIndex}");
                        int startIndex = progress.lastCheckpointIndex;
                        await StartSequenceFromStepAsync(sequence, startIndex);
                    }
                }
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
                        // 로비 튜토리얼은 Stage1Clear가 완료된 후에만 실행
                        if (sequence.sequenceId == "forced_05_lobbytutorial")
                        {
                            if (DatabaseManager.Instance.IsTutorialSequenceCompleted("forced_03_stage1clear"))
                                return sequence;
                        }
                        // 뽑기 파트2는 뽑기 파트1이 완료된 후에만 실행
                        else if (sequence.sequenceId == "forced_05_gacha_part2")
                        {
                            if (DatabaseManager.Instance.IsTutorialSequenceCompleted("forced_05_gacha"))
                                return sequence;
                        }
                        // 강화 파트2는 강화 파트1이 완료된 후에만 실행
                        else if (sequence.sequenceId == "enhance_part2")
                        {
                            if (DatabaseManager.Instance.IsTutorialSequenceCompleted("enhance_tutorial"))
                                return sequence;
                        }
                        else
                        {
                            return sequence;
                        }
                        break;

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
            DebugLog($"CheckAndStartTutorialAsync - triggerType: {triggerType}, stageId: {stageId}, level: {level}, isPlaying: {isPlaying}");

            if (isPlaying)
            {
                DebugLog("튜토리얼 진행 중이라 스킵");
                return false;
            }

            // 전체 튜토리얼 완료됐으면 스킵
            if (DatabaseManager.Instance.IsTutorialCompleted())
            {
                DebugLog("전체 튜토리얼 완료됨, 스킵");
                return false;
            }

            var sequence = FindSequenceByTrigger(triggerType, stageId, level);
            if (sequence == null)
            {
                DebugLog($"triggerType: {triggerType}, stageId: {stageId}에 맞는 시퀀스 없음");
                return false;
            }

            DebugLog($"시퀀스 찾음: {sequence.sequenceId}");
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
                int currentStageId = 0;
                var stageManager = FindAnyObjectByType<StageManager>();
                if (stageManager != null)
                {
                    currentStageId = stageManager.CurrentStageId;
                }

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

        /// <summary>
        /// 현재 스텝 재생
        /// </summary>
        private async UniTask PlayCurrentStepAsync()
        {
            var step = CurrentStep;
            if (step == null)
            {
                await CompleteSequenceAsync();
                return;
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
            try
            {
                await WaitForActionAsync(step);
                isWaitingForAction = false;

                // 스텝 완료 처리
                await CompleteCurrentStepAsync();
            }
            catch (System.OperationCanceledException)
            {
                // 씬 전환 버튼 클릭으로 시퀀스가 조기 완료됨
                isWaitingForAction = false;
                DebugLog("씬 전환으로 시퀀스 조기 완료됨");
            }
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
                            SoundManager.Instance?.PlaySFX(voiceClip);
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
            if (!string.IsNullOrEmpty(step.highlightTarget))
            {
                tutorialUI.ShowHighlight(step.highlightTarget, step.highlightOffset, step.highlightSize);
            }
            else
            {
                tutorialUI.HideHighlight();
            }

            // 손가락 가이드 표시
            if (step.showHandGuide)
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
                    tutorialBlocker.AllowDrag(step.dragSourceName, step.dragTargetName, step.allowedTiles, step.allowedUnitNames);
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

                    // 여기서 예외를 던져서 CompleteCurrentStepAsync가 실행되지 않도록 함
                    throw new System.OperationCanceledException("Scene transition button clicked - sequence completed early");
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

            // 보상 지급
            if (step.reward != null && step.reward.HasReward)
            {
                await GiveRewardAsync(step.reward);
            }

            // 체크포인트면 Firebase 저장
            bool isCheckpoint = step.isCheckpoint;
            await DatabaseManager.Instance.UpdateTutorialStepAsync(currentStepIndex, isCheckpoint);

            OnStepComplete?.Invoke(step);

            // UI 숨기기
            if (tutorialUI != null)
            {
                tutorialUI.HideDialog();
                tutorialUI.HideHighlight();
                tutorialUI.HideHandGuide();
            }

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
                    var playerGold = FindObjectOfType<PlayerStageGold>();
                    if (playerGold != null)
                    {
                        playerGold.AddCredit(reward.amount);
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
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.PauseGame();
            }
            else
            {
                Time.timeScale = 0f;
            }
        }

        private void ResumeGame()
        {
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.ResumeGame();
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
            int currentStageId = 0;
            var stageManager = FindAnyObjectByType<StageManager>();
            if (stageManager != null)
            {
                currentStageId = stageManager.CurrentStageId;
            }

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
