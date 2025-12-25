using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized);


        // 3. 로그인 상태 확인
        if (AuthManager.Instance.IsLoggedIn)
        {
            ShowLoading(true, "데이터 로드 중...");
            await LoadDataAndGoToLobbyAsync();
        }
        else
        {
            ShowLoading(false);
            ShowLoginScreen();
        }
    }


    private async UniTask<bool> WaitForFirebaseWithTimeoutAsync()
    {
        try
        {

            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(2f));
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
        LoadingRequest request = new LoadingRequest("Lobby");

        // 1. 데이터 테이블 로드
        request.AddTask("데이터 테이블 로드", async (ct) =>
        {
            await DataTableManager.InitAsync();
        }, weight: 0.2f);

        // 2. 유저 데이터 로드
        request.AddTask("유저 데이터 로드", async (ct) =>
        {
            await DatabaseManager.Instance.WaitForInitializationAsync();
            await DatabaseManager.Instance.LoadUserDataAsync();
        }, weight: 0.2f);

        // 3. 게임 리소스 프리로드
        request.AddTask("게임 리소스 로드", async (ct, progress) =>
        {
            await AddressablePreloader.Instance.PreloadAllAsync(ct, progress);
        }, weight: 0.5f);

        // 4. 스프라이트 캐싱 (데이터 테이블 로드 후)
        request.AddTask("UI 아이콘 캐싱", async (ct) =>
        {
            await PreloadSpritesAsync();
        }, weight: 0.1f);

        request.onLoadingComplete = () =>
        {
        };

        await LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }

    private async UniTask PreloadSpritesAsync()
    {
        // DataTableManager 초기화 대기
        if (!DataTableManager.IsInitialized)
        {
            Debug.LogWarning("[GameInitializer] DataTableManager가 초기화되지 않았습니다. 스프라이트 프리로드를 건너뜁니다.");
            return;
        }

        var addresses = new HashSet<string>();

        // 보유한 모든 유닛 아이콘
        var characters = DatabaseManager.Instance.GetAllCharacters();
        var unitTable = DataTableManager.UnitTable;

        if (unitTable == null)
        {
            Debug.LogWarning("[GameInitializer] UnitTable이 null입니다. 스프라이트 프리로드를 건너뜁니다.");
            return;
        }

        foreach (var character in characters)
        {
            if (int.TryParse(character.id, out int unitId))
            {
                var unitData = unitTable.Get(unitId);
                if (unitData != null && !string.IsNullOrEmpty(unitData.UNIT_ICON))
                {
                    addresses.Add(unitData.UNIT_ICON);

                    // 조각 아이콘도 프리로드
                    string icon = unitData.UNIT_ICON;
                    icon = System.Text.RegularExpressions.Regex.Replace(icon, @"\d+$", "");
                    addresses.Add($"unit/fragment/{icon}");
                }
            }
        }

        // 스킬 아이콘 프리로드
        var skillTable = DataTableManager.SkillTable;
        if (skillTable != null)
        {
            foreach (var skill in skillTable.GetAll())
            {
                if (!string.IsNullOrEmpty(skill.SKILL_ICON))
                {
                    addresses.Add(skill.SKILL_ICON);
                }
            }
        }

        if (addresses.Count > 0)
        {
            Debug.Log($"[GameInitializer] {addresses.Count}개 스프라이트 프리로드 시작");
            await SpriteCache.Instance.PreloadSpritesAsync(addresses);
            Debug.Log($"[GameInitializer] 프리로드 완료! 캐시된 스프라이트: {SpriteCache.Instance.CachedCount}개");
        }
        else
        {
            Debug.LogWarning("[GameInitializer] 프리로드할 스프라이트가 없습니다.");
        }
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
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }
        await LoadDataAndGoToLobbyAsync();
    }


    private void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
