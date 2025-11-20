using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System;
using TMPro;

public class GameInitializer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Settings")]
    [SerializeField] private float firebaseTimeoutSeconds = 2f;


    private async UniTaskVoid Start()
    {
        await InitializeGameAsync();

    }

    private async UniTask InitializeGameAsync()
    {
        // 로딩 패널 표시
        ShowLoading(true, "Firebase 연결 중...");

        // 1. Firebase 초기화 (타임아웃 체크)
        bool firebaseReady = await WaitForFirebaseWithTimeoutAsync();

        if (!firebaseReady)
        {
            // Firebase 연결 실패 시 앱 종료
            ShowLoading(true, "Firebase 연결 실패. 앱을 종료합니다...");
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            QuitApplication();
            return;
        }

        // 2. AuthManager 초기화 대기
        ShowLoading(true, "인증 시스템 초기화 중...");
        Debug.Log(AuthManager.Instance.IsInitialized);
        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized);


        // 3. 로그인 상태 확인
        if (AuthManager.Instance.IsLoggedIn)
        {
            Debug.Log("[GameInitializer] 사용자 이미 로그인됨. 로비로 이동합니다.");
            ShowLoading(true, "데이터 로드 중...");
            await LoadDataAndGoToLobbyAsync();
        }
        else
        {
            Debug.Log("[GameInitializer] 로그인 필요. 로그인 화면을 표시합니다.");
            ShowLoading(false);
            ShowLoginScreen();
        }
    }


    private async UniTask<bool> WaitForFirebaseWithTimeoutAsync()
    {
        try
        {

            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            var waitTask = UniTask.WaitUntil(() => FirebaseInitializer.Instance != null);

            await UniTask.WhenAny(waitTask, timeoutTask);

            if (FirebaseInitializer.Instance == null)
            {
                Debug.LogError("[GameInitializer] FirebaseInitializer를 찾을 수 없습니다.");
                return false;
            }

            // Firebase 초기화 완료를 타임아웃과 함께 대기
            var initTimeoutTask = UniTask.Delay(TimeSpan.FromSeconds(firebaseTimeoutSeconds));
            var initWaitTask = FirebaseInitializer.Instance.WaitForInitializationAsync();

            // 인덱스 기반으로 어느 작업이 먼저 완료되었는지 확인
            int completedIndex = await UniTask.WhenAny(initWaitTask, initTimeoutTask);

            if (completedIndex == 0) // initWaitTask가 먼저 완료됨
            {
                if (FirebaseInitializer.Instance.IsInitialized)
                {
                    Debug.Log("[GameInitializer] Firebase 초기화 성공");
                    return true;
                }
                else
                {
                    Debug.LogError("[GameInitializer] Firebase 초기화 실패");
                    return false;
                }
            }
            else // 타임아웃
            {
                Debug.LogError($"[GameInitializer] Firebase 초기화 타임아웃 ({firebaseTimeoutSeconds}초)");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameInitializer] Firebase 대기 중 오류: {ex.Message}");
            return false;
        }
    }


    private async UniTask LoadDataAndGoToLobbyAsync()
    {
        Debug.Log("[GameInitializer] 로비 씬 로딩 시작");
        LoadingRequest request = new LoadingRequest("DevScene_Lobby");


        // request.AddTask("Load Game Resources", async (ct) =>
        // {
        //     // 리소스 로드 시뮬레이션
        //     await DatabaseManager.Instance.ReadPlayerDataAsync(AuthManager.Instance.UserId);
        // }, weight: 0.7f);

        // 예시: 데이터 초기화 작업 추가
        request.AddTask("Initialize Game Data", async (ct) =>
        {
            // 데이터 초기화 시뮬레이션
            await DataTableManager.InitAsync();
        }, weight: 0.3f);

        request.onLoadingComplete = () =>
        {
            Debug.Log("로비 씬 로딩 완료!");
        };

        LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }

    private void ShowLoginScreen()
    {
        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
        }
    }


    private void ShowLoading(bool show, string message = "")
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(show);
        }

        if (statusText != null && !string.IsNullOrEmpty(message))
        {
            statusText.text = message;
        }
    }


    public async void OnLoginSuccess()
    {
        Debug.Log("[GameInitializer] OnLoginSuccess 호출됨");
        
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }

        Debug.Log("[GameInitializer] LoadDataAndGoToLobbyAsync 호출 시작");
        await LoadDataAndGoToLobbyAsync();
        Debug.Log("[GameInitializer] LoadDataAndGoToLobbyAsync 완료");
    }


    private void QuitApplication()
    {
        Debug.Log("[GameInitializer] 애플리케이션을 종료합니다...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}