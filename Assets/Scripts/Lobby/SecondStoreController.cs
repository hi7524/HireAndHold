using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// SecondStoreWindow 컨트롤러
/// 기존 UI 버튼에 직접 연결해서 사용
/// </summary>
public class SecondStoreController : MonoBehaviour
{
    [Header("구매 확인 패널")]
    [SerializeField] private GameObject purchaseConfirmPanel;
    [SerializeField] private TextMeshProUGUI confirmProductNameText;
    [SerializeField] private TextMeshProUGUI confirmPriceText;
    [SerializeField] private Image confirmProductIcon;
    [SerializeField] private Button confirmBuyButton;
    [SerializeField] private Button confirmCancelButton;

    /// <summary>
    /// 구매 완료 이벤트 (sellingId)
    /// </summary>
    public static event Action<int> OnPurchaseCompleted;

    /// <summary>
    /// 현재 확인 대기 중인 상품
    /// </summary>
    private SellingData pendingProduct;

    private void Awake()
    {
        // 확인 버튼 이벤트 연결
        if (confirmBuyButton != null)
        {
            confirmBuyButton.onClick.AddListener(OnConfirmPurchase);
        }

        // 취소 버튼 이벤트 연결
        if (confirmCancelButton != null)
        {
            confirmCancelButton.onClick.AddListener(OnCancelPurchase);
        }

        // 초기에는 확인 패널 숨김
        if (purchaseConfirmPanel != null)
        {
            purchaseConfirmPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 상품 구매 버튼 클릭 (Button OnClick에서 호출)
    /// sellingId: SellingTable의 SELLING_ID
    /// </summary>
    public void OnClickBuyProduct(int sellingId)
    {
        var product = DataTableManager.SellingTable?.Get(sellingId);
        if (product == null)
        {
            Debug.LogError($"[SecondStore] 상품을 찾을 수 없음: {sellingId}");
            return;
        }

        // 구매 가능 여부 먼저 확인
        if (!CanPurchase(product))
        {
            Debug.LogWarning("[SecondStore] 구매 불가 (재화 부족 또는 제한 초과)");
            return;
        }

        // 확인 팝업 표시
        ShowConfirmPanel(product);
    }

    /// <summary>
    /// 구매 확인 패널 표시
    /// </summary>
    private void ShowConfirmPanel(SellingData product)
    {
        pendingProduct = product;

        if (purchaseConfirmPanel != null)
        {
            // 상품 정보 표시
            if (confirmProductNameText != null)
            {
                confirmProductNameText.text = GetProductName(product.SELLING_ID);
            }

            if (confirmPriceText != null)
            {
                confirmPriceText.text = GetPriceText(product.SELLING_ID);
            }

            if (confirmProductIcon != null)
            {
                // 아이콘 로드 (Addressable 캐시에서)
                var itemData = DataTableManager.ItemTable?.Get(product.SELLING_ITEM);
                if (itemData != null && AddressablePreloader.Instance != null)
                {
                    var iconSprite = AddressablePreloader.Instance.GetCachedSprite(itemData.ITEM_ICON);
                    if (iconSprite != null)
                    {
                        confirmProductIcon.sprite = iconSprite;
                    }
                }
            }

            purchaseConfirmPanel.SetActive(true);
        }
        else
        {
            // 확인 패널이 없으면 바로 구매 진행
            PurchaseAsync(product).Forget();
        }
    }

    /// <summary>
    /// 확인 버튼 클릭 - 실제 구매 진행
    /// </summary>
    private void OnConfirmPurchase()
    {
        if (pendingProduct != null)
        {
            PurchaseAsync(pendingProduct).Forget();
        }

        HideConfirmPanel();
    }

    /// <summary>
    /// 취소 버튼 클릭
    /// </summary>
    private void OnCancelPurchase()
    {
        pendingProduct = null;
        HideConfirmPanel();
    }

    /// <summary>
    /// 확인 패널 숨김
    /// </summary>
    private void HideConfirmPanel()
    {
        if (purchaseConfirmPanel != null)
        {
            purchaseConfirmPanel.SetActive(false);
        }
        pendingProduct = null;
    }

    /// <summary>
    /// 구매 처리
    /// </summary>
    private async UniTaskVoid PurchaseAsync(SellingData data)
    {
        // 구매 가능 여부 확인
        if (!CanPurchase(data))
        {
            Debug.LogWarning("[SecondStore] 재화가 부족합니다.");
            return;
        }

        // 재화 차감
        bool deductSuccess = await DeductCurrencyAsync(data.SELLING_MONEY, data.SELLING_PRICE);
        if (!deductSuccess)
        {
            Debug.LogError("[SecondStore] 재화 차감 실패");
            return;
        }

        // 아이템 지급
        bool giveSuccess = await GiveItemAsync(data.SELLING_ITEM, data.SELLING_AMOUNT);
        if (!giveSuccess)
        {
            Debug.LogError("[SecondStore] 아이템 지급 실패");
            return;
        }

        // 구매 기록 저장 (제한 상품인 경우)
        if (data.SELLING_LIMIT > 0 && data.SELLING_NUM > 0)
        {
            await SavePurchaseRecord(data.SELLING_ID);
        }

        // ⭐ 캐시 동기화 - await 추가!
        await UniTask.DelayFrame(1); // DB 업데이트 대기
        PlayData.SyncItemsFromDatabase();
        await UniTask.DelayFrame(1); // 동기화 완료 대기

        Debug.Log($"[SecondStore] 구매 성공: {data.SELLING_ID}");

        // 구매 완료 이벤트 발생
        OnPurchaseCompleted?.Invoke(data.SELLING_ID);

        // 모든 상품 UI 갱신
        RefreshAllProductUI();
    }

    /// <summary>
    /// 모든 상품 UI 갱신
    /// </summary>
    public void RefreshAllProductUI()
    {
        var productElements = FindObjectsByType<StoreProductElementUI>(FindObjectsSortMode.None);
        foreach (var element in productElements)
        {
            element.Refresh();
        }
    }

    /// <summary>
    /// 구매 가능 여부 확인
    /// </summary>
    public bool CanPurchase(SellingData data)
    {
        if (data == null) return false;

        // 구매 제한 체크
        if (!CheckPurchaseLimit(data)) return false;

        // 재화 체크
        return HasEnoughCurrency(data.SELLING_MONEY, data.SELLING_PRICE);
    }

    /// <summary>
    /// 구매 제한 타입
    /// </summary>
    public enum PurchaseLimitType
    {
        Unlimited = 0,      // 무제한
        AccountLimit = 1,   // 계정당 제한
        DailyLimit = 2,     // 일일 제한
        WeeklyLimit = 3,    // 주간 제한
        MonthlyLimit = 4    // 월간 제한
    }

    /// <summary>
    /// 구매 제한 체크
    /// SELLING_LIMIT: 제한 타입 (0=무제한, 1=계정당, 2=일일, 3=첫구매, 4=첫구매2)
    /// SELLING_NUM: 구매 가능 횟수
    /// </summary>
    private bool CheckPurchaseLimit(SellingData data)
    {
        // 제한 없음
        if (data.SELLING_LIMIT == 0 || data.SELLING_NUM == 0) return true;

        var limitType = (PurchaseLimitType)data.SELLING_LIMIT;

        switch (limitType)
        {
            case PurchaseLimitType.Unlimited:
                return true;

            case PurchaseLimitType.AccountLimit:
                // 계정 전체 구매 횟수 체크
                int totalCount = GetPurchasedCount(data.SELLING_ID);
                return totalCount < data.SELLING_NUM;

            case PurchaseLimitType.DailyLimit:
                // 오늘 구매 횟수 체크
                int todayCount = GetTodayPurchasedCount(data.SELLING_ID);
                return todayCount < data.SELLING_NUM;

            case PurchaseLimitType.WeeklyLimit:
                // 이번 주 구매 횟수 체크
                int weekCount = GetWeeklyPurchasedCount(data.SELLING_ID);
                return weekCount < data.SELLING_NUM;

            case PurchaseLimitType.MonthlyLimit:
                // 이번 달 구매 횟수 체크
                int monthCount = GetMonthlyPurchasedCount(data.SELLING_ID);
                return monthCount < data.SELLING_NUM;

            default:
                return true;
        }
    }

    /// <summary>
    /// 유저의 해당 상품 구매 횟수 조회 (전체)
    /// </summary>
    private int GetPurchasedCount(int sellingId)
    {
        return DatabaseManager.Instance.GetTotalPurchaseCount(sellingId);
    }

    /// <summary>
    /// 유저의 오늘 구매 횟수 조회
    /// </summary>
    private int GetTodayPurchasedCount(int sellingId)
    {
        return DatabaseManager.Instance.GetTodayPurchaseCount(sellingId);
    }

    /// <summary>
    /// 유저의 이번 주 구매 횟수 조회
    /// </summary>
    private int GetWeeklyPurchasedCount(int sellingId)
    {
        return DatabaseManager.Instance.GetWeeklyPurchaseCount(sellingId);
    }

    /// <summary>
    /// 유저의 이번 달 구매 횟수 조회
    /// </summary>
    private int GetMonthlyPurchasedCount(int sellingId)
    {
        return DatabaseManager.Instance.GetMonthlyPurchaseCount(sellingId);
    }

    /// <summary>
    /// 구매 횟수 저장
    /// </summary>
    private async UniTask SavePurchaseRecord(int sellingId)
    {
        await DatabaseManager.Instance.AddPurchaseRecordAsync(sellingId);
    }

    /// <summary>
    /// 재화 보유량 확인
    /// </summary>
    public bool HasEnoughCurrency(int moneyType, int price)
    {
        switch (moneyType)
        {
            case 1: // 골드
                return PlayData.HasEnoughGold(price);
            case 2: // 다이아
                return PlayData.Diamond >= price;
            case 3: // 현금 (IAP)
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 재화 차감
    /// </summary>
    private async UniTask<bool> DeductCurrencyAsync(int moneyType, int price)
    {
        switch (moneyType)
        {
            case 1: // 골드
                return await DatabaseManager.Instance.AddGoldAsync(-price);
            case 2: // 다이아
                return await DatabaseManager.Instance.AddDiamondAsync(-price);
            case 3: // 현금 (IAP)
                // TODO: 실제 IAP 결제 연동 시 여기에 구현
                Debug.Log("[SecondStore] IAP 결제 (테스트 모드 - 무료 통과)");
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 아이템 지급
    /// </summary>
    private async UniTask<bool> GiveItemAsync(int itemId, int amount)
    {
        switch (itemId)
        {
            case 0:
                // 골드 지급
                return await DatabaseManager.Instance.AddGoldAsync(amount);
            case 5107:
                // 다이아 지급
                return await DatabaseManager.Instance.AddDiamondAsync(amount);
            default:
                // 패키지 아이템인지 확인
                var itemData = DataTableManager.ItemTable?.Get(itemId);
                if (itemData != null && itemData.PACKAGE_ID > 0)
                {
                    // 패키지 내용물 지급
                    return await GivePackageItemsAsync(itemData.PACKAGE_ID, amount);
                }

                // 일반 아이템 지급
                return await DatabaseManager.Instance.AddItemAsync(itemId, amount);
        }
    }

    /// <summary>
    /// 패키지 내용물을 우편으로 발송
    /// </summary>
    private async UniTask<bool> GivePackageItemsAsync(int packageId, int packageAmount)
    {
        bool success = await DatabaseManager.Instance.SendPackageMailAsync(packageId, packageAmount);

        if (success)
        {
            Debug.Log($"[SecondStore] 패키지 {packageId} 우편 발송 완료 (수량: {packageAmount})");
        }
        else
        {
            Debug.LogError($"[SecondStore] 패키지 {packageId} 우편 발송 실패");
        }

        return success;
    }

    /// <summary>
    /// 상품명 가져오기 (UI 텍스트 설정용)
    /// </summary>
    public string GetProductName(int sellingId)
    {
        var product = DataTableManager.SellingTable?.Get(sellingId);
        if (product == null) return "";

        var name = DataTableManager.GetString(int.Parse(product.SELLING_NAME));
        return name ?? $"상품 {sellingId}";
    }

    /// <summary>
    /// 가격 텍스트 가져오기 (UI 텍스트 설정용)
    /// </summary>
    public string GetPriceText(int sellingId)
    {
        var product = DataTableManager.SellingTable?.Get(sellingId);
        if (product == null) return "";

        string currencySymbol = product.SELLING_MONEY switch
        {
            1 => "G",      // 골드
            2 => "D",      // 다이아
            3 => "₩",      // 현금
            _ => ""
        };

        return $"{currencySymbol}{product.SELLING_PRICE:N0}";
    }

    /// <summary>
    /// 수량 텍스트 가져오기
    /// </summary>
    public string GetAmountText(int sellingId)
    {
        var product = DataTableManager.SellingTable?.Get(sellingId);
        if (product == null) return "";

        return product.SELLING_AMOUNT > 1 ? $"x{product.SELLING_AMOUNT}" : "";
    }

    /// <summary>
    /// 제한 타입 가져오기
    /// </summary>
    public PurchaseLimitType GetLimitType(int sellingId)
    {
        var product = DataTableManager.SellingTable?.Get(sellingId);
        if (product == null) return PurchaseLimitType.Unlimited;

        return (PurchaseLimitType)product.SELLING_LIMIT;
    }

    /// <summary>
    /// 남은 구매 가능 횟수 가져오기
    /// </summary>
    public int GetRemainingPurchaseCount(int sellingId)
    {
        var product = DataTableManager.SellingTable?.Get(sellingId);
        if (product == null) return 0;

        // 무제한이면 -1 반환
        if (product.SELLING_LIMIT == 0 || product.SELLING_NUM == 0) return -1;

        var limitType = (PurchaseLimitType)product.SELLING_LIMIT;

        int purchasedCount = limitType switch
        {
            PurchaseLimitType.DailyLimit => GetTodayPurchasedCount(sellingId),
            PurchaseLimitType.WeeklyLimit => GetWeeklyPurchasedCount(sellingId),
            PurchaseLimitType.MonthlyLimit => GetMonthlyPurchasedCount(sellingId),
            _ => GetPurchasedCount(sellingId)
        };

        return Mathf.Max(0, product.SELLING_NUM - purchasedCount);
    }

    /// <summary>
    /// 제한 표시 텍스트 (UI용)
    /// </summary>
    public string GetLimitText(int sellingId)
    {
        var product = DataTableManager.SellingTable?.Get(sellingId);
        if (product == null) return "";

        var limitType = (PurchaseLimitType)product.SELLING_LIMIT;

        int remaining = GetRemainingPurchaseCount(sellingId);

        switch (limitType)
        {
            case PurchaseLimitType.Unlimited:
                return "";
            case PurchaseLimitType.AccountLimit:
                return $"({remaining}/{product.SELLING_NUM})";
            case PurchaseLimitType.DailyLimit:
                return $"일일 ({remaining}/{product.SELLING_NUM})";
            case PurchaseLimitType.WeeklyLimit:
                return $"주간 ({remaining}/{product.SELLING_NUM})";
            case PurchaseLimitType.MonthlyLimit:
                return $"월간 ({remaining}/{product.SELLING_NUM})";
            default:
                return "";
        }
    }
}
