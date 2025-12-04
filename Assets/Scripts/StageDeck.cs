using UnityEngine;
using UnityEngine.UI;

public class StageDeck : MonoBehaviour
{
    public Image[] slotImages;
    public Sprite emptySprite;

    private void Start()
    {
        // 로딩 씬에서 DataTableManager와 AddressablePreloader가 이미 초기화됨
        Init();
    }

    private void OnEnable()
    {
        // Start 이후에 활성화될 때만 Init 호출
        if (DataTableManager.IsInitialized)
        {
            Init();
        }
    }

    public void Init()
    {
        int preset = PlayData.currentSelectedPreset;
        Debug.Log("[StageDeck] Using Preset = " + preset);

        for (int i = 0; i < slotImages.Length; i++)
        {
            int unitId = PlayData.selectedDeckUnitIds[preset, i];
            Debug.Log("[StageDeck] slot " + i + " = " + unitId);

            if (unitId == 0)
            {
                slotImages[i].sprite = emptySprite;
                continue;
            }

            // DataTableManager에서 유닛 데이터 가져오기
            var unitData = DataTableManager.UnitTable?.Get(unitId);
            if (unitData == null || string.IsNullOrEmpty(unitData.UNIT_ICON))
            {
                slotImages[i].sprite = emptySprite;
                continue;
            }

            // AddressablePreloader에서 캐싱된 스프라이트 가져오기
            var sprite = AddressablePreloader.Instance.GetCachedSprite(unitData.UNIT_ICON);
            slotImages[i].sprite = sprite != null ? sprite : emptySprite;
        }
    }

    public void Refresh()
    {
        Init();
    }
}
