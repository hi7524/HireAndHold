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

    private AsyncOperationHandle<Sprite>? iconHandle;

    public async void Setup(GachaItem item)
    {
        var unitData = DataTableManager.UnitTable.Get(item.unitId);
        if (unitData == null)
        {
            Debug.LogError($"[GachaResultCard] UnitData 없음: {item.unitId}");
            return;
        }

        //unitNameText.text = unitData.NAME;

        // Addressables 아이콘 로드
        if (!string.IsNullOrEmpty(unitData.UNIT_ICON))
        {
            iconHandle = Addressables.LoadAssetAsync<Sprite>(unitData.UNIT_ICON);
            await iconHandle.Value.Task;

            if (iconHandle.Value.Status == AsyncOperationStatus.Succeeded)
            {
                unitIcon.sprite = iconHandle.Value.Result;
                unitIcon.enabled = true;
            }
            else
            {
                Debug.LogError($"[GachaResultCard] 아이콘 로드 실패: {unitData.UNIT_ICON}");
            }
        }

        ApplyRarityBackground(item.rarity);
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
