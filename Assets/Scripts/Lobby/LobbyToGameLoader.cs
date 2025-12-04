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
        LoadingRequest request = new LoadingRequest("Stage");

        // 리소스 프리로드 확인 (이미 타이틀->로비에서 로드됨)
        request.AddTask("리소스 확인", async (ct) =>
        {
            // 프리로드가 완료되지 않았으면 대기 (안전장치)
            if (!AddressablePreloader.Instance.IsLoaded)
            {
                await AddressablePreloader.Instance.PreloadAllAsync(ct);
            }
            // 최소 로딩 표시 시간
            await UniTask.Delay(300, cancellationToken: ct);
        }, weight: 1.0f);

        request.onLoadingComplete = () =>
        {
            Debug.Log("게임 씬 로딩 완료!");
        };

        LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }
}
