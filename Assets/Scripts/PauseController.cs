using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

public class PauseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private PassiveSkillManager passiveSkillManager;
    [SerializeField] private DragManager dragManager;
    [SerializeField] private GameObject panelRoot;

    [Header("UI Elements")]
    [SerializeField] private Button lobbbyButton;

    [Header("Owned Skills Display")]
    [SerializeField] private Transform ownedSkillsContent;
    [SerializeField] private GameObject ownedSkillSlotPrefab;

    [Header("Owned Items Display")]
    [SerializeField] private Transform ownedItemsContent;
    [SerializeField] private GameObject ownedItemSlotPrefab;

    private int currentStageId;
    private int currentStars;
    private int currentGold;
    private int currentExp;
    private float currentClearTime;

    private void Awake()
    {
        if (lobbbyButton != null)
        {
            lobbbyButton.onClick.AddListener(OnConfirmButtonClick);
        }
    }

    private void OnEnable()
    {
        RefreshOwnedSkills();
        RefreshOwnedItems();
        if (gameManager != null)
            gameManager.PauseGame();

        // 드래그 비활성화
        if (dragManager != null)
            dragManager.SetDragEnabled(false);
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.ResumeGame();

        // 드래그 재활성화
        if (dragManager != null)
            dragManager.SetDragEnabled(true);
    }

    public void Show(string stageName, int expReward, int goldReward, int stars = 3)
    {
        if (stageManager != null)
        {
            currentStageId = stageManager.CurrentStageId;
            currentClearTime = gameManager?.ElapsedTime ?? 0f;
        }

        currentStars = stars;
        currentGold = goldReward;
        currentExp = expReward;

        panelRoot.SetActive(true);
    }

    public void TogglePanel()
    {
        if (panelRoot == null) return;

        if (panelRoot.activeSelf)
            Hide();
        else
            panelRoot.SetActive(true);
    }

    private void RefreshOwnedSkills()
    {
        if (passiveSkillManager == null || ownedSkillsContent == null) return;

        var ownedSkills = passiveSkillManager.GetOwnedPassiveSkills();
        if (ownedSkills == null) return;

        int slotIndex = 0;

        foreach (var skill in ownedSkills)
        {
            int skillId = skill.GetCurrentSkillId();
            if (skillId < 0) continue;

            SkillData skillData = DataTableManager.SkillTable?.Get(skillId);
            if (skillData == null) continue;

            // 슬롯 가져오기 또는 생성
            GameObject slot;
            if (slotIndex < ownedSkillsContent.childCount)
            {
                slot = ownedSkillsContent.GetChild(slotIndex).gameObject;
                slot.SetActive(true);
            }
            else if (ownedSkillSlotPrefab != null)
            {
                slot = Instantiate(ownedSkillSlotPrefab, ownedSkillsContent);
            }
            else
            {
                continue;
            }

            // 스킬 아이콘 설정
            Transform iconTransform = slot.transform.Find("SkillIcon");
            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null && !string.IsNullOrEmpty(skillData.SKILL_ICON))
                {
                    if (AddressablePreloader.Instance != null)
                    {
                        Sprite sprite = AddressablePreloader.Instance.GetCachedSprite(skillData.SKILL_ICON);
                        if (sprite != null)
                            iconImage.sprite = sprite;
                    }
                }
            }

            // 별 표시 설정
            Transform starsTransform = slot.transform.Find("Stars");
            if (starsTransform != null)
            {
                for (int i = 0; i < starsTransform.childCount; i++)
                {
                    starsTransform.GetChild(i).gameObject.SetActive(i < skill.currentStar);
                }
            }

            slotIndex++;
        }

        // 남은 슬롯 비활성화
        for (int i = slotIndex; i < ownedSkillsContent.childCount; i++)
        {
            ownedSkillsContent.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void RefreshOwnedItems()
    {
        Debug.Log($"[PauseController] RefreshOwnedItems 시작");

        if (stageManager == null || ownedItemsContent == null)
        {
            Debug.LogWarning($"[PauseController] stageManager={stageManager}, ownedItemsContent={ownedItemsContent}");
            return;
        }

        Dictionary<int, int> items = stageManager.GetAccumulatedItems();
        Debug.Log($"[PauseController] 획득 아이템 수: {items?.Count ?? 0}");

        if (items == null) return;

        int slotIndex = 0;

        foreach (var kvp in items)
        {
            int itemId = kvp.Key;
            int count = kvp.Value;

            ItemData itemData = DataTableManager.ItemTable?.Get(itemId);
            Debug.Log($"[PauseController] itemId={itemId}, itemData={itemData?.ITEM_NAME ?? "NULL"}, ITEM_ICON={itemData?.ITEM_ICON ?? "NULL"}");

            if (itemData == null) continue;

            // 슬롯 가져오기 또는 생성
            GameObject slot;
            if (slotIndex < ownedItemsContent.childCount)
            {
                slot = ownedItemsContent.GetChild(slotIndex).gameObject;
                slot.SetActive(true);
            }
            else if (ownedItemSlotPrefab != null)
            {
                slot = Instantiate(ownedItemSlotPrefab, ownedItemsContent);
            }
            else
            {
                Debug.LogWarning($"[PauseController] ownedItemSlotPrefab이 null입니다");
                continue;
            }

            // 아이템 아이콘 설정
            Transform iconTransform = slot.transform.Find("ItemIcon");
            Debug.Log($"[PauseController] iconTransform={iconTransform}");

            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null && !string.IsNullOrEmpty(itemData.ITEM_ICON))
                {
                    if (AddressablePreloader.Instance != null)
                    {
                        Sprite sprite = AddressablePreloader.Instance.GetCachedSprite(itemData.ITEM_ICON);
                        Debug.Log($"[PauseController] ITEM_ICON={itemData.ITEM_ICON}, sprite={sprite}");

                        if (sprite != null)
                            iconImage.sprite = sprite;
                        else
                            Debug.LogWarning($"[PauseController] 스프라이트를 찾을 수 없음: {itemData.ITEM_ICON}");
                    }
                }
            }

            // 수량 텍스트 설정
            TMP_Text countText = slot.GetComponentInChildren<TMP_Text>();
            if (countText != null)
            {
                countText.text = $"x{count}";
            }

            slotIndex++;
        }

        // 남은 슬롯 비활성화
        for (int i = slotIndex; i < ownedItemsContent.childCount; i++)
        {
            ownedItemsContent.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (lobbbyButton != null)
        {
            lobbbyButton.onClick.RemoveListener(OnConfirmButtonClick);
        }
    }

    private async void OnConfirmButtonClick()
    {
        Hide();
        Time.timeScale = 1f;

        // 로비로 이동 (로딩씬 사용)
        LoadingRequest request = new("Lobby");
        await LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }
}
