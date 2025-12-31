using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
/// <summary>
/// 재화 + 버튼 - 상점 열고 해당 탭으로 이동
/// </summary>
public class CurrencyPlusButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Settings")]
    [SerializeField] private CurrencyType currencyType;
    [Header("References")]
    [SerializeField] private Button plusButton;
    [SerializeField] private GameObject storeWindow;
    [SerializeField] private StoreTabController tabController;

    private Image buttonImage;
    private Color originalColor;
    private Color pressedColor;
    private bool isPressed = false;

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

            // 버튼을 항상 활성화 상태로 유지
            plusButton.interactable = true;

            // 버튼 이미지 가져오기
            buttonImage = plusButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                // ColorBlock에서 색상 가져오기
                ColorBlock colors = plusButton.colors;
                originalColor = colors.normalColor;
                pressedColor = colors.pressedColor;

                // 초기 색상을 normalColor로 설정
                buttonImage.color = originalColor;
            }

            // Transition을 None으로 설정하여 수동으로 제어
            plusButton.transition = Selectable.Transition.None;

            // Navigation을 None으로 설정
            Navigation nav = plusButton.navigation;
            nav.mode = Navigation.Mode.None;
            plusButton.navigation = nav;
        }
    }

    private void OnEnable()
    {
        // 활성화될 때마다 색상 초기화
        if (buttonImage != null)
        {
            buttonImage.color = originalColor;
            isPressed = false;
        }

        if (plusButton != null)
        {
            plusButton.interactable = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 버튼을 눌렀을 때 색상 변경
        if (buttonImage != null && plusButton != null && plusButton.interactable)
        {
            isPressed = true;
            buttonImage.color = pressedColor;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 버튼에서 손을 뗐을 때 즉시 원래 색상으로 복구
        if (buttonImage != null)
        {
            isPressed = false;
            buttonImage.color = originalColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 버튼 영역을 벗어났을 때도 원래 색상으로 복구
        if (buttonImage != null && isPressed)
        {
            isPressed = false;
            buttonImage.color = originalColor;
        }
    }

    /// <summary>
    /// + 버튼 클릭
    /// </summary>
    private void OnPlusButtonClicked()
    {
        // EventSystem의 선택 해제하여 버튼이 선택 상태로 남지 않도록
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // 즉시 원래 색상으로 복구
        if (buttonImage != null)
        {
            isPressed = false;
            buttonImage.color = originalColor;
        }

        // 버튼 활성화 상태 유지
        if (plusButton != null)
        {
            plusButton.interactable = true;
        }

        // 상점 윈도우 열기
        if (storeWindow != null)
        {
            if (!storeWindow.activeSelf)
            {
                storeWindow.SetActive(true);
            }

            // TabController가 없으면 찾기
            if (tabController == null)
            {
                tabController = storeWindow.GetComponentInChildren<StoreTabController>();
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

        // 다음 프레임에 색상 재확인 (안전장치)
        StartCoroutine(ResetColorNextFrame());
    }

    private IEnumerator ResetColorNextFrame()
    {
        yield return null;

        if (buttonImage != null)
        {
            buttonImage.color = originalColor;
        }

        if (plusButton != null)
        {
            plusButton.interactable = true;
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
