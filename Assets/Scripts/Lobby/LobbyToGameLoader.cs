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
        // 타이틀 → 로비에서 이미 모든 초기화 완료됨
        // (DataTableManager, AddressablePreloader, DatabaseManager)
        LoadingRequest request = new LoadingRequest("Stage");

        // 최소 로딩 화면 표시 (이미 초기화 완료 상태)
        request.AddTask("게임 준비", async (ct) =>
        {
            await UniTask.Delay(300, cancellationToken: ct);
        }, weight: 1.0f);

        request.onLoadingComplete = () =>
        {
        };

        LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }
}
