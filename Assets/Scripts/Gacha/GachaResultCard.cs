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

        string iconAddress = null;

        if (item.isDuplicate)
        {
            iconAddress = GetFragmentIconAddress(unitData);
        }
        else
        {
            // 기존 유닛 아이콘
            iconAddress = unitData.UNIT_ICON;
        }

        if (unitNameText != null)
        {
            unitNameText.text = unitData.StringName;
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
            else
            {
                Debug.LogWarning($"[GachaResultCard] Icon load failed: {iconAddress}");
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
            GachaRarity.Common => new Color32(78, 70, 56, 255),   // Dark Warm Brown
            GachaRarity.Rare => new Color32(64, 100, 78, 255),  // Dark Emerald Green
            GachaRarity.Unique => new Color32(60, 82, 122, 255),  // Dark Sapphire Blue
            GachaRarity.Epic => new Color32(142, 114, 58, 255), // Dark Gold / Bronze
            GachaRarity.Legendary => new Color32(120, 78, 150, 255), // Dark Violet
            _ => new Color32(70, 70, 70, 255)
        };
    }



    private string GetFragmentIconAddress(UnitData unitData)
    {
        if (unitData == null || string.IsNullOrEmpty(unitData.UNIT_ICON))
            return null;

        // UNIT_ICON 예: MILIA1, TARON2, VALEN3
        string icon = unitData.UNIT_ICON;

        // 뒤 숫자 제거
        icon = System.Text.RegularExpressions.Regex.Replace(icon, @"\d+$", "");

        // Addressables 주소 생성
        return $"unit/fragment/{icon}";
    }


}
