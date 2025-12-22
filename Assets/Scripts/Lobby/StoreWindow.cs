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

    [Header("Cheat (Debug)")]
    [SerializeField] private UnityEngine.UI.Button cheatButton;
    [SerializeField] private int cheatDiceAmount = 100;

    private bool isPlaying = false;
    private CancellationTokenSource cts;

    private bool isPlayingAnimation = false; 
    private bool isSkipping = false;

    private void Start()
    {
        // 초기 상태 설정
        isPlaying = false;
        
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

        if (cheatButton != null)
        {
            cheatButton.onClick.AddListener(OnClickCheat);
        }

        Time.timeScale = 1f;
    }



    public void OnClickNormalSingle()
    {
        if (isPlaying) 
        {
            return;
        }
        
        if (gachaManager == null)
        {
            Debug.LogError("[StoreWindow] GachaManager가 null입니다!");
            return;
        }
        gachaManager.ExecuteGacha(GachaType.Normal, 1);
    }

    public void OnClickNormalTen()
    {
        if (isPlaying) 
        {
            return;
        }
        
        if (gachaManager == null)
        {
            Debug.LogError("[StoreWindow] GachaManager가 null입니다!");
            return;
        }
        gachaManager.ExecuteGacha(GachaType.Normal, 10);
    }

    public void OnClickPremiumSingle()
    {
        if (isPlaying) 
        {
            return;
        }
        gachaManager.ExecuteGacha(GachaType.Premium, 1);
    }

    public void OnClickPremiumTen()
    {
        if (isPlaying) 
        {
            return;
        }
        
        if (gachaManager == null)
        {
            Debug.LogError("[StoreWindow] GachaManager가 null입니다!");
            return;
        }
        gachaManager.ExecuteGacha(GachaType.Premium, 10);
    }


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
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
        
        try
        {
            await PlayResultAnimationAsync(result, cts.Token);
        }
        catch (OperationCanceledException)
        {
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
        isPlaying = true;
        isPlayingAnimation = true;
        isSkipping = false;

        if (gachaResultPanel != null)
            gachaResultPanel.SetActive(true);

        ClearResultCards();

        try
        {
            for (int i = 0; i < result.items.Count; i++)
            {
                if (isSkipping)
                {
                    ShowAllRemainingCards(result, i);
                    break;
                }

                ct.ThrowIfCancellationRequested();
                await ShowResultCardAsync(result.items[i], i, ct);

                if (i < result.items.Count - 1)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(cardAppearDelay),
                        cancellationToken: ct
                    );
                }
            }
        }
        catch (OperationCanceledException)
        {
            if (isSkipping)
            {
                ShowAllRemainingCards(result, resultContainer.childCount);
            }
            else
            {
                throw;
            }
        }
        finally
        {
            isPlayingAnimation = false;
            isPlaying = false;
        }
    }

    private void ShowAllRemainingCards(GachaResult result, int startIndex)
    {
        for (int i = startIndex; i < result.items.Count; i++)
        {
            var cardObj = Instantiate(gachaResultCardPrefab, resultContainer);
            var card = cardObj.GetComponent<GachaResultCard>();
            card?.Setup(result.items[i]);

            // 애니메이션 없이 즉시 표시
            var rect = cardObj.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;

            var canvasGroup = cardObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = cardObj.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 1f;
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
        if (!isPlayingAnimation || isSkipping) return;
        isSkipping = true;
        cts?.Cancel();
    }



    private void OnClickClose()
    {
        cts?.Cancel();

        isPlaying = false;
        isPlayingAnimation = false;
        isSkipping = false;

        if (gachaResultPanel != null)
            gachaResultPanel.SetActive(false);

        ClearResultCards();
    }


    /// <summary>
    /// 치트 버튼: 뽑기권 지급
    /// </summary>
    private async void OnClickCheat()
    {
        // 일반 뽑기, 프리미엄 뽑기권 지급
        bool normalSuccess = await DatabaseManager.Instance.AddItemAsync(5102, cheatDiceAmount);
        bool premiumSuccess = await DatabaseManager.Instance.AddItemAsync(5103, cheatDiceAmount);

        if (normalSuccess && premiumSuccess)
        {
            // 캐시 동기화
            PlayData.SyncItemsFromDatabase();
        }
        else
        {
            Debug.LogError("[StoreWindow] 치트 아이템 지급 실패");
        }
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
