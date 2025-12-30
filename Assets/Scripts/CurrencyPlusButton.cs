using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 재화 + 버튼 - 상점 열고 해당 탭으로 이동
/// </summary>
public class CurrencyPlusButton : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CurrencyType currencyType;

    [Header("References")]
    [SerializeField] private Button plusButton;
    [SerializeField] private GameObject storeWindow;
    [SerializeField] private StoreTabController tabController;

    /// <summary>
    /// 재화 타입
    /// </summary>
    public enum CurrencyType
    {
        Gold = 1,      // 골드 상점
        Diamond = 2,   // 다이아 상점
        Energy = 3     // 에너지 상점 (필요시)
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
        // 상점 윈도우 열기
        if (storeWindow != null && !storeWindow.activeSelf)
        {
            storeWindow.SetActive(true);
        }

        // TabController가 없으면 찾기
        if (tabController == null)
        {
            tabController = storeWindow?.GetComponentInChildren<StoreTabController>();
        }

        // 해당 재화 타입의 탭으로 전환
        if (tabController != null)
        {
            StoreTabType targetTab = ConvertToTabType(currencyType);
            tabController.SelectTab(targetTab);
        }
        else
        {
            Debug.LogWarning("[CurrencyPlusButton] StoreTabController를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// CurrencyType을 StoreTabType으로 변환
    /// </summary>
    private StoreTabType ConvertToTabType(CurrencyType currency)
    {
        return currency switch
        {
            CurrencyType.Diamond => StoreTabType.Diamond,
            CurrencyType.Gold => StoreTabType.Gold,
            CurrencyType.Energy => StoreTabType.Item, // 에너지는 아이템 탭
            _ => StoreTabType.Diamond
        };
    }

    private void OnDestroy()
    {
        if (plusButton != null)
        {
            plusButton.onClick.RemoveListener(OnPlusButtonClicked);
        }
    }
}
