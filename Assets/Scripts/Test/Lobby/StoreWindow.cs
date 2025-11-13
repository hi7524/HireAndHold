using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;


public class StoreWindow : GenericWindow
{

    [Header("Manager")]
    [SerializeField] private GachaManager gachaManager;

    [Header("UI Panels")]
       [SerializeField] private GameObject gachaResultPanel;

    [Header("Result UI")]
    [SerializeField] private Transform resultContainer;
    [SerializeField] private GameObject gachaResultCardPrefab;
    
    [Header("Buttons")]
    [SerializeField] private UnityEngine.UI.Button skipButton;
    [SerializeField] private UnityEngine.UI.Button closeButton;

    [Header("Animation Settings")]
    [SerializeField] private float cardAppearDelay = 0.5f;
    [SerializeField] private float cardAnimationDuration = 0.3f;

    private bool isPlaying = false;
    private CancellationTokenSource cts;

    private void Start()
    {
        // 초기 상태 설정
        isPlaying = false;
        
        // 이벤트 구독
        if (gachaManager != null)
        {
            gachaManager.OnGachaComplete += OnGachaComplete;
            gachaManager.OnGachaError += OnGachaError;
            Debug.Log("[StoreWindow] GachaManager 이벤트 구독 완료");
        }
        

        // 버튼 이벤트
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnClickSkip);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnClickClose);
        }
        
        Time.timeScale = 1f;
        Debug.Log("[StoreWindow] 초기화 완료");
    }

    // ============================================
    // 버튼 이벤트
    // ============================================

    public void OnClickNormalSingle()
    {
        if (isPlaying) 
        {
            Debug.Log("[StoreWindow] 가챠 진행 중이라 버튼 무시");
            return;
        }
        
        if (gachaManager == null)
        {
            Debug.LogError("[StoreWindow] GachaManager가 null입니다!");
            return;
        }
        
        Debug.Log("[StoreWindow] 일반 단일 가챠 시작");
        gachaManager.ExecuteGacha(GachaType.Normal, 1);
    }

    public void OnClickNormalTen()
    {
        if (isPlaying) 
        {
            Debug.Log("[StoreWindow] 가챠 진행 중이라 버튼 무시");
            return;
        }
        
        if (gachaManager == null)
        {
            Debug.LogError("[StoreWindow] GachaManager가 null입니다!");
            return;
        }
        
        Debug.Log("[StoreWindow] 일반 10연차 가챠 시작");
        gachaManager.ExecuteGacha(GachaType.Normal, 10);
    }

    public void OnClickPremiumSingle()
    {
        if (isPlaying) 
        {
            Debug.Log("[StoreWindow] 가챠 진행 중이라 버튼 무시");
            return;
        }
        
    
        
        Debug.Log("[StoreWindow] 프리미엄 단일 가챠 시작");
        gachaManager.ExecuteGacha(GachaType.Premium, 1);
    }

    public void OnClickPremiumTen()
    {
        if (isPlaying) 
        {
            Debug.Log("[StoreWindow] 가챠 진행 중이라 버튼 무시");
            return;
        }
        
        if (gachaManager == null)
        {
            Debug.LogError("[StoreWindow] GachaManager가 null입니다!");
            return;
        }
        
        Debug.Log("[StoreWindow] 프리미엄 10연차 가챠 시작");
        gachaManager.ExecuteGacha(GachaType.Premium, 10);
    }

    // ============================================
    // 가챠 결과 처리
    // ============================================

    /// <summary>
    /// 가챠 완료 이벤트 핸들러
    /// </summary>
    private async void OnGachaComplete(GachaResult result)
    {
        if (result == null)
        {
            Debug.LogError("[StoreWindow] GachaResult가 null입니다!");
            return;
        }
        
        Debug.Log($"[StoreWindow] 가챠 결과 수신: {result.items.Count}개 아이템");
        
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
        
        try
        {
            await PlayResultAnimationAsync(result, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[StoreWindow] 연출 취소됨");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[StoreWindow] 가챠 결과 처리 중 오류: {ex.Message}");
            // 오류 발생 시에도 상태 리셋
            isPlaying = false;
        }
    }

    /// <summary>
    /// 가챠 에러 핸들러
    /// </summary>
    private void OnGachaError(string errorMessage)
    {
        Debug.LogWarning($"[GachaUI] {errorMessage}");
        
    }

    /// <summary>
    /// 결과 애니메이션 재생
    /// </summary>
    private async UniTask PlayResultAnimationAsync(GachaResult result, CancellationToken ct)
    {
        try
        {
            isPlaying = true;
            Debug.Log("[StoreWindow] 결과 애니메이션 시작");

            if (gachaResultPanel != null)
            {
                gachaResultPanel.SetActive(true);
            }

            // 기존 카드 제거
            ClearResultCards();

            // 카드 하나씩 표시
            for (int i = 0; i < result.items.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var item = result.items[i];
                await ShowResultCardAsync(item, i, ct);
                Debug.Log($"[GachaUI] 카드 표시: {item.unitId} ({item.rarity})");

                if (i < result.items.Count - 1)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(cardAppearDelay), cancellationToken: ct);
                }
            }

            // 모든 카드 표시 후 대기
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: ct);
            
            Debug.Log("[StoreWindow] 결과 애니메이션 정상 완료");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[StoreWindow] 결과 애니메이션 취소됨");
            throw; // 상위로 전파
        }
        finally
        {
            // 어떤 경우든 isPlaying 상태는 반드시 리셋
            isPlaying = false;
            Debug.Log("[StoreWindow] isPlaying 상태 리셋 완료");
        }
    }

    // <summary>
    // 결과 카드 표시
    // </summary>
    private async UniTask ShowResultCardAsync(GachaItem item, int index, CancellationToken ct)
    {
        if (gachaResultCardPrefab == null || resultContainer == null)
        {
            return;
        }

        // 카드 생성
        GameObject cardObj = Instantiate(gachaResultCardPrefab, resultContainer);
        var card = cardObj.GetComponent<GachaResultCard>();
        
        if (card != null)
        {
            card.Setup(item);
        }

        // 애니메이션
        RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = cardObj.GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = cardObj.AddComponent<CanvasGroup>();
        }

        rectTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        // 등장 애니메이션
        float elapsed = 0f;
        while (elapsed < cardAnimationDuration)
        {
            ct.ThrowIfCancellationRequested();

            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / cardAnimationDuration;
            
            float scale = Mathf.Lerp(0f, 1f, EaseOutBack(t));
            rectTransform.localScale = Vector3.one * scale;
            canvasGroup.alpha = t;

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        rectTransform.localScale = Vector3.one;
        canvasGroup.alpha = 1f;

        // // 특수 효과
        // if (item.rarity == GachaRarity.Legendary)
        // {
        //     PlayLegendaryEffect(cardObj);
        // }
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // private void PlayLegendaryEffect(GameObject cardObj)
    // {
    //     Debug.Log("[GachaUI] ✨ 전설 획득! ✨");
    //     // ParticleSystem, SFX 재생
    // }

    private void ClearResultCards()
    {
        if (resultContainer == null) return;

        foreach (Transform child in resultContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void OnClickSkip()
    {
        Debug.Log("[StoreWindow] Skip 버튼 클릭");
        
        // 애니메이션 취소
        cts?.Cancel();
        
        // 상태 리셋
        isPlaying = false;
        
        // 결과 패널 즉시 닫기
        if (gachaResultPanel != null)
        {
            gachaResultPanel.SetActive(false);
        }
        
        // 카드들 정리
        ClearResultCards();
        
        Debug.Log("[StoreWindow] Skip 처리 완료, 다시 가챠 가능");
    }

    private void OnClickClose()
    {
        Debug.Log("[StoreWindow] Close 버튼 클릭");
        
        // 애니메이션 취소
        cts?.Cancel();
        
        // 상태 리셋
        isPlaying = false;
        
        // 결과 패널 닫기
        if (gachaResultPanel != null)
        {
            gachaResultPanel.SetActive(false);
        }
        
        // 카드들 정리
        ClearResultCards();
        
        Debug.Log("[StoreWindow] Close 처리 완료, 다시 가챠 가능");
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();

        if (gachaManager != null)
        {
            gachaManager.OnGachaComplete -= OnGachaComplete;
            gachaManager.OnGachaError -= OnGachaError;
        }
    }
    

}
