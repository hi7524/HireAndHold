using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;


public class StoreWindow : GenericWindow
{

    [Header("Manager")]
    [SerializeField] private GachaManager gachaManager;

    [Header("UI Panels")]
    [SerializeField] private GameObject gachaMenuPanel;
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
        // 이벤트 구독
        if (gachaManager != null)
        {
            gachaManager.OnGachaComplete += OnGachaComplete;
            gachaManager.OnGachaError += OnGachaError;
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
    }

    // ============================================
    // 버튼 이벤트
    // ============================================

    public void OnClickNormalSingle()
    {
        if (isPlaying) return;
        gachaManager.ExecuteGacha(GachaType.Normal, 1);
    }

    public void OnClickNormalTen()
    {
        if (isPlaying) return;
        gachaManager.ExecuteGacha(GachaType.Normal, 10);
    }

    public void OnClickPremiumSingle()
    {
        if (isPlaying) return;
        gachaManager.ExecuteGacha(GachaType.Premium, 1);
    }

    public void OnClickPremiumTen()
    {
        if (isPlaying) return;
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
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        try
        {
            await PlayResultAnimationAsync(result, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[GachaUI] 연출 취소됨");
        }
    }

    /// <summary>
    /// 가챠 에러 핸들러
    /// </summary>
    private void OnGachaError(string errorMessage)
    {
        Debug.LogWarning($"[GachaUI] {errorMessage}");
        // PopupManager.Instance.ShowError(errorMessage);
    }

    /// <summary>
    /// 결과 애니메이션 재생
    /// </summary>
    private async UniTask PlayResultAnimationAsync(GachaResult result, CancellationToken ct)
    {
        isPlaying = true;

        // 메뉴 패널 숨기고 결과 패널 표시
        if (gachaMenuPanel != null)
        {
            gachaMenuPanel.SetActive(false);
        }
        
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
            // Debug.Log($"[GachaUI] 카드 표시: {item.unitId} ({item.rarity})");

            if (i < result.items.Count - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(cardAppearDelay), cancellationToken: ct);
            }
        }

        // 모든 카드 표시 후 대기
        await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: ct);

        isPlaying = false;
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

            elapsed += Time.deltaTime;
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
        cts?.Cancel();
        
    }

    private void OnClickClose()
    {
        if (gachaResultPanel != null)
        {
            gachaResultPanel.SetActive(false);
        }
        
        if (gachaMenuPanel != null)
        {
            gachaMenuPanel.SetActive(true);
        }

        ClearResultCards();
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
