using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// SecondStoreWindow 컨트롤러
/// 기존 UI 버튼에 직접 연결해서 사용
/// </summary>
public class SecondStoreController : MonoBehaviour
{
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

        PurchaseAsync(product).Forget();
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
            // TODO: 재화 부족 UI 표시
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

        // 캐시 동기화
        PlayData.SyncItemsFromDatabase();

        Debug.Log($"[SecondStore] 구매 성공: {data.SELLING_ID}");
        // TODO: 구매 성공 UI 표시
    }

    /// <summary>
    /// 구매 가능 여부 확인
    /// </summary>
    public bool CanPurchase(SellingData data)
    {
        if (data == null) return false;
        return HasEnoughCurrency(data.SELLING_MONEY, data.SELLING_PRICE);
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
                return await DatabaseManager.Instance.AddItemAsync(5102, -price);
            case 3: // 현금 (IAP)
                // TODO: IAP 결제 처리
                Debug.Log("[SecondStore] IAP 결제 처리 필요");
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
        if (itemId == 0)
        {
            // 아이템 ID가 0이면 골드 지급
            return await DatabaseManager.Instance.AddGoldAsync(amount);
        }
        return await DatabaseManager.Instance.AddItemAsync(itemId, amount);
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
}
