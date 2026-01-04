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
    [SerializeField] private GameObject insufficientCurrencyPanel;
    [SerializeField] private TMPro.TextMeshProUGUI insufficientMessageText;

    [Header("Result UI")]
    [SerializeField] private Transform resultContainer;
    [SerializeField] private GameObject gachaResultCardPrefab;

    [Header("Buttons")]
    [SerializeField] private UnityEngine.UI.Button skipButton;
    [SerializeField] private UnityEngine.UI.Button closeButton;
    [SerializeField] private UnityEngine.UI.Button retryButton; // 다시 뽑기 버튼
    [SerializeField] private UnityEngine.UI.Button insufficientCloseButton;

    [Header("Currency Display")]
    [SerializeField] private TMPro.TextMeshProUGUI normalSingleCurrencyText;
    [SerializeField] private TMPro.TextMeshProUGUI normalTenCurrencyText;
    [SerializeField] private TMPro.TextMeshProUGUI premiumSingleCurrencyText;
    [SerializeField] private TMPro.TextMeshProUGUI premiumTenCurrencyText;

    [Header("Currency Settings")]
    [SerializeField] private int normalDiceItemId = 5102;
    [SerializeField] private int premiumDiceItemId = 5103;
    [SerializeField] private int normalSingleCost = 1;
    [SerializeField] private int normalTenCost = 10;
    [SerializeField] private int premiumSingleCost = 1;
    [SerializeField] private int premiumTenCost = 10;

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

    // 마지막으로 실행한 가챠 정보 저장
    private GachaType lastGachaType;
    private int lastGachaCount;

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

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnClickRetry);
        }

        if (insufficientCloseButton != null)
        {
            insufficientCloseButton.onClick.AddListener(OnClickInsufficientClose);
        }

        if (cheatButton != null)
        {
            cheatButton.onClick.AddListener(OnClickCheat);
        }

        // 재화 표시 초기화
        UpdateAllCurrencyDisplays();

        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        // 창이 활성화될 때마다 재화 표시 업데이트
        UpdateAllCurrencyDisplays();
    }

    /// <summary>
    /// 모든 뽑기 버튼의 재화 표시를 업데이트
    /// </summary>
    private void UpdateAllCurrencyDisplays()
    {
        UpdateCurrencyDisplay(normalSingleCurrencyText, normalDiceItemId, normalSingleCost);
        UpdateCurrencyDisplay(normalTenCurrencyText, normalDiceItemId, normalTenCost);
        UpdateCurrencyDisplay(premiumSingleCurrencyText, premiumDiceItemId, premiumSingleCost);
        UpdateCurrencyDisplay(premiumTenCurrencyText, premiumDiceItemId, premiumTenCost);
    }

    /// <summary>
    /// 개별 버튼의 재화 표시 업데이트 (형식: "보유재화/필요재화")
    /// </summary>
    private void UpdateCurrencyDisplay(TMPro.TextMeshProUGUI currencyText, int itemId, int cost)
    {
        if (currencyText == null) return;

        int currentAmount = PlayData.GetItemCount(itemId);

        // "보유재화/필요재화" 형식으로 표시
        currencyText.text = $"{currentAmount}/{cost}";

        // 재화가 부족하면 빨간색으로 표시
        if (currentAmount < cost)
        {
            currencyText.color = Color.red;
        }
        else
        {
            currencyText.color = Color.white;
        }
    }

    public void OnClickNormalSingle()
    {
        if (isPlaying) return;

        currentGachaCount = 1;
        lastGachaType = GachaType.Normal;
        lastGachaCount = 1;

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
        lastGachaType = GachaType.Normal;
        lastGachaCount = 10;

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
        lastGachaType = GachaType.Premium;
        lastGachaCount = 1;

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
        lastGachaType = GachaType.Premium;
        lastGachaCount = 10;

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
            ShowDoorStatic();
            await WaitForGachaResultAsync(ct);
            await WaitForSwipeAsync(ct);
            await PlayDoorOpenAnimationAsync(ct);

            if (pendingResult != null)
            {
                await ShowResultCardsAsync(pendingResult, ct);
                pendingResult = null;
            }

            // 결과 표시 후 재화 업데이트
            UpdateAllCurrencyDisplays();
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

    private void ShowDoorStatic()
    {
        if (doorPanel != null)
        {
            doorPanel.SetActive(true);
        }

        if (doorAnimation != null)
        {
            doorAnimation.Stop();
        }

        isDoorActive = true;
        isWaitingForSwipe = true;

        Debug.Log("[StoreWindow] 문 정지 상태로 표시 완료");
    }

    private async UniTask PlayDoorOpenAnimationAsync(CancellationToken ct)
    {
        Debug.Log("[StoreWindow] 문 열림 애니메이션 시작");

        if (swipeHintText != null)
            swipeHintText.SetActive(false);

        var animationTask = doorAnimation != null
            ? doorAnimation.PlayOnceAsync(ct)
            : UniTask.CompletedTask;

        var saveTask = pendingResult != null
            ? pendingResult.WaitForSaveAsync()
            : UniTask.CompletedTask;

        await UniTask.WhenAll(animationTask, saveTask);

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

        // 다시 뽑기 버튼 활성화
        if (retryButton != null)
            retryButton.gameObject.SetActive(true);

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

    private void OnGachaError(string errorMessage)
    {
        Debug.LogWarning($"[StoreWindow] 가챠 에러: {errorMessage}");

        cts?.Cancel();

        isPlaying = false;
        pendingResult = null;
        isDoorActive = false;
        isWaitingForSwipe = false;

        if (doorPanel != null)
            doorPanel.SetActive(false);
        if (swipeHintText != null)
            swipeHintText.SetActive(false);
        if (gachaResultPanel != null)
            gachaResultPanel.SetActive(false);

        ShowInsufficientCurrencyPanel(errorMessage);

        // 에러 후 재화 표시 업데이트
        UpdateAllCurrencyDisplays();
    }

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

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        ClearResultCards();

        // 창 닫을 때 재화 표시 업데이트
        UpdateAllCurrencyDisplays();
    }

    /// <summary>
    /// 다시 뽑기 버튼 클릭
    /// </summary>
    private void OnClickRetry()
    {
        if (isPlaying) return;

        Debug.Log($"[StoreWindow] 다시 뽑기 - Type: {lastGachaType}, Count: {lastGachaCount}");

        // 결과 패널 닫기
        if (gachaResultPanel != null)
            gachaResultPanel.SetActive(false);

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        ClearResultCards();

        // 마지막 가챠 정보로 다시 실행
        currentGachaCount = lastGachaCount;
        isPlaying = true;

        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        ShowDoorImmediately(cts.Token).Forget();

        gachaManager.ExecuteGacha(lastGachaType, lastGachaCount);
    }

    private async void OnClickCheat()
    {
        bool normalSuccess = await DatabaseManager.Instance.AddItemAsync(5102, cheatDiceAmount);
        bool premiumSuccess = await DatabaseManager.Instance.AddItemAsync(5103, cheatDiceAmount);

        if (normalSuccess && premiumSuccess)
        {
            PlayData.SyncItemsFromDatabase();

            // 치트 사용 후 재화 표시 업데이트
            UpdateAllCurrencyDisplays();
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

        if (swipeHintText != null)
            swipeHintText.SetActive(true);

        PlaySwipeHintPulseAsync(ct).Forget();

        float autoSkipTime = 5f;
        float elapsed = 0f;

        while (isWaitingForSwipe && elapsed < autoSkipTime)
        {
            ct.ThrowIfCancellationRequested();

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;

                if (touch.press.wasPressedThisFrame)
                {
                    swipeStartPos = touch.position.ReadValue();
                }

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
