using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class StageDeck : MonoBehaviour
{
    public Image[] slotImages;
    public Sprite emptySprite;

    private async void Start()
    {
        await DatabaseManager.Instance.WaitForInitializationAsync();
        await AutoFillAllPresetsIfNeeded();
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
        // 프리셋이 비어있는지 확인 (해제된 슬롯만)
        bool isEmpty = true;
        for (int i = 0; i < 5; i++)
        {
            // 해제된 슬롯만 체크
            if (!IsSlotUnlocked(preset, i))
                continue;

            if (PlayData.selectedDeckUnitIds[preset, i] != 0)
            {
                isEmpty = false;
                break;
            }
        }

        if (!isEmpty)
        {
            return;
        }

        // 보유 캐릭터 
        var owned = DatabaseManager.Instance.GetAllCharacters();
        if (owned.Count == 0)
        {
            Debug.LogWarning("[StageDeck] 소유한 캐릭터가 없음");
            return;
        }

        if (DataTableManager.UnitTable == null)
        {
            Debug.LogError("[StageDeck] UnitTable이 초기화되지 않음");
            return;
        }

        // 최대 5개까지 자동 편성 (해제된 슬롯만)
        int filledCount = 0;
        for (int i = 0; i < 5 && filledCount < owned.Count; i++)
        {
            // 슬롯이 잠겨있으면 스킵
            if (!IsSlotUnlocked(preset, i))
                continue;

            int unitId = int.Parse(owned[filledCount].id);
            var unitData = DataTableManager.UnitTable.Get(unitId);
            if (unitData == null)
            {
                Debug.LogWarning($"[StageDeck] unitId={unitId} 데이터를 찾을 수 없음");
                filledCount++;
                continue;
            }

            PlayData.selectedDeckUnitIds[preset, i] = unitId;
            PlayData.selectedDeckUnitIconAddresses[preset, i] = unitData.UNIT_ICON;
            filledCount++;
        }

        // DB에 저장
        await DatabaseManager.Instance.SavePresetFromPlayDataAsync(preset);
    }

    public void Init()
    {
        int preset = PlayData.currentSelectedPreset;

        // 편성된 유닛 수집
        List<int> assignedUnits = new List<int>();

        for (int i = 0; i < slotImages.Length; i++)
        {
            // 해제된 슬롯만 확인
            if (!IsSlotUnlocked(preset, i))
            {
                slotImages[i].sprite = emptySprite;
                continue;
            }

            int unitId = PlayData.selectedDeckUnitIds[preset, i];
            if (unitId == 0)
            {
                slotImages[i].sprite = emptySprite;
                continue;
            }

            assignedUnits.Add(unitId);

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

        // 인게임용 랜덤 유닛 채우기 (편성된 2마리 포함 총 10마리)
        FillRandomUnitsForBattle(assignedUnits);
    }

    /// <summary>
    /// 인게임용 랜덤 유닛 채우기
    /// </summary>
    private void FillRandomUnitsForBattle(List<int> assignedUnits)
    {
        // 전체 유닛 풀 생성 (보유 + 미보유)
        List<int> availableUnits = new List<int>();

        // 보유 유닛 추가
        var ownedCharacters = DatabaseManager.Instance.GetAllCharacters();
        foreach (var character in ownedCharacters)
        {
            int unitId = int.Parse(character.id);

            // 이미 편성된 유닛은 제외
            if (!assignedUnits.Contains(unitId))
            {
                availableUnits.Add(unitId);
            }
        }

        // 미보유 유닛 추가 (전체 유닛 ID 범위에서)
        // DataTable_Unit은 Get 메서드만 있고 All이 없으므로, 알려진 ID 범위를 순회
        if (DataTableManager.UnitTable != null)
        {
            // 유닛 ID 범위: 11101 ~ 11150 (예시, 실제 범위에 맞게 조정)
            for (int unitId = 11101; unitId <= 11150; unitId++)
            {
                var unitData = DataTableManager.UnitTable.Get(unitId);
                if (unitData == null)
                    continue;

                // 이미 편성되었거나 보유 중인 유닛은 제외
                if (!assignedUnits.Contains(unitId) && !IsUnitOwned(unitId))
                {
                    availableUnits.Add(unitId);
                }
            }
        }

        // 필요한 유닛 수 계산 (총 10마리 - 편성된 유닛 수)
        int needCount = 10 - assignedUnits.Count;
        needCount = Mathf.Min(needCount, availableUnits.Count);

        // 랜덤으로 선택
        List<int> randomUnits = new List<int>();
        for (int i = 0; i < needCount; i++)
        {
            if (availableUnits.Count == 0)
                break;

            int randomIndex = Random.Range(0, availableUnits.Count);
            int selectedUnit = availableUnits[randomIndex];

            randomUnits.Add(selectedUnit);
            availableUnits.RemoveAt(randomIndex);
        }

        // PlayData.selectedUnitIds에 전체 유닛 설정 (편성된 유닛 + 랜덤 유닛)
        PlayData.selectedUnitIds.Clear();

        // 편성된 유닛 먼저 추가
        foreach (int unitId in assignedUnits)
        {
            PlayData.selectedUnitIds.Add(unitId);
        }

        // 랜덤 유닛 추가
        foreach (int unitId in randomUnits)
        {
            PlayData.selectedUnitIds.Add(unitId);
        }

        Debug.Log($"[StageDeck] 인게임 유닛 구성: 편성 {assignedUnits.Count}마리 + 랜덤 {randomUnits.Count}마리 = 총 {PlayData.selectedUnitIds.Count}마리");
    }

    /// <summary>
    /// 유닛을 보유하고 있는지 확인
    /// </summary>
    private bool IsUnitOwned(int unitId)
    {
        string characterId = unitId.ToString();
        return PlayData.HasCharacter(characterId);
    }

    /// <summary>
    /// 슬롯이 해제되었는지 확인
    /// </summary>
    private bool IsSlotUnlocked(int presetIndex, int slotIndex)
    {
        // 0, 1번 슬롯은 항상 해제
        if (slotIndex < 2)
            return true;

        var userData = DatabaseManager.Instance.CurrentUser;
        if (userData?.presetSlotUnlocks == null)
            return false;

        return userData.presetSlotUnlocks.IsSlotUnlocked(presetIndex, slotIndex);
    }

    public void Refresh()
    {
        Init();
    }
}
