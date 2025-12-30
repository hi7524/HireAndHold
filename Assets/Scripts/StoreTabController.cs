using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 상점 탭 타입
/// </summary>
public enum StoreTabType
{
    Diamond,   // 다이아 상점
    Gold,      // 골드 상점
    Item,      // 아이템 상점
    Package,   // 패키지 상점
    Special    // 특가 상점
}

/// <summary>
/// 상점 탭 컨트롤러
/// </summary>
public class StoreTabController : MonoBehaviour
{
    [Header("탭 버튼들")]
    [SerializeField] private Button diamondTabButton;
    [SerializeField] private Button goldTabButton;
    [SerializeField] private Button itemTabButton;
    [SerializeField] private Button packageTabButton;

    [Header("탭 컨텐츠들")]
    [SerializeField] private GameObject diamondContent;
    [SerializeField] private GameObject goldContent;
    [SerializeField] private GameObject itemContent;
    [SerializeField] private GameObject packageContent;

    [Header("탭 선택 표시 (선택사항)")]
    [SerializeField] private GameObject diamondSelectedIndicator;
    [SerializeField] private GameObject goldSelectedIndicator;
    [SerializeField] private GameObject itemSelectedIndicator;
    [SerializeField] private GameObject packageSelectedIndicator;

    private StoreTabType currentTab = StoreTabType.Diamond;
    private Dictionary<StoreTabType, Button> tabButtons;
    private Dictionary<StoreTabType, GameObject> tabContents;
    private Dictionary<StoreTabType, GameObject> tabIndicators;

    private void Awake()
    {
        InitializeTabs();
        SetupButtonListeners();
    }

    private void OnEnable()
    {
        // 상점이 열릴 때 현재 탭 활성화
        SelectTab(currentTab);
    }

    /// <summary>
    /// 탭 딕셔너리 초기화
    /// </summary>
    private void InitializeTabs()
    {
        // 버튼 딕셔너리
        tabButtons = new Dictionary<StoreTabType, Button>
        {
            { StoreTabType.Diamond, diamondTabButton },
            { StoreTabType.Gold, goldTabButton },
            { StoreTabType.Item, itemTabButton },
            { StoreTabType.Package, packageTabButton }
        };

        // 컨텐츠 딕셔너리
        tabContents = new Dictionary<StoreTabType, GameObject>
        {
            { StoreTabType.Diamond, diamondContent },
            { StoreTabType.Gold, goldContent },
            { StoreTabType.Item, itemContent },
            { StoreTabType.Package, packageContent }
        };

        // 선택 표시 딕셔너리
        tabIndicators = new Dictionary<StoreTabType, GameObject>
        {
            { StoreTabType.Diamond, diamondSelectedIndicator },
            { StoreTabType.Gold, goldSelectedIndicator },
            { StoreTabType.Item, itemSelectedIndicator },
            { StoreTabType.Package, packageSelectedIndicator }
        };
    }

    /// <summary>
    /// 버튼 리스너 설정
    /// </summary>
    private void SetupButtonListeners()
    {
        if (diamondTabButton != null)
            diamondTabButton.onClick.AddListener(() => SelectTab(StoreTabType.Diamond));

        if (goldTabButton != null)
            goldTabButton.onClick.AddListener(() => SelectTab(StoreTabType.Gold));

        if (itemTabButton != null)
            itemTabButton.onClick.AddListener(() => SelectTab(StoreTabType.Item));

        if (packageTabButton != null)
            packageTabButton.onClick.AddListener(() => SelectTab(StoreTabType.Package));

    }

    /// <summary>
    /// 탭 선택 (외부에서 호출 가능)
    /// </summary>
    public void SelectTab(StoreTabType tabType)
    {
        currentTab = tabType;

        // 모든 컨텐츠 비활성화
        foreach (var content in tabContents.Values)
        {
            if (content != null)
                content.SetActive(false);
        }

        // 모든 선택 표시 비활성화
        foreach (var indicator in tabIndicators.Values)
        {
            if (indicator != null)
                indicator.SetActive(false);
        }

        // 선택된 탭만 활성화
        if (tabContents.TryGetValue(tabType, out GameObject selectedContent) && selectedContent != null)
        {
            selectedContent.SetActive(true);
        }

        // 선택된 탭 표시 활성화
        if (tabIndicators.TryGetValue(tabType, out GameObject selectedIndicator) && selectedIndicator != null)
        {
            selectedIndicator.SetActive(true);
        }

        // 버튼 상태 업데이트 (interactable로 표시)
        UpdateButtonStates(tabType);

        Debug.Log($"[StoreTabController] 탭 전환: {tabType}");
    }

    /// <summary>
    /// 버튼 상태 업데이트
    /// </summary>
    private void UpdateButtonStates(StoreTabType selectedTab)
    {
        foreach (var kvp in tabButtons)
        {
            if (kvp.Value != null)
            {
                // 선택된 탭은 비활성화, 나머지는 활성화
                kvp.Value.interactable = (kvp.Key != selectedTab);
            }
        }
    }

    /// <summary>
    /// 현재 탭 가져오기
    /// </summary>
    public StoreTabType GetCurrentTab()
    {
        return currentTab;
    }

    private void OnDestroy()
    {
        // 리스너 제거
        if (diamondTabButton != null)
            diamondTabButton.onClick.RemoveAllListeners();

        if (goldTabButton != null)
            goldTabButton.onClick.RemoveAllListeners();

        if (itemTabButton != null)
            itemTabButton.onClick.RemoveAllListeners();

        if (packageTabButton != null)
            packageTabButton.onClick.RemoveAllListeners();

    }
}
