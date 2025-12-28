using UnityEngine;
using UnityEngine.UI;

public class CurrencyPlusButton : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CurrencyType currencyType;

    [Header("References")]
    [SerializeField] private Button plusButton;
    [SerializeField] private GameObject storeWindow;
    [SerializeField] private ScrollRect storeScrollRect; 
    [SerializeField] private RectTransform targetSection; 

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 5f;
    [SerializeField] private float scrollDelay = 0.2f; 
    [SerializeField] private float topPadding = 0f; 

    private bool isScrolling = false;
    private float targetScrollPosition = 0f;
    private float startScrollPosition = 0f;
    private float scrollElapsedTime = 0f;
    private float scrollDuration = 0.3f;

    private bool waitingForLayout = false;
    private float layoutWaitTime = 0f;
    private int layoutFrameWait = 0;

    /// <summary>
    /// 재화 타입
    /// </summary>
    public enum CurrencyType
    {
        Gold = 1,   
        Diamond = 2, 
        Energy = 3     
    }

    private void Awake()
    {
        if (plusButton != null)
        {
            plusButton.onClick.AddListener(OnPlusButtonClicked);
        }
    }

    /// <summary>
    /// + 버튼 클릭
    /// </summary>
    private void OnPlusButtonClicked()
    {
        // 이미 스크롤 중이면 무시
        if (isScrolling || waitingForLayout)
        {
            return;
        }

        // 상점 윈도우 열기
        if (storeWindow != null && !storeWindow.activeSelf)
        {
            storeWindow.SetActive(true);
        }

        // 레이아웃 대기 시작
        waitingForLayout = true;
        layoutWaitTime = 0f;
        layoutFrameWait = 0;
        isScrolling = false;
    }

    private void Update()
    {
        // 레이아웃 업데이트 대기
        if (waitingForLayout)
        {
            layoutWaitTime += Time.deltaTime;
            layoutFrameWait++;

            // 시간과 프레임 둘 다 확인
            if (layoutWaitTime >= scrollDelay && layoutFrameWait >= 3)
            {
                waitingForLayout = false;
                CalculateScrollPosition();
            }
            return;
        }

        // 스크롤 애니메이션
        if (isScrolling && storeScrollRect != null)
        {
            scrollElapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(scrollElapsedTime / scrollDuration);

            // EaseOutCubic 이징
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            storeScrollRect.verticalNormalizedPosition = Mathf.Lerp(startScrollPosition, targetScrollPosition, easedT);

            // 목표 위치에 도달하면 스크롤 중지
            if (t >= 1f)
            {
                storeScrollRect.verticalNormalizedPosition = targetScrollPosition;
                isScrolling = false;
            }
        }
    }

    /// <summary>
    /// 스크롤 위치 계산
    /// </summary>
    private void CalculateScrollPosition()
    {
        if (storeScrollRect == null || targetSection == null)
        {
            Debug.LogWarning("[CurrencyPlusButton] ScrollRect 또는 TargetSection이 설정되지 않았습니다.");
            return;
        }

        RectTransform content = storeScrollRect.content;
        RectTransform viewport = storeScrollRect.viewport;

        if (content == null || viewport == null)
        {
            Debug.LogWarning("[CurrencyPlusButton] Content 또는 Viewport가 없습니다.");
            return;
        }

        // Canvas 강제 업데이트
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        // 콘텐츠와 뷰포트 높이
        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;
        float scrollableHeight = contentHeight - viewportHeight;

        if (scrollableHeight <= 0)
        {
            Debug.Log("[CurrencyPlusButton] 스크롤할 필요 없음 (콘텐츠가 뷰포트보다 작음)");
            return;
        }


        Vector2 contentPos = content.anchoredPosition;
        Vector2 targetPos = targetSection.anchoredPosition;

        float targetDistanceFromContentTop = Mathf.Abs(targetPos.y);

        float desiredScrollDistance = targetDistanceFromContentTop - topPadding;

        float normalizedPosition = Mathf.Clamp01(desiredScrollDistance / scrollableHeight);

        Debug.Log($"[CurrencyPlusButton] {currencyType} - " +
                  $"targetY: {targetPos.y}, " +
                  $"targetDistanceFromTop: {targetDistanceFromContentTop}, " +
                  $"scrollableHeight: {scrollableHeight}, " +
                  $"desiredScrollDistance: {desiredScrollDistance}, " +
                  $"normalized: {normalizedPosition}");


        startScrollPosition = storeScrollRect.verticalNormalizedPosition;
        targetScrollPosition = 1f - normalizedPosition;
        scrollElapsedTime = 0f;
        scrollDuration = 1f / scrollSpeed;
        isScrolling = true;
    }

    private void OnDestroy()
    {
        if (plusButton != null)
        {
            plusButton.onClick.RemoveListener(OnPlusButtonClicked);
        }
    }

    private void OnDisable()
    {

        isScrolling = false;
        waitingForLayout = false;
    }
}
