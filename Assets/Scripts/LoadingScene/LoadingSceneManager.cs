using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class LoadingSceneManager : MonoBehaviour
{
    private static LoadingSceneManager instance;
    public static LoadingSceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("LoadingSceneManager");
                instance = go.AddComponent<LoadingSceneManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("Settings")]
    [SerializeField] private string loadingSceneName = "DevScene_Loading";
    [SerializeField] private float minimumLoadingTime = 1f; 

    private LoadingRequest currentRequest;
    private CancellationTokenSource cts;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 로딩 씬을 거쳐서 다른 씬으로 이동
    /// </summary>
    public async void LoadSceneWithLoading(LoadingRequest request)
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        try
        {
            await LoadSceneWithLoadingAsync(request, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[LoadingManager] 로딩 취소됨");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoadingManager] 로딩 실패: {e.Message}");
        }
    }

    private async UniTask LoadSceneWithLoadingAsync(LoadingRequest request, CancellationToken ct)
    {
        currentRequest = request;
        Time.timeScale = 1f;
        // 1. 로딩 씬으로 이동
        await Addressables.LoadSceneAsync(loadingSceneName, LoadSceneMode.Single)
    .ToUniTask(cancellationToken: ct);

        // 2. 로딩 씬이 준비될 때까지 대기
        await UniTask.WaitUntil(() => LoadingSceneUI.Instance != null, cancellationToken: ct);

        // 3. 최소 로딩 시간 보장
        float startTime = Time.time;

        // 4. 실제 로딩 작업 수행
        await ExecuteLoadingTasksAsync(ct);

        // 5. 최소 시간 확보
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minimumLoadingTime)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(minimumLoadingTime - elapsedTime), cancellationToken: ct);
        }

        // 6. 타겟 씬으로 이동
        await Addressables.LoadSceneAsync(request.targetSceneName, request.loadMode).ToUniTask(cancellationToken: ct);

        // 7. 완료 콜백
        request.onLoadingComplete?.Invoke();
        currentRequest = null;
    }

    /// <summary>
    /// 로딩 작업 실행
    /// </summary>
    private async UniTask ExecuteLoadingTasksAsync(CancellationToken ct)
    {
        if (currentRequest.tasks.Count == 0)
        {
            Debug.LogWarning("[LoadingManager] 로딩 작업이 없습니다.");
            return;
        }

        // 전체 가중치 계산
        float totalWeight = 0f;
        foreach (var task in currentRequest.tasks)
        {
            totalWeight += task.weight;
        }

        float currentProgress = 0f;
Debug.Log($"[LoadingManager] {totalWeight} 시작");
        // 각 작업 실행
        for (int i = 0; i < currentRequest.tasks.Count; i++)
        {
            var task = currentRequest.tasks[i];
            

            // UI 업데이트
            if (LoadingSceneUI.Instance != null)
            {
                LoadingSceneUI.Instance.UpdateLoadingText(task.taskName);
            }

            // 작업 실행
            await task.taskAction(ct);

            // 진행도 업데이트
            currentProgress += task.weight / totalWeight;
            if (LoadingSceneUI.Instance != null)
            {
                LoadingSceneUI.Instance.UpdateProgress(currentProgress);
            }

            Debug.Log($"[LoadingManager] {task.taskName} 완료 ({currentProgress * 100:F0}%)");
        }
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}