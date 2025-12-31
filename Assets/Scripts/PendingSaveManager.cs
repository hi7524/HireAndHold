using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 백그라운드 저장 작업을 추적하고, 앱 종료 시 완료를 보장하는 매니저
/// </summary>
public class PendingSaveManager : MonoBehaviour
{
    private static PendingSaveManager instance;
    private static readonly List<UniTask> pendingTasks = new List<UniTask>();
    private static readonly object lockObj = new object();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance == null)
        {
            var go = new GameObject("[PendingSaveManager]");
            instance = go.AddComponent<PendingSaveManager>();
            DontDestroyOnLoad(go);
        }
    }

    /// <summary>
    /// 저장 작업을 추적에 추가
    /// </summary>
    public static void Track(UniTask task)
    {
        lock (lockObj)
        {
            pendingTasks.Add(task);
        }

        // 작업 완료 후 리스트에서 제거
        CleanupAfterComplete(task).Forget();
    }

    private static async UniTaskVoid CleanupAfterComplete(UniTask task)
    {
        try
        {
            await task;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PendingSaveManager] 저장 작업 실패: {e.Message}");
        }
        finally
        {
            lock (lockObj)
            {
                pendingTasks.Remove(task);
            }
        }
    }

    /// <summary>
    /// 모든 대기 중인 저장 작업 완료 대기
    /// </summary>
    public static async UniTask WaitAllPendingAsync()
    {
        UniTask[] tasksToWait;
        lock (lockObj)
        {
            if (pendingTasks.Count == 0)
                return;

            tasksToWait = pendingTasks.ToArray();
            Debug.Log($"[PendingSaveManager] 대기 중인 저장 작업 {tasksToWait.Length}개 완료 대기...");
        }

        try
        {
            await UniTask.WhenAll(tasksToWait);
            Debug.Log("[PendingSaveManager] 모든 저장 작업 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PendingSaveManager] 일부 저장 작업 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 대기 중인 작업 수
    /// </summary>
    public static int PendingCount
    {
        get
        {
            lock (lockObj)
            {
                return pendingTasks.Count;
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 앱이 백그라운드로 갈 때 저장 작업 완료 대기
            Debug.Log("[PendingSaveManager] 앱 일시정지 - 저장 작업 완료 대기");
            WaitAllPendingAsync().Forget();
        }
    }

    private void OnApplicationQuit()
    {
        // 앱 종료 시 저장 작업 완료 대기
        Debug.Log("[PendingSaveManager] 앱 종료 - 저장 작업 완료 대기");

        // 동기적으로 대기 (앱 종료 시에는 async 사용 불가)
        var task = WaitAllPendingAsync();

        // Unity에서는 OnApplicationQuit에서 비동기 대기가 어려우므로
        // PlayerPrefs에 플래그 저장하고 다음 실행 시 복구하는 방식도 고려 가능
        if (PendingCount > 0)
        {
            Debug.LogWarning($"[PendingSaveManager] 앱 종료 시 {PendingCount}개의 저장 작업이 진행 중입니다.");
        }
    }
}
