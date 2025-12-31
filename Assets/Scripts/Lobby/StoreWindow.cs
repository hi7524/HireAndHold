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
    [SerializeField] private GameObject insufficientCurrencyPanel; // 재화 부족 패널
    [SerializeField] private TMPro.TextMeshProUGUI insufficientMessageText; // 재화 부족 메시지

    [Header("Result UI")]
    [SerializeField] private Transform resultContainer;
    [SerializeField] private GameObject gachaResultCardPrefab;

    [Header("Buttons")]
    [SerializeField] private UnityEngine.UI.Button skipButton;
    [SerializeField] private UnityEngine.UI.Button closeButton;
    [SerializeField] private UnityEngine.UI.Button insufficientCloseButton; // 재화 부족 패널 닫기 버튼

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

        if (insufficientCloseButton != null)
        {
            insufficientCloseButton.onClick.AddListener(OnClickInsufficientClose);
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

    private async UniTaskVoid ShowDoorImmediately(CancellationToken ct)
    {
        try
        {
            // 1. 문을 정지 상태로 표시 (애니메이션 없이)
            ShowDoorStatic();

            // 2. 가챠 결과 대기
            await WaitForGachaResultAsync(ct);

            // 3. 스와이프 대기 (문은 정지 상태)
            await WaitForSwipeAsync(ct);

            // 4. 스와이프 완료 시 문 열림 애니메이션 재생
            await PlayDoorOpenAnimationAsync(ct);

            // 5. 결과 표시
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
    /// 문을 정지 상태로 표시 (애니메이션 재생 없음)
    /// </summary>
    private void ShowDoorStatic()
    {
        if (doorPanel != null)
        {
            doorPanel.SetActive(true);
        }

        // 애니메이션은 재생하지 않고 첫 프레임만 표시
        if (doorAnimation != null)
        {
            doorAnimation.Stop();
        }

        isDoorActive = true;
        isWaitingForSwipe = true;

        Debug.Log("[StoreWindow] 문 정지 상태로 표시 완료");
    }

    /// <summary>
    /// 스와이프 시 문 열림 애니메이션 재생
    /// </summary>
    private async UniTask PlayDoorOpenAnimationAsync(CancellationToken ct)
    {
        Debug.Log("[StoreWindow] 문 열림 애니메이션 시작");

        if (swipeHintText != null)
            swipeHintText.SetActive(false);

        // 문 열림 애니메이션과 Firebase 저장을 동시에 대기
        // 애니메이션 중에 저장이 완료되므로 추가 대기 시간 없음
        var animationTask = doorAnimation != null
            ? doorAnimation.PlayOnceAsync(ct)
            : UniTask.CompletedTask;

        var saveTask = pendingResult != null
            ? pendingResult.WaitForSaveAsync()
            : UniTask.CompletedTask;

        await UniTask.WhenAll(animationTask, saveTask);

        // 애니메이션 완료 후 문 패널 닫기
        if (doorPanel != null)
            doorPanel.SetActive(false);

        isDoorActive = false;

        Debug.Log("[StoreWindow] 문 열림 애니메이션 + Firebase 저장 완료");
    }

    private async UniTask WaitForGachaResultAsync(CancellationToken ct)
    {
        float waitTime = 0f;
        float maxWaitTime = 3f;

        while (pendingResult == null && waitTime < maxWaitTime)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Delay(50, cancellationToken: ct);
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

            if (result.items.Count == 1)
            {
                ShowAllRemainingCards(result, 0);
                await UniTask.Delay(100, cancellationToken: ct);
            }
            else
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
        }
        finally
        {
            isPlayingAnimation = false;
            isPlaying = false;
        }
    }

    /// <summary>
    /// 가챠 에러 핸들러 - 애니메이션 즉시 중단!
    /// </summary>
    private void OnGachaError(string errorMessage)
    {
        Debug.LogWarning($"[StoreWindow] 가챠 에러: {errorMessage}");

        // ⭐ 애니메이션 즉시 중단
        cts?.Cancel();

        isPlaying = false;
        pendingResult = null;
        isDoorActive = false;
        isWaitingForSwipe = false;

        // 문 패널 즉시 닫기
        if (doorPanel != null)
            doorPanel.SetActive(false);
        if (swipeHintText != null)
            swipeHintText.SetActive(false);
        if (gachaResultPanel != null)
            gachaResultPanel.SetActive(false);

        // ⭐ 재화 부족 패널 표시
        ShowInsufficientCurrencyPanel(errorMessage);
    }

    /// <summary>
    /// 재화 부족 패널 표시
    /// </summary>
    private void ShowInsufficientCurrencyPanel(string message)
    {
        if (insufficientCurrencyPanel != null)
        {
            insufficientCurrencyPanel.SetActive(true);

            if (insufficientMessageText != null)
            {
                insufficientMessageText.text = message;
            }
        }
    }

    /// <summary>
    /// 재화 부족 패널 닫기
    /// </summary>
    private void OnClickInsufficientClose()
    {
        if (insufficientCurrencyPanel != null)
        {
            insufficientCurrencyPanel.SetActive(false);
        }
    }

    private void ShowAllRemainingCards(GachaResult result, int startIndex)
    {
        for (int i = startIndex; i < result.items.Count; i++)
        {
            var cardObj = Instantiate(gachaResultCardPrefab, resultContainer);
            var card = cardObj.GetComponent<GachaResultCard>();
            card?.Setup(result.items[i]);

            var rect = cardObj.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;

            var canvasGroup = cardObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = cardObj.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 1f;
        }
    }

    private async UniTask ShowResultCardAsync(GachaItem item, int index, CancellationToken ct)
    {
        if (gachaResultCardPrefab == null || resultContainer == null)
        {
            return;
        }

        GameObject cardObj = Instantiate(gachaResultCardPrefab, resultContainer);
        var card = cardObj.GetComponent<GachaResultCard>();

        if (card != null)
        {
            card.Setup(item);
        }

        RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = cardObj.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = cardObj.AddComponent<CanvasGroup>();
        }

        rectTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

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
            Debug.Log("[StoreWindow] Skip - 스와이프 단계 스킵");
            isWaitingForSwipe = false;
            return;
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
        pendingResult = null;

        if (gachaResultPanel != null)
            gachaResultPanel.SetActive(false);

        if (doorPanel != null)
            doorPanel.SetActive(false);

        if (insufficientCurrencyPanel != null)
            insufficientCurrencyPanel.SetActive(false);

        ClearResultCards();
    }

    private async void OnClickCheat()
    {
        bool normalSuccess = await DatabaseManager.Instance.AddItemAsync(5102, cheatDiceAmount);
        bool premiumSuccess = await DatabaseManager.Instance.AddItemAsync(5103, cheatDiceAmount);

        if (normalSuccess && premiumSuccess)
        {
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

    private async UniTask WaitForSwipeAsync(CancellationToken ct)
    {
        hasShownSwipeHint = false;

        // 스와이프 힌트 텍스트 표시
        if (swipeHintText != null)
            swipeHintText.SetActive(true);

        // 펄스 애니메이션 시작
        PlaySwipeHintPulseAsync(ct).Forget();

        float autoSkipTime = 5f; // 자동 스킵 시간 증가 (사용자가 읽을 시간 제공)
        float elapsed = 0f;

        while (isWaitingForSwipe && elapsed < autoSkipTime)
        {
            ct.ThrowIfCancellationRequested();

            // 터치 입력 처리
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;

                // 터치 시작
                if (touch.press.wasPressedThisFrame)
                {
                    swipeStartPos = touch.position.ReadValue();
                }

                // 터치 종료 시 스와이프 거리 계산
                if (touch.press.wasReleasedThisFrame)
                {
                    Vector2 currentPos = touch.position.ReadValue();
                    Vector2 delta = currentPos - swipeStartPos;

                    if (delta.x > swipeThreshold)
                    {
                        Debug.Log($"[StoreWindow] 스와이프 감지! delta.x = {delta.x}");
                        isWaitingForSwipe = false;
                        break;
                    }
                }
            }

            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        if (isWaitingForSwipe)
        {
            Debug.Log("[StoreWindow] 스와이프 대기 시간 초과 - 자동으로 문 열기");
            isWaitingForSwipe = false;
        }
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
