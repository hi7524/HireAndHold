using Cysharp.Threading.Tasks;
using UnityEngine;

public class LobbyToGameLoader : MonoBehaviour
{
    public void OnStartGameButtonClicked()
    {
        LoadGameScene();
    }
    private void LoadGameScene()
    {
        LoadingRequest request = new LoadingRequest("DevScene_stage");

        // 예시: 리소스 로드 작업 추가
        request.AddTask("Load Game Resources", async (ct) =>
        {
            // 리소스 로드 시뮬레이션
            await UniTask.Delay(2000, cancellationToken: ct);
        }, weight: 0.7f);

        // 예시: 데이터 초기화 작업 추가
        request.AddTask("Initialize Game Data", async (ct) =>
        {
            // 데이터 초기화 시뮬레이션
            await UniTask.Delay(1000, cancellationToken: ct);
        }, weight: 0.3f);

        request.onLoadingComplete = () =>
        {
            Debug.Log("게임 씬 로딩 완료!");
        };

        LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }
}
