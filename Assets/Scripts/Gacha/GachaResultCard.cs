using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameData;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GachaResultCard : MonoBehaviour
{
    [SerializeField] private Image unitIcon;
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private Image rarityBackground;

    [Header("Overlays")]
    [SerializeField] private GameObject newBadge;
    [SerializeField] private GameObject fragmentOverlay;

    [Header("Visual Control")]
    [SerializeField] private CanvasGroup cardCanvasGroup;

    private AsyncOperationHandle<Sprite>? iconHandle;

    public async void Setup(GachaItem item)
    {
        var unitData = DataTableManager.UnitTable.Get(item.unitId);
        if (unitData == null) return;

        string iconAddress;

        if (item.isDuplicate)
        {
            var fragmentItem = DataTableManager.ItemTable
                .Get(unitData.FRAGMENT_ITEM_ID);

            iconAddress = fragmentItem?.ITEM_ICON;
        }
        else
        {
            iconAddress = unitData.UNIT_ICON;
        }
        if (unitNameText != null)
        {
            unitNameText.text = $"{item.unitName}";
        }

        if (!string.IsNullOrEmpty(iconAddress))
        {
            iconHandle = Addressables.LoadAssetAsync<Sprite>(iconAddress);
            await iconHandle.Value.Task;

            if (iconHandle.Value.Status == AsyncOperationStatus.Succeeded)
            {
                unitIcon.sprite = iconHandle.Value.Result;
                unitIcon.enabled = true;
            }
        }

        ApplyRarityBackground(item.rarity);

        if(newBadge != null)
        {
            newBadge.SetActive(item.isNew);
        }

        if (item.isDuplicate)
        {
            // 카드 톤 다운 
            if (cardCanvasGroup != null)
                cardCanvasGroup.alpha = 0.6f;

            // 조각 오버레이 표시
            if (fragmentOverlay != null)
                fragmentOverlay.SetActive(true);    
        }
        else
        {
            if (cardCanvasGroup != null)
                cardCanvasGroup.alpha = 1f;

            if (fragmentOverlay != null)
                fragmentOverlay.SetActive(false);
        }
    }


    private void OnDestroy()
    {
        if (iconHandle.HasValue)
        {
            Addressables.Release(iconHandle.Value);
        }
    }

    private void ApplyRarityBackground(GachaRarity rarity)
    {
        rarityBackground.color = rarity switch
        {
            GachaRarity.Common => Color.white,
            GachaRarity.Rare => Color.green,
            GachaRarity.Unique => Color.blue,
            GachaRarity.Legendary => new Color(0.6f, 0.3f, 0.9f),
            GachaRarity.Epic => new Color(1f, 0.8f, 0.2f),
            _ => Color.white
        };
    }
}
