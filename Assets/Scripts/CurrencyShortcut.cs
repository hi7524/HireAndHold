using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// 재화 + 버튼 클릭 시 상점으로 이동하고 해당 재화 섹션으로 스크롤
/// 섹션을 자동으로 찾아서 이동합니다.
/// </summary>
public class CurrencyShortcut : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CurrencyType targetCurrency;

    [Header("Store References")]
    [SerializeField] private GameObject storeWindow; // 상점 윈도우
    [SerializeField] private ScrollRect storeScrollRect; // 상점의 ScrollRect

    [Header("Section Finding (Optional)")]
    [SerializeField] private string sectionTagName = "StoreSection"; // 섹션의 태그 이름
    [SerializeField] private RectTransform manualTargetSection; // 수동으로 지정할 섹션

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 5f;
    [SerializeField] private float scrollDelay = 0.1f; // 윈도우 열린 후 스크롤 시작 딜레이

    private Button button;

    public enum CurrencyType
    {
        Gold,      // 골드
        Diamond,   // 다이아
        Ticket     // 티켓
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        OpenStoreAndScroll().Forget();
    }

    private async UniTaskVoid OpenStoreAndScroll()
    {
        // 1. 상점 윈도우 열기
        if (storeWindow != null && !storeWindow.activeSelf)
        {
            storeWindow.SetActive(true);
        }

        // 2. 잠깐 대기 (레이아웃 업데이트를 위해)
        await UniTask.Delay(System.TimeSpan.FromSeconds(scrollDelay));

        // 3. 타겟 섹션 찾기
        RectTransform targetSection = FindTargetSection();

        if (targetSection == null)
        {
            Debug.LogWarning($"[CurrencyShortcut] {targetCurrency} 섹션을 찾을 수 없습니다.");
            return;
        }

        // 4. 스크롤
        ScrollToSection(targetSection);
    }

    /// <summary>
    /// 타겟 섹션 찾기
    /// </summary>
    private RectTransform FindTargetSection()
    {
        // 수동으로 지정된 섹션이 있으면 사용
        if (manualTargetSection != null)
        {
            return manualTargetSection;
        }

        // ScrollRect의 Content에서 섹션 찾기
        if (storeScrollRect == null || storeScrollRect.content == null)
        {
            return null;
        }

        // 1. 태그로 찾기
        GameObject[] sections = GameObject.FindGameObjectsWithTag(sectionTagName);
        foreach (var section in sections)
        {
            var sectionComponent = section.GetComponent<StoreSectionMarker>();
            if (sectionComponent != null && sectionComponent.sectionType == (StoreSectionType)targetCurrency)
            {
                return section.GetComponent<RectTransform>();
            }
        }

        // 2. 이름으로 찾기 (fallback)
        string sectionName = targetCurrency switch
        {
            CurrencyType.Gold => "GoldSection",
            CurrencyType.Diamond => "DiamondSection",
            CurrencyType.Ticket => "TicketSection",
            _ => ""
        };

        Transform found = storeScrollRect.content.Find(sectionName);
        if (found != null)
        {
            return found.GetComponent<RectTransform>();
        }

        // 3. StoreSectionMarker 컴포넌트로 찾기
        var allMarkers = storeScrollRect.content.GetComponentsInChildren<StoreSectionMarker>();
        foreach (var marker in allMarkers)
        {
            if (marker.sectionType == (StoreSectionType)targetCurrency)
            {
                return marker.GetComponent<RectTransform>();
            }
        }

        return null;
    }

    /// <summary>
    /// 섹션으로 스크롤
    /// </summary>
    private void ScrollToSection(RectTransform targetSection)
    {
        if (storeScrollRect == null || targetSection == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform content = storeScrollRect.content;
        RectTransform viewport = storeScrollRect.viewport;

        // 타겟의 로컬 위치
        Vector2 targetLocalPos = content.InverseTransformPoint(targetSection.position);
        Vector2 viewportLocalPos = content.InverseTransformPoint(viewport.position);

        // 섹션을 뷰포트 상단에 위치시키기 위한 오프셋 계산
        float offset = -targetLocalPos.y + viewportLocalPos.y;

        // 콘텐츠의 높이와 뷰포트 높이
        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        // 스크롤 가능한 높이
        float scrollableHeight = contentHeight - viewportHeight;

        if (scrollableHeight <= 0)
        {
            // 스크롤할 필요 없음
            return;
        }

        // 정규화된 스크롤 위치 계산 (0~1)
        float normalizedPosition = Mathf.Clamp01(offset / scrollableHeight);

        // 부드럽게 스크롤
        SmoothScrollTo(1f - normalizedPosition).Forget();
    }

    /// <summary>
    /// 부드러운 스크롤
    /// </summary>
    private async UniTaskVoid SmoothScrollTo(float targetPosition)
    {
        float startPosition = storeScrollRect.verticalNormalizedPosition;
        float elapsed = 0f;
        float duration = 1f / scrollSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // EaseOutCubic 이징
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            storeScrollRect.verticalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, easedT);

            await UniTask.Yield();
        }

        storeScrollRect.verticalNormalizedPosition = targetPosition;
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}

/// <summary>
/// 상점 섹션 타입
/// </summary>
public enum StoreSectionType
{
    Gold = 0,
    Diamond = 1,
    Ticket = 2
}

/// <summary>
/// 상점 섹션 마커 - 각 섹션의 GameObject에 추가
/// </summary>
public class StoreSectionMarker : MonoBehaviour
{
    public StoreSectionType sectionType;
}
