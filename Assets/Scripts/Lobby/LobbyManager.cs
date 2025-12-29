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
        // TutorialManager가 있으면 로비 진입 튜토리얼 체크
        if (TutorialManager.Instance != null)
        {
            await TutorialManager.Instance.CheckAndStartTutorialAsync(TutorialTriggerType.OnLobbyEnter);
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
        windowManager.Open(Windows.Dungeon);
    }
    public void OnClickedUnitButton()
    {
        windowManager.Open(Windows.Unit);
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
        await AuthManager.Instance.SignOutAsync();
        SceneManager.LoadScene("01_Title");
    }


}
