using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameData;
using Cysharp.Threading.Tasks;

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

    /// <summary>
    /// 카드 설정 (동기 버전 - 캐시된 스프라이트 사용)
    /// </summary>
    public void Setup(GachaItem item)
    {
        var unitData = DataTableManager.UnitTable.Get(item.unitId);
        if (unitData == null) return;

        // 유닛 이름 설정
        if (unitNameText != null)
        {
            unitNameText.text = unitData.StringName;
        }

        // 아이콘 주소 결정
        string iconAddress = null;
        if (item.isDuplicate)
        {
            iconAddress = GetFragmentIconAddress(unitData);
        }
        else
        {
            iconAddress = unitData.UNIT_ICON;
        }

        // 캐시된 스프라이트 즉시 가져오기 (이미 프리로드됨)
        if (!string.IsNullOrEmpty(iconAddress))
        {
            var sprite = SpriteCache.Instance.GetCachedSprite(iconAddress);
            if (sprite != null && unitIcon != null)
            {
                unitIcon.sprite = sprite;
                unitIcon.enabled = true;
            }
            else
            {
                // 캐시에 없으면 비동기 로드 (백업)
                LoadIconAsync(iconAddress).Forget();
            }
        }

        // 레어리티 배경 설정
        ApplyRarityBackground(item.rarity);

        // 뱃지 설정
        if (newBadge != null)
        {
            newBadge.SetActive(item.isNew);
        }

        // 중복 처리
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

    /// <summary>
    /// 아이콘 비동기 로드 (캐시 미스 시 백업용)
    /// </summary>
    private async UniTaskVoid LoadIconAsync(string iconAddress)
    {
        var sprite = await SpriteCache.Instance.LoadSpriteAsync(iconAddress);
        if (sprite != null && unitIcon != null)
        {
            unitIcon.sprite = sprite;
            unitIcon.enabled = true;
        }
    }

    private void ApplyRarityBackground(GachaRarity rarity)
    {
        if (rarityBackground == null) return;

        rarityBackground.color = rarity switch
        {
            GachaRarity.Common => new Color32(78, 70, 56, 255),      // Dark Warm Brown
            GachaRarity.Rare => new Color32(64, 100, 78, 255),       // Dark Emerald Green
            GachaRarity.Unique => new Color32(60, 82, 122, 255),     // Dark Sapphire Blue
            GachaRarity.Epic => new Color32(142, 114, 58, 255),      // Dark Gold / Bronze
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
