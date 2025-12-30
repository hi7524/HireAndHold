using Cysharp.Threading.Tasks;
using Tutorial;
using UnityEngine;

public class LobbyToGameLoader : MonoBehaviour
{
    private void Awake()
    {
        // 튜토리얼 타겟으로 등록 (StartButton으로)
        TutorialTargetRegistry.Register(TutorialButtons.StartButton, gameObject);
    }

    private void OnDestroy()
    {
        TutorialTargetRegistry.Unregister(TutorialButtons.StartButton);
    }

    public void OnStartGameButtonClicked()
    {
        // 튜토리얼에 버튼 터치 알림
        TutorialManager.Instance?.NotifyButtonTouched(TutorialButtons.StartButton);

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
