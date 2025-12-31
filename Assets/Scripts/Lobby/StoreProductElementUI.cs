using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StoreProductElementUI : MonoBehaviour
{
    [Header("상품 설정")]
    [SerializeField] private int sellingId;

    [Header("UI References")]
    [SerializeField] private Image productIcon;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI limitText;
    [SerializeField] private Button buyButton;

    [Header("상태 표시")]
    [SerializeField] private GameObject soldOutOverlay;
    [SerializeField] private GameObject limitBadge;

    [Header("재화 아이콘")]
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite diamondIcon;
    [SerializeField] private Sprite cashIcon;

    [Header("가격 아이콘")]
    [SerializeField] private Image priceIcon;

    [Header("Popup")]
    [SerializeField] private UIPopupManager popupManager;

    private SecondStoreController storeController;
    private SellingData productData;

    public int SellingId => sellingId;


    private void Awake()
    {
        // UIPopupManager가 없으면 찾기 시도
        if (popupManager == null)
        {
            popupManager = FindObjectOfType<UIPopupManager>();

            if (popupManager == null)
            {
                Debug.LogWarning("[StoreProductElementUI] UIPopupManager를 찾을 수 없습니다. 팝업 기능이 제한됩니다.");
            }
        }

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void Start()
    {
        storeController = FindObjectOfType<SecondStoreController>();

        if (sellingId > 0)
            Setup(sellingId);
    }

    private void OnEnable()
    {
        if (sellingId > 0 && productData != null)
        {
            UpdateButtonState();
            UpdateLimitDisplay();
        }
    }

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(OnBuyClicked);
    }



    public void Setup(int id)
    {
        sellingId = id;
        productData = DataTableManager.SellingTable?.Get(sellingId);

        if (productData == null)
        {
            Debug.LogWarning($"[StoreProduct] 상품을 찾을 수 없음: {sellingId}");
            gameObject.SetActive(false);
            return;
        }

        UpdateUI();
    }


    public void UpdateUI()
    {
        if (productData == null) return;

        if (amountText != null)
            amountText.text = $"x{productData.SELLING_AMOUNT:N0}";

        if (priceText != null)
            priceText.text = $"{productData.SELLING_PRICE:N0}";

        if (priceIcon != null)
            priceIcon.sprite = GetPriceIcon();

        UpdateLimitDisplay();
        LoadProductIcon();
        UpdateButtonState();
    }

    private void UpdateLimitDisplay()
    {
        if (storeController == null)
            return;

        bool hasLimit = productData.SELLING_LIMIT > 0 && productData.SELLING_NUM > 0;

        if (limitBadge != null)
            limitBadge.SetActive(hasLimit);

        if (limitText != null)
        {
            limitText.text = storeController.GetLimitText(sellingId);
            limitText.gameObject.SetActive(hasLimit);
        }
    }

    public void UpdateButtonState()
    {
        if (storeController == null)
            return;

        bool isSoldOut = false;

        if (productData.SELLING_LIMIT > 0 && productData.SELLING_NUM > 0)
        {
            int remaining = storeController.GetRemainingPurchaseCount(sellingId);
            isSoldOut = remaining <= 0;
        }

        if (buyButton != null)
            buyButton.interactable = !isSoldOut;

        if (soldOutOverlay != null)
            soldOutOverlay.SetActive(isSoldOut);
    }


    private Sprite GetPriceIcon()
    {
        return productData.SELLING_MONEY switch
        {
            1 => goldIcon,
            2 => diamondIcon,
            3 => cashIcon,
            _ => null
        };
    }

    private void LoadProductIcon()
    {
        if (productIcon == null) return;

        var itemData = DataTableManager.ItemTable?.Get(productData.SELLING_ITEM);
        if (itemData == null) return;

        string iconAddress = itemData.ITEM_ICON;
        if (string.IsNullOrEmpty(iconAddress)) return;

        Addressables.LoadAssetAsync<Sprite>(iconAddress).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                productIcon.sprite = handle.Result;
            }
        };
    }

    private void OnBuyClicked()
    {
        if (storeController == null || productData == null)
            return;

        if (!storeController.HasEnoughCurrency(
            productData.SELLING_MONEY,
            productData.SELLING_PRICE))
        {
            ShowInsufficientCurrencyPopup();
            return;
        }

        if (productData.SELLING_LIMIT > 0 && productData.SELLING_NUM > 0)
        {
            int remaining = storeController.GetRemainingPurchaseCount(sellingId);
            if (remaining <= 0)
            {
                ShowPopup("구매 불가\n구매 가능 횟수를 초과했습니다.");
                return;
            }
        }

        storeController.OnClickBuyProduct(sellingId);
        Invoke(nameof(UpdateUI), 0.5f);
    }


    private void ShowInsufficientCurrencyPopup()
    {
        string currencyName = productData.SELLING_MONEY switch
        {
            1 => "골드",
            2 => "다이아몬드",
            3 => "캐시",
            _ => "재화"
        };

        ShowPopup($"{currencyName}가 부족합니다.\n 충전 후 다시 시도해주세요.");
    }

    /// <summary>
    /// 팝업 표시 (UIPopupManager가 있으면 사용, 없으면 로그만 출력)
    /// </summary>
    private void ShowPopup(string message)
    {
        if (popupManager != null)
        {
            popupManager.ShowAlert(message);
        }
        else
        {
            Debug.LogWarning($"[StoreProductElementUI] {message}");
        }
    }


    public void Refresh()
    {
        if (sellingId <= 0) return;

        productData = DataTableManager.SellingTable?.Get(sellingId);
        UpdateUI();
    }
}
