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
    [SerializeField] private float cardAppearDelay = 0.3f;     
    [SerializeField] private float cardAnimationDuration = 0.2f; 

    [Header("Cheat (Debug)")]
    [SerializeField] private UnityEngine.UI.Button cheatButton;
    [SerializeField] private int cheatDiceAmount = 100;

    private int currentGachaCount = 0;

    [Header("Door Animation")]
    [SerializeField] private float doorFadeDuration = 0.5f;
    [SerializeField] private float swipeThreshold = 100f;
    [SerializeField] private GameObject doorPanel;
    [SerializeField] private UISpriteFrameAnimation doorAnimation;
    [SerializeField] private GameObject swipeHintText;
    private bool hasShownSwipeHint = false;

    private bool isDoorActive = false;
    private bool isWaitingForSwipe = false;
    private Vector2 swipeStartPos;

    private bool isPlaying = false;
    private CancellationTokenSource cts;

    private bool isPlayingAnimation = false;
    private bool isSkipping = false;

    private GachaResult pendingResult = null;

    private void Start()
    {
        isPlaying = false;

        if (gachaManager != null)
        {
            gachaManager.OnGachaComplete += OnGachaComplete;
            gachaManager.OnGachaError += OnGachaError;
        }

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
        isPlaying = true;

        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        ShowDoorImmediately(cts.Token).Forget();

        gachaManager.ExecuteGacha(GachaType.Normal, 1);
    }

    public void OnClickNormalTen()
    {
        if (isPlaying) return;

        currentGachaCount = 10;
        isPlaying = true;

        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        ShowDoorImmediately(cts.Token).Forget();

        gachaManager.ExecuteGacha(GachaType.Normal, 10);
    }

    public void OnClickPremiumSingle()
    {
        if (isPlaying) return;

        currentGachaCount = 1;
        isPlaying = true;

        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        ShowDoorImmediately(cts.Token).Forget();

        gachaManager.ExecuteGacha(GachaType.Premium, 1);
    }

    public void OnClickPremiumTen()
    {
        if (isPlaying) return;

        currentGachaCount = 10;
        isPlaying = true;
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        ShowDoorImmediately(cts.Token).Forget();

        gachaManager.ExecuteGacha(GachaType.Premium, 10);
    }

    /// <summary>
    /// 버튼 클릭 즉시 문 연출 시작
    /// </summary>
    private async UniTaskVoid ShowDoorImmediately(CancellationToken ct)
    {
        try
        {
   
            var doorTask = ShowDoorAsync(ct);
            var waitResultTask = WaitForGachaResultAsync(ct);


            await UniTask.WhenAll(doorTask, waitResultTask);


            await WaitForSwipeAsync(ct);

            if (pendingResult != null)
            {
                await ShowResultCardsAsync(pendingResult, ct);
                pendingResult = null;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[StoreWindow] 문 연출 취소됨");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[StoreWindow] 문 연출 중 오류: {ex.Message}");
            isPlaying = false;
        }
    }

    /// <summary>
    /// 가챠 결과 대기 (최적화 - 더 짧은 대기 시간)
    /// </summary>
    private async UniTask WaitForGachaResultAsync(CancellationToken ct)
    {
        float waitTime = 0f;
        float maxWaitTime = 3f; // 5초 → 3초로 단축

        while (pendingResult == null && waitTime < maxWaitTime)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Delay(50, cancellationToken: ct); // 100ms → 50ms로 더 자주 체크
            waitTime += 0.05f;
        }

        if (pendingResult == null)
        {
            Debug.LogWarning("[StoreWindow] 가챠 결과를 받지 못했습니다");
            isPlaying = false;
        }
        else
        {
            Debug.Log($"[StoreWindow] 가챠 결과 수신 완료 (대기 시간: {waitTime:F2}초)");
        }
    }

    /// <summary>
    /// 가챠 완료 이벤트 핸들러
    /// </summary>
    private void OnGachaComplete(GachaResult result)
    {
        if (result == null)
        {
            Debug.LogError("[StoreWindow] GachaResult가 null입니다!");
            isPlaying = false;
            return;
        }

        Debug.Log($"[StoreWindow] 가챠 완료 - {result.items.Count}개 아이템");
        pendingResult = result;
    }

    /// <summary>
    /// 결과 카드 표시 (최적화)
    /// </summary>
    private async UniTask ShowResultCardsAsync(GachaResult result, CancellationToken ct)
    {
        isPlayingAnimation = true;
        isSkipping = false;

        if (skipButton != null)
            skipButton.gameObject.SetActive(currentGachaCount > 1);

        try
        {
            if (gachaResultPanel != null)
                gachaResultPanel.SetActive(true);

            ClearResultCards();

            // 1회 뽑기는 애니메이션 없이 즉시 표시
            if (result.items.Count == 1)
            {
                ShowAllRemainingCards(result, 0);
                await UniTask.Delay(100, cancellationToken: ct); // 잠깐 대기
            }
            else
            {
                // 10회 뽑기는 애니메이션
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
        }
        finally
        {
            isPlayingAnimation = false;
            isPlaying = false;
        }
    }

    /// <summary>
    /// 가챠 에러 핸들러
    /// </summary>
    private void OnGachaError(string errorMessage)
    {
        Debug.LogWarning($"[GachaUI] {errorMessage}");
        isPlaying = false;
        pendingResult = null;
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

    /// <summary>
    /// 결과 카드 표시 (최적화 - 더 빠른 애니메이션)
    /// </summary>
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
            card.Setup(item); // 동기 Setup (캐시된 스프라이트 사용)
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

        // 등장 애니메이션 (최적화)
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
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

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

        if (isDoorActive && isWaitingForSwipe)
        {
            isWaitingForSwipe = false;

            doorAnimation?.ShowLastFrame();

            if (swipeHintText != null)
                swipeHintText.SetActive(false);

            if (doorPanel != null)
                doorPanel.SetActive(false);

            isDoorActive = false;
        }
    }

    private void OnClickClose()
    {
        cts?.Cancel();

        isPlaying = false;
        isPlayingAnimation = false;
        isSkipping = false;
        pendingResult = null;

        if (gachaResultPanel != null)
            gachaResultPanel.SetActive(false);

        if (doorPanel != null)
            doorPanel.SetActive(false);

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

        if (doorAnimation != null)
        {
            await doorAnimation.PlayOnceAsync(ct);
        }

        if (swipeHintText != null)
            swipeHintText.SetActive(true);

        isDoorActive = true;
        isWaitingForSwipe = true;

        PlaySwipeHintPulseAsync(ct).Forget();
    }

    /// <summary>
    /// 문 슬라이드 처리 (최적화 - 더 빠른 반응)
    /// </summary>
    private async UniTask WaitForSwipeAsync(CancellationToken ct)
    {
        hasShownSwipeHint = false;

        // 최대 대기 시간 추가 (2초 후 자동으로 넘어감)
        float autoSkipTime = 2f;
        float elapsed = 0f;

        while (isWaitingForSwipe && elapsed < autoSkipTime)
        {
            ct.ThrowIfCancellationRequested();

            Vector2 currentPos = Vector2.zero;
            bool isPressed = false;
            bool isReleased = false;

            // PC
            if (Mouse.current != null)
            {
                isPressed = Mouse.current.leftButton.isPressed;
                isReleased = Mouse.current.leftButton.wasReleasedThisFrame;
                currentPos = Mouse.current.position.ReadValue();
            }

            // Mobile
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                isPressed = touch.press.isPressed;
                isReleased = touch.press.wasReleasedThisFrame;
                currentPos = touch.position.ReadValue();
            }

            if (isPressed && !hasShownSwipeHint)
            {
                Vector2 delta = currentPos - swipeStartPos;

                if (Mathf.Abs(delta.x) > 10f)
                {
                    hasShownSwipeHint = true;

                    if (swipeHintText != null)
                        swipeHintText.SetActive(true);

                    PlaySwipeHintPulseAsync(ct).Forget();
                }
            }

            if (isReleased)
            {
                Vector2 delta = currentPos - swipeStartPos;

                if (delta.x > swipeThreshold)
                {
                    isWaitingForSwipe = false;
                    FadeOutDoorAsync(ct).Forget();
                    break;
                }
            }

            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        // 시간 초과 시 자동으로 문 닫기
        if (isWaitingForSwipe)
        {
            Debug.Log("[StoreWindow] 스와이프 대기 시간 초과 - 자동으로 문 닫기");
            isWaitingForSwipe = false;
            FadeOutDoorAsync(ct).Forget();
        }
    }

    private async UniTask FadeOutDoorAsync(CancellationToken ct)
    {
        if (swipeHintText != null)
            swipeHintText.SetActive(false);

        if (doorPanel != null)
            doorPanel.SetActive(false);

        isDoorActive = false;
    }

    private async UniTask PlaySwipeHintPulseAsync(CancellationToken ct)
    {
        if (swipeHintText == null) return;

        RectTransform rect = swipeHintText.GetComponent<RectTransform>();
        if (rect == null) return;

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
