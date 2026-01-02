using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameData;
using Tutorial;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private GameObject stageButton;

    private void Awake()
    {
        // 튜토리얼 타겟 등록
        if (stageButton != null)
        {
            TutorialTargetRegistry.Register("StageButton", stageButton);
        }
    }

    private void OnDestroy()
    {
        TutorialTargetRegistry.Unregister("StageButton");
    }

    private void Start()
    {
        Time.timeScale = 1f;

        // 로비 BGM 재생
        PlayLobbyBGM();

        // 튜토리얼 체크 및 시작
        CheckTutorialAsync().Forget();
    }

    private void PlayLobbyBGM()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[LobbyManager] SoundManager.Instance is null");
            return;
        }

        if (AddressablePreloader.Instance == null)
        {
            Debug.LogWarning("[LobbyManager] AddressablePreloader.Instance is null");
            return;
        }

        var lobbyBGM = AddressablePreloader.Instance.GetCachedAudioClip("LobbyBGM");
        if (lobbyBGM != null)
        {
            Debug.Log("[LobbyManager] Playing LobbyBGM");
            SoundManager.Instance.PlayBGMWithFadeIn(lobbyBGM, 1f);
        }
        else
        {
            Debug.LogWarning("[LobbyManager] LobbyBGM AudioClip not found in cache");
        }
    }

    private async UniTaskVoid CheckTutorialAsync()
    {
        if (TutorialManager.Instance == null) return;

        // 로비 진입 튜토리얼 먼저 체크
        bool lobbyTutorialStarted = await TutorialManager.Instance.CheckAndStartTutorialAsync(TutorialTriggerType.OnLobbyEnter);

        // 로비 튜토리얼이 시작되지 않았으면 다른 튜토리얼 체크
        if (!lobbyTutorialStarted)
        {
            // 1스테이지 클리어 → 뽑기 튜토리얼
            bool stage1ClearCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted(TutorialSequenceIds.Stage1Clear);
            bool gachaTutorialCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted(TutorialSequenceIds.GachaTutorial);

            if (stage1ClearCompleted && !gachaTutorialCompleted)
            {
                Debug.Log("[LobbyManager] 1스테이지 클리어 완료 → 뽑기 튜토리얼 시작");
                TutorialManager.Instance.NotifyConditionMet(TutorialConditions.GACHA_TUTORIAL);
                return;
            }

            // 3스테이지 클리어 → 강화 튜토리얼
            bool stage3ClearCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted(TutorialSequenceIds.Stage3Clear);
            bool enhanceTutorialCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted(TutorialSequenceIds.EnhanceTutorial);

            if (stage3ClearCompleted && !enhanceTutorialCompleted)
            {
                Debug.Log("[LobbyManager] 3스테이지 클리어 완료 → 강화 튜토리얼 시작");
                TutorialManager.Instance.NotifyConditionMet(TutorialConditions.ENHANCE_TUTORIAL);
                return;
            }
        }
    }

    public void OnClickedStoreButton()
    {
        windowManager.Open(Windows.Store);
    }
    public void OnClickedMainButton()
    {
        windowManager.Open(Windows.Main);
    }

    public void OnClickedDungeonButton()
    {
        OnClickedDungeonButtonAsync().Forget();
    }

    private async UniTaskVoid OnClickedDungeonButtonAsync()
    {
        // 던전 튜토리얼이 완료되지 않았으면 튜토리얼 먼저 시작
        bool isDungeonTutorialCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted(TutorialSequenceIds.DungeonTutorial);
        if (!isDungeonTutorialCompleted && TutorialManager.Instance != null)
        {
            // 던전 패널 먼저 열기
            windowManager.Open(Windows.Dungeon);

            // 튜토리얼 시작 (완료될 때까지 대기)
            await TutorialManager.Instance.NotifyConditionMetAsync(TutorialConditions.DUNGEON_TAB_FIRST_ENTER);
        }
        else
        {
            windowManager.Open(Windows.Dungeon);
        }
    }
    public void OnClickedUnitButton()
    {
        OnClickedUnitButtonAsync().Forget();
    }

    private async UniTaskVoid OnClickedUnitButtonAsync()
    {
        // 강화 튜토리얼이 완료되지 않았으면 튜토리얼 먼저 시작
        bool isEnhanceTutorialCompleted = DatabaseManager.Instance.IsTutorialSequenceCompleted(TutorialSequenceIds.EnhanceTutorial);
        if (!isEnhanceTutorialCompleted && TutorialManager.Instance != null)
        {
            // 유닛 패널 먼저 열기
            windowManager.Open(Windows.Unit);

            // 튜토리얼 시작 (완료될 때까지 대기)
            await TutorialManager.Instance.NotifyConditionMetAsync(TutorialConditions.UNIT_TAB_FIRST_ENTER);
        }
        else
        {
            windowManager.Open(Windows.Unit);
        }
    }
    public void OnClickedStageButton()
    {
        // 튜토리얼에 버튼 터치 알림
        TutorialManager.Instance?.NotifyButtonTouched(TutorialButtons.StageButton);

        windowManager.Open(Windows.Stage);
    }

    public void OnClickedEnforceButton()
    {
        windowManager.Open(Windows.Enforce);
    }
    public async void OnClickedLogOutButton()
    {
        // 튜토리얼 매니저 상태 초기화
        TutorialManager.Instance?.ResetForLogout();

        await AuthManager.Instance.SignOutAsync();
        SceneManager.LoadScene("01_Title");
    }


}
