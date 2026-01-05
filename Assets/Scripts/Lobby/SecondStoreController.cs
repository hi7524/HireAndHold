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
    [SerializeField] private UIPopupManager popupManager;

    private bool isPurchasing = false;


    private enum PurchaseFailReason
    {
        None,
        PurchaseLimit,
        NotEnoughCurrency
    }

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
        if (confirmBuyButton != null)
        {
            confirmBuyButton.onClick.RemoveAllListeners();
            confirmBuyButton.onClick.AddListener(OnConfirmPurchase);
        }

        if (confirmCancelButton != null)
        {
            confirmCancelButton.onClick.RemoveAllListeners();
            confirmCancelButton.onClick.AddListener(OnCancelPurchase);
        }

        purchaseConfirmPanel?.SetActive(false);
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

        if (!CanPurchase(product, out var reason))
        {
            switch (reason)
            {
                case PurchaseFailReason.PurchaseLimit:
                    popupManager?.ShowAlertAsync("구매 제한 아이템입니다.").Forget();
                    break;

                case PurchaseFailReason.NotEnoughCurrency:
                    popupManager?.ShowAlertAsync("재화가 부족합니다.").Forget();
                    break;
            }
            return;
        }



        ShowConfirmPanel(product);
    }


    /// <summary>
    /// 구매 확인 패널 표시
    /// </summary>
    private void ShowConfirmPanel(SellingData product)
    {
        pendingProduct = product;

        confirmBuyButton.interactable = true;
        isPurchasing = false;

        purchaseConfirmPanel.SetActive(true);
    }


    /// <summary>
    /// 확인 버튼 클릭 - 실제 구매 진행
    /// </summary>
    private void OnConfirmPurchase()
    {
        if (isPurchasing)
            return;

        if (pendingProduct != null)
        {
            isPurchasing = true;
            confirmBuyButton.interactable = false;
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
    /// 구매 처리 (낙관적 업데이트 - 단, IAP는 동기 처리)
    /// </summary>
    private async UniTaskVoid PurchaseAsync(SellingData data)
    {
        if (!CanPurchase(data, out _))
        {
            ResetPurchaseState();
            return;
        }

        bool isIAP = data.SELLING_MONEY == 3;

        if (isIAP)
            await PurchaseIAPAsync(data);
        else
            PurchaseInGameCurrency(data);

        ResetPurchaseState();
    }
    private void ResetPurchaseState()
    {
        isPurchasing = false;
        if (confirmBuyButton != null)
            confirmBuyButton.interactable = true;
    }


    /// <summary>
    /// IAP 구매 처리 (Firebase 저장 완료 후 아이템 지급)
    /// </summary>
    private async UniTask PurchaseIAPAsync(SellingData data)
    {
        Debug.Log($"[SecondStore] IAP 구매 시작: {data.SELLING_ID}");

        // 아이템 지급 Firebase (await로 완료 보장)
        // GiveItemFirebaseAsync 내부에서 로컬 캐시도 업데이트됨
        await GiveItemFirebaseAsync(data.SELLING_ITEM, data.SELLING_AMOUNT);

        // 구매 기록 저장 Firebase
        if (data.SELLING_LIMIT > 0 && data.SELLING_NUM > 0)
        {
            await DatabaseManager.Instance.AddPurchaseRecordAsync(data.SELLING_ID);
            // AddPurchaseRecordAsync는 로컬 캐시를 업데이트하지 않으므로 별도 처리
            DatabaseManager.Instance.AddPurchaseRecordLocal(data.SELLING_ID);
        }

        // GiveItemFirebaseAsync에서 이미 로컬 캐시가 업데이트되므로
        // GiveItemLocal 호출 제거 (중복 지급 방지)

        PlayData.SyncItemsFromDatabase();
        PlayData.NotifyCurrencyChanged();

        Debug.Log($"[SecondStore] IAP 구매 완료: {data.SELLING_ID}");

        OnPurchaseCompleted?.Invoke(data.SELLING_ID);
        RefreshAllProductUI();
    }

    /// <summary>
    /// 인게임 재화 구매 처리 (낙관적 업데이트)
    /// </summary>
    private void PurchaseInGameCurrency(SellingData data)
    {
        // 로컬 캐시 즉시 업데이트 (재화 차감)
        DeductCurrencyLocal(data.SELLING_MONEY, data.SELLING_PRICE);

        // 로컬 캐시 즉시 업데이트 (아이템 지급)
        GiveItemLocal(data.SELLING_ITEM, data.SELLING_AMOUNT);

        // 구매 기록 로컬 저장 (제한 상품인 경우)
        if (data.SELLING_LIMIT > 0 && data.SELLING_NUM > 0)
        {
            DatabaseManager.Instance.AddPurchaseRecordLocal(data.SELLING_ID);
        }

        PlayData.SyncItemsFromDatabase();
        PlayData.NotifyCurrencyChanged();

        Debug.Log($"[SecondStore] 구매 성공: {data.SELLING_ID}");

        OnPurchaseCompleted?.Invoke(data.SELLING_ID);
        RefreshAllProductUI();

        // Firebase 저장은 백그라운드로 처리
        var saveTasks = new System.Collections.Generic.List<UniTask>();

        saveTasks.Add(DeductCurrencyFirebaseAsync(data.SELLING_MONEY, data.SELLING_PRICE));
        saveTasks.Add(GiveItemFirebaseAsync(data.SELLING_ITEM, data.SELLING_AMOUNT));

        if (data.SELLING_LIMIT > 0 && data.SELLING_NUM > 0)
        {
            saveTasks.Add(DatabaseManager.Instance.AddPurchaseRecordAsync(data.SELLING_ID));
        }

        PendingSaveManager.Track(UniTask.WhenAll(saveTasks));
    }

    /// <summary>
    /// 재화 차감 (로컬 캐시만)
    /// </summary>
    private void DeductCurrencyLocal(int moneyType, int price)
    {
        switch (moneyType)
        {
            case 1: // 골드
                PlayData.SetGoldImmediate(PlayData.Gold - price);
                break;
            case 2: // 다이아
                PlayData.SetDiamondImmediate(PlayData.Diamond - price);
                break;
            case 3: // 현금 (IAP)
                Debug.Log("[SecondStore] IAP 결제 (테스트 모드 - 무료 통과)");
                break;
        }
    }

    /// <summary>
    /// 아이템 지급 (로컬 캐시만)
    /// </summary>
    private void GiveItemLocal(int itemId, int amount)
    {
        switch (itemId)
        {
            case 0:
                PlayData.SetGoldImmediate(PlayData.Gold + amount);
                break;
            case 5107:
                PlayData.SetDiamondImmediate(PlayData.Diamond + amount);
                break;
            case 5201: 
                PlayData.SetEnhanceStoneImmediate(PlayData.EnhanceStone + amount);
                break;
            default:
                var itemData = DataTableManager.ItemTable?.Get(itemId);
                if (itemData != null && itemData.PACKAGE_ID > 0) return;
                PlayData.SetItemCountImmediate(itemId, PlayData.GetItemCount(itemId) + amount);
                break;
        }
    }


    /// <summary>
    /// 재화 차감 (Firebase만)
    /// </summary>
    private UniTask DeductCurrencyFirebaseAsync(int moneyType, int price)
    {
        switch (moneyType)
        {
            case 1: // 골드
                return DatabaseManager.Instance.AddGoldAsync(-price);
            case 2: // 다이아
                return DatabaseManager.Instance.AddDiamondAsync(-price);
            case 3: // 현금 (IAP)
                return UniTask.CompletedTask;
            default:
                return UniTask.CompletedTask;
        }
    }

    /// <summary>
    /// 아이템 지급 (Firebase만)
    /// </summary>
    private UniTask GiveItemFirebaseAsync(int itemId, int amount)
    {
        switch (itemId)
        {
            case 0:
                return DatabaseManager.Instance.AddGoldAsync(amount);
            case 5107:
                return DatabaseManager.Instance.AddDiamondAsync(amount);
            case 5201: 
                return DatabaseManager.Instance.AddEnhanceStoneAsync(amount);
            default:
                var itemData = DataTableManager.ItemTable?.Get(itemId);
                if (itemData != null && itemData.PACKAGE_ID > 0)
                    return DatabaseManager.Instance.SendPackageMailAsync(itemData.PACKAGE_ID, amount);

                return DatabaseManager.Instance.AddItemAsync(itemId, amount);
        }
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
    private bool CanPurchase(SellingData data, out PurchaseFailReason reason)
    {
        reason = PurchaseFailReason.None;

        if (data == null)
            return false;

        if (!CheckPurchaseLimit(data))
        {
            reason = PurchaseFailReason.PurchaseLimit;
            return false;
        }

        if (!HasEnoughCurrency(data.SELLING_MONEY, data.SELLING_PRICE))
        {
            reason = PurchaseFailReason.NotEnoughCurrency;
            return false;
        }

        return true;
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
