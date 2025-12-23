using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


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

    private int currentGachaCount = 0;

    [Header("Door Animation")]
    [SerializeField] private GameObject doorPanel;
    [SerializeField] private Image doorImage;
    [SerializeField] private GameObject swipeHintText; 
    [SerializeField] private float doorFadeDuration = 0.5f;
    [SerializeField] private float swipeThreshold = 100f; 

    private bool isDoorActive = false;
    private bool isWaitingForSwipe = false;
    private Vector2 swipeStartPos;

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
        if (isPlaying) return;

        currentGachaCount = 1;
        gachaManager.ExecuteGacha(GachaType.Normal, 1);
    }


    public void OnClickNormalTen()
    {
        if (isPlaying) return;

        currentGachaCount = 10;
        gachaManager.ExecuteGacha(GachaType.Normal, 10);
    }


    public void OnClickPremiumSingle()
    {
        if (isPlaying) return;

        currentGachaCount = 1;
        gachaManager.ExecuteGacha(GachaType.Premium, 1);
    }

    public void OnClickPremiumTen()
    {
        if (isPlaying) return;

        currentGachaCount = 10;
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

        if (skipButton != null)
            skipButton.gameObject.SetActive(currentGachaCount > 1);

        try
        {
            // 문 → 슬라이드 → 결과 연출
            await ShowDoorAsync(ct);
            await WaitForSwipeAsync(ct);

            if (gachaResultPanel != null)
                gachaResultPanel.SetActive(true);

            ClearResultCards();

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
        if (isDoorActive && isWaitingForSwipe)
        {
            Debug.Log("[StoreWindow] Skip - 문 즉시 닫기");
            isWaitingForSwipe = false;

            if (doorPanel != null)
                doorPanel.SetActive(false);
            if (swipeHintText != null)
                swipeHintText.SetActive(false);

            isDoorActive = false;
        }


        if (isPlayingAnimation && !isSkipping)
        {
            Debug.Log("[StoreWindow] Skip 버튼 클릭 - 연출 스킵");
            isSkipping = true;
        }
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

    /// <summary>
    /// 문 등장 애니메이션 (페이드 인)
    /// </summary>
    private async UniTask ShowDoorAsync(CancellationToken ct)
    {
        if (doorPanel != null)
            doorPanel.SetActive(true);

        if (doorImage != null)
        {
            var color = doorImage.color;
            color.a = 0f;
            doorImage.color = color;

            // 페이드 인
            float elapsed = 0f;
            while (elapsed < doorFadeDuration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / doorFadeDuration;

                color.a = t;
                doorImage.color = color;

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            color.a = 1f;
            doorImage.color = color;
        }

        // "슬라이드하세요" 텍스트 표시
        if (swipeHintText != null)
            swipeHintText.SetActive(true);

        isDoorActive = true;
        isWaitingForSwipe = true;

        PlaySwipeHintPulseAsync(ct).Forget();
    }

    /// <summary>
    /// 문 슬라이드 처리
    /// </summary>
    private async UniTask WaitForSwipeAsync(CancellationToken ct)
    {
        while (isWaitingForSwipe)
        {
            ct.ThrowIfCancellationRequested();

            // PC
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                    swipeStartPos = Mouse.current.position.ReadValue();

                else if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    Vector2 delta = Mouse.current.position.ReadValue() - swipeStartPos;

                    if (delta.x > swipeThreshold)
                    {
                        isWaitingForSwipe = false;

                        FadeOutDoorAsync(ct).Forget();

                        break;
                    }
                }
            }

            // Mobile
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;

                if (touch.press.wasPressedThisFrame)
                    swipeStartPos = touch.position.ReadValue();

                else if (touch.press.wasReleasedThisFrame)
                {
                    Vector2 delta = touch.position.ReadValue() - swipeStartPos;

                    if (delta.x > swipeThreshold)
                    {
                        isWaitingForSwipe = false;

                        FadeOutDoorAsync(ct).Forget();

                        break;
                    }
                }
            }

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    private async UniTask FadeOutDoorAsync(CancellationToken ct)
    {
        if (swipeHintText != null)
            swipeHintText.SetActive(false);

        if (doorImage == null) return;

        float duration = 0.6f;
        float elapsed = 0f;

        Color startColor = doorImage.color;

        while (elapsed < duration)
        {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;

            float t = elapsed / duration;

            // Ease Out
            float smooth = 1f - Mathf.Pow(1f - t, 3f);

            // 알파 감소
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, smooth);
            doorImage.color = c;

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        // 완전 제거
        doorImage.color = new Color(startColor.r, startColor.g, startColor.b, 0f);

        if (doorPanel != null)
            doorPanel.SetActive(false);

        isDoorActive = false;
    }

    private async UniTask PlaySwipeHintPulseAsync(CancellationToken ct)
    {
        RectTransform rect = swipeHintText.GetComponent<RectTransform>();
        Vector3 baseScale = Vector3.one;

        float time = 0f;

        while (isWaitingForSwipe)
        {
            ct.ThrowIfCancellationRequested();

            time += Time.unscaledDeltaTime;
            float scale = 1f + Mathf.Sin(time * 3f) * 0.08f;

            rect.localScale = baseScale * scale;

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        rect.localScale = baseScale;
    }



}
