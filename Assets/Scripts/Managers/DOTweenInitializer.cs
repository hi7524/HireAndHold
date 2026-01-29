using DG.Tweening;
using UnityEngine;

/// <summary>
/// DOTween 전역 초기화 설정
/// 씬 로드 전에 자동 실행됨
/// </summary>
public static class DOTweenInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        DOTween.Init(
            recycleAllByDefault: true,   // 모든 Tween 자동 재활용 (GC 감소)
            useSafeMode: false           // 안전 검사 끄기 (성능 향상)
        );

        // Tween 풀 크기 설정 (런타임 할당 방지)
        DOTween.SetTweensCapacity(500, 50);

        Debug.Log("[DOTweenInitializer] DOTween initialized with recycling enabled");
    }
}
