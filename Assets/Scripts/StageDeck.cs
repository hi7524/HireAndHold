using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class StageDeck : MonoBehaviour
{
    public Image[] slotImages;
    public Sprite emptySprite;

    private async void Start()
    {
        await DatabaseManager.Instance.WaitForInitializationAsync();

        await AutoFillAllPresetsIfNeeded();

        // 초기화
        Init();
    }

    private async UniTask AutoFillAllPresetsIfNeeded()
    {
        await AutoFillPresetIfEmpty(PlayData.currentSelectedPreset);
    }

    private void OnEnable()
    {

        if (DataTableManager.IsInitialized)
        {
            Init();
        }
    }

    private async UniTask AutoFillPresetIfEmpty(int preset)
    {

        // 프리셋이 비어있는지 확인
        bool isEmpty = true;
        for (int i = 0; i < 5; i++)
        {
            if (PlayData.selectedDeckUnitIds[preset, i] != 0)
            {
                isEmpty = false;
                break;
            }
        }

        if (!isEmpty)
        {
            Debug.Log($"[StageDeck] Preset {preset}는 이미 편성되어 있음");
            return;
        }

        // 보유 캐릭터 
        var owned = DatabaseManager.Instance.GetAllCharacters();

        if (owned.Count == 0)
        {
            Debug.LogWarning("[StageDeck] 소유한 캐릭터가 없음");
            return;
        }

        Debug.Log($"[StageDeck] Preset {preset} 자동 편성 시작 (소유 캐릭터: {owned.Count}개)");

        if (DataTableManager.UnitTable == null)
        {
            Debug.LogError("[StageDeck] UnitTable이 초기화되지 않음");
            return;
        }

        // 최대 5개까지 자동 편성
        for (int i = 0; i < 5 && i < owned.Count; i++)
        {
            int unitId = int.Parse(owned[i].id);
            var unitData = DataTableManager.UnitTable.Get(unitId);

            if (unitData == null)
            {
                Debug.LogWarning($"[StageDeck] unitId={unitId} 데이터를 찾을 수 없음");
                continue;
            }

            PlayData.selectedDeckUnitIds[preset, i] = unitId;
            PlayData.selectedDeckUnitIconAddresses[preset, i] = unitData.UNIT_ICON;

            Debug.Log($"[StageDeck] Slot {i}에 {unitData.NAME} (ID:{unitId}) 편성");
        }

        // DB에 저장
        await DatabaseManager.Instance.SavePresetFromPlayDataAsync(preset);

        Debug.Log($"[StageDeck] Preset {preset} 자동 편성 완료");
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
                Debug.LogWarning($"[StageDeck] unitId={unitId} 데이터 또는 아이콘 없음");
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
