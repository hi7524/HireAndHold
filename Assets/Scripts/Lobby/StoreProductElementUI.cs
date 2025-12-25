using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 상점 상품 버튼 UI
/// DiamondButton 등 상품 버튼 프리팹에 붙여서 사용
/// </summary>
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

    private SecondStoreController storeController;
    private SellingData productData;

    public int SellingId => sellingId;

    private void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void Start()
    {
        // SecondStoreController 찾기
        storeController = FindObjectOfType<SecondStoreController>();

        if (sellingId > 0)
        {
            Setup(sellingId);
        }
    }

    /// <summary>
    /// 상품 ID로 초기화
    /// </summary>
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

    /// <summary>
    /// UI 갱신
    /// </summary>
    public void UpdateUI()
    {
        if (productData == null) return;

        // 수량 텍스트
        if (amountText != null)
        {
            if (productData.SELLING_AMOUNT > 1)
                amountText.text = $"x{productData.SELLING_AMOUNT}";
            else
                amountText.text = "";
        }

        // 가격 텍스트
        if (priceText != null)
        {
            priceText.text = GetPriceText();
        }

        // 가격 아이콘
        if (priceIcon != null)
        {
            priceIcon.sprite = GetPriceIcon();
        }

        // 제한 텍스트
        UpdateLimitDisplay();

        // 상품 아이콘 로드
        LoadProductIcon();

        // 버튼 상태 갱신
        UpdateButtonState();
    }

    /// <summary>
    /// 제한 표시 갱신
    /// </summary>
    private void UpdateLimitDisplay()
    {
        if (storeController == null)
            storeController = FindObjectOfType<SecondStoreController>();

        bool hasLimit = productData.SELLING_LIMIT > 0 && productData.SELLING_NUM > 0;

        if (limitBadge != null)
            limitBadge.SetActive(hasLimit);

        if (limitText != null && storeController != null)
        {
            limitText.text = storeController.GetLimitText(sellingId);
            limitText.gameObject.SetActive(hasLimit);
        }
    }

    /// <summary>
    /// 버튼 상태 갱신
    /// </summary>
    public void UpdateButtonState()
    {
        if (storeController == null)
            storeController = FindObjectOfType<SecondStoreController>();

        bool canPurchase = storeController != null && storeController.CanPurchase(productData);
        bool isSoldOut = false;

        // 구매 제한 체크
        if (productData.SELLING_LIMIT > 0 && productData.SELLING_NUM > 0)
        {
            int remaining = storeController?.GetRemainingPurchaseCount(sellingId) ?? 0;
            isSoldOut = remaining <= 0;
        }

        // 버튼 상호작용
        if (buyButton != null)
        {
            buyButton.interactable = canPurchase && !isSoldOut;
        }

        // 품절 오버레이
        if (soldOutOverlay != null)
        {
            soldOutOverlay.SetActive(isSoldOut);
        }
    }

    /// <summary>
    /// 가격 텍스트 생성
    /// </summary>
    private string GetPriceText()
    {
        if (productData.SELLING_MONEY == 3) // 현금
            return $"{productData.SELLING_PRICE:N0}";
        else
            return $"{productData.SELLING_PRICE:N0}";
    }

    /// <summary>
    /// 가격 아이콘 가져오기
    /// </summary>
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

    /// <summary>
    /// 상품 아이콘 로드
    /// </summary>
    private void LoadProductIcon()
    {
        if (productIcon == null) return;

        // 아이템 테이블에서 아이콘 주소 가져오기
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

    /// <summary>
    /// 구매 버튼 클릭
    /// </summary>
    private void OnBuyClicked()
    {
        if (storeController == null)
        {
            storeController = FindObjectOfType<SecondStoreController>();
        }

        if (storeController != null)
        {
            storeController.OnClickBuyProduct(sellingId);

            // 구매 후 UI 갱신 (약간의 딜레이 후)
            Invoke(nameof(UpdateUI), 0.5f);
        }
    }

    /// <summary>
    /// 외부에서 강제 갱신
    /// </summary>
    public void Refresh()
    {
        if (sellingId > 0)
        {
            productData = DataTableManager.SellingTable?.Get(sellingId);
            UpdateUI();
        }
    }

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(OnBuyClicked);
    }

    private void OnEnable()
    {
        // 활성화될 때마다 상태 갱신
        if (sellingId > 0 && productData != null)
        {
            UpdateButtonState();
            UpdateLimitDisplay();
        }
    }
}
