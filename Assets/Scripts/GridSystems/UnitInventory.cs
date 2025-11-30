using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class UnitInventory : MonoBehaviour, IDroppable
{
    [SerializeField] private UnitInventorySlot slotPrf;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private PlayerStageGold playerGold;
    [Header("Managers")]
    [SerializeField] private DragManager dragManager;
    [SerializeField] private StageUiManager uiManager;
    [SerializeField] private GridManager gridManager;

    private const string RewardUnitBlockedMsg = "레벨업 보상 유닛은 즉시 배치해야 합니다!";
    private const string MinUnitRequiredMsg = "최소 1개의 유닛은 배치되어야 합니다!";

    private const int MaxCapacity = 16;
    private const int SellCost = 25; // 테스트용 **

    private List<int> ownedUnitIds = new List<int>();
    private List<int> ownedUnitStars = new List<int>(); // 각 유닛의 성급 저장
    private UnitInventorySlot[] slots;
    private int slotIndex;
    private Sequence dropSequence;

    private Dictionary<int, UnitGridData> gridDataCache = new Dictionary<int, UnitGridData>();


    async UniTaskVoid Start()
    {
        InitializeSlots();
        await DataTableManager.InitAsync();
        await CacheAllGridData();
        UpdateAllSlotsUi();
    }

    private void OnEnable()
    {
        dragManager.SetDragEnabled(true);
    }

    private void OnDisable()
    {
        dragManager.SetDragEnabled(false);
        SellUnits();
        UpdateAllSlotsUi();
    }

    private void OnDestroy()
    {
        dropSequence?.Kill();
        transform.DOKill();

        foreach (var gridData in gridDataCache.Values)
        {
            if (gridData != null)
            {
                Addressables.Release(gridData);
            }
        }
        gridDataCache.Clear();
    }

    // 슬롯 배열 초기화 및 생성
    private void InitializeSlots()
    {
        slots = new UnitInventorySlot[MaxCapacity];
        CreateSlots(MaxCapacity);
    }

    // 지정된 수만큼 슬롯 생성
    private void CreateSlots(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            var slot = Instantiate(slotPrf, scrollRect.content);
            slot.SetInventory(this);
            slots[i] = slot;
        }
    }

    // 유닛 추가
    public void AddUnit(int unitId, int starLevel = 1)
    {
        if (!CanAddUnit())
        {
            Debug.Log("인벤토리 가득 참");
            return;
        }

        ownedUnitIds.Add(unitId);
        ownedUnitStars.Add(starLevel);
        SetupSlot(slotIndex, unitId, starLevel);
        slotIndex++;
    }

    // 유닛 추가 가능 여부 확인
    private bool CanAddUnit()
    {
        return ownedUnitIds.Count < MaxCapacity;
    }

    // 특정 슬롯 설정 및 활성화
    private void SetupSlot(int index, int unitId, int starLevel = 1)
    {
        slots[index].gameObject.SetActive(true);
        slots[index].SetUnit(unitId, starLevel);

        if (gridDataCache.TryGetValue(unitId, out var gridData))
        {
            slots[index].UpdatePreviewImages(gridData);
        }

        slots[index].UpdateUi();
    }

    // 유닛 제거
    public void RemoveUnit(int unitId)
    {
        if (!HasUnits())
        {
            Debug.Log("보유 유닛 없음");
            return;
        }

        int removeIndex = FindUnitIndex(unitId);
        if (removeIndex == -1)
        {
            Debug.LogWarning($"유닛 ID {unitId}를 찾을 수 없습니다.");
            return;
        }

        RemoveUnitAtIndex(removeIndex);
        UpdateAllSlotsUi();
    }

    // 유닛 보유 여부 확인
    private bool HasUnits()
    {
        return ownedUnitIds.Count > 0;
    }

    // 유닛 ID로 인덱스 찾기
    private int FindUnitIndex(int unitId)
    {
        return ownedUnitIds.IndexOf(unitId);
    }

    // 특정 인덱스의 유닛 제거
    private void RemoveUnitAtIndex(int index)
    {
        ownedUnitIds.RemoveAt(index);
        ownedUnitStars.RemoveAt(index);
        slotIndex--;
    }

    // 모든 잔여 유닛 판매
    private void SellUnits()
    {
        if (!HasUnits())
            return;

        int totalGold = CalculateSellValue();
        playerGold.AddGold(totalGold);

        ShowSellMessage(totalGold, ownedUnitIds.Count);
        ClearInventory();
    }

    // 판매 금액 계산
    private int CalculateSellValue()
    {
        return ownedUnitIds.Count * SellCost;
    }

    // 판매 완료 메시지 표시
    private void ShowSellMessage(int gold, int unitCount)
    {
        string msg = $"+{gold}G\n유닛 {unitCount}개 판매!";
        uiManager.UpdateInfoText(msg);
    }

    // 인벤토리 초기화
    private void ClearInventory()
    {
        ownedUnitIds.Clear();
        ownedUnitStars.Clear();
        slotIndex = 0;
    }

    // 전체 슬롯 UI 갱신
    public void UpdateAllSlotsUi()
    {
        for (int i = 0; i < MaxCapacity; i++)
        {
            if (i < ownedUnitIds.Count)
            {
                UpdateActiveSlot(i);
            }
            else
            {
                DeactivateSlot(i);
            }
        }
    }

    // 모든 유닛의 GridData를 미리 캐싱
    private async UniTask CacheAllGridData()
    {
        gridDataCache.Clear();

        // UnitTable의 모든 유닛에 대해 GridData 캐싱
        var unitTable = DataTableManager.UnitTable;
        if (unitTable == null) return;

        foreach (var unitData in unitTable.GetAll())
        {
            if (unitData != null && !string.IsNullOrEmpty(unitData.GRID_DATA))
            {
                int unitId = unitData.UNIT_ID;

                // 이미 캐시되어 있으면 스킵
                if (gridDataCache.ContainsKey(unitId))
                    continue;

                var gridDataHandle = Addressables.LoadAssetAsync<UnitGridData>(unitData.GRID_DATA);
                var gridData = await gridDataHandle.ToUniTask();

                if (gridDataHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    gridDataCache[unitId] = gridData;
                }
            }
        }
    }

    // 활성 슬롯 UI 업데이트
    private void UpdateActiveSlot(int index)
    {
        int unitId = ownedUnitIds[index];
        int starLevel = ownedUnitStars[index];
        slots[index].SetUnit(unitId, starLevel);

        // 캐시에서 GridData 가져오기
        if (gridDataCache.TryGetValue(unitId, out var gridData))
        {
            slots[index].UpdatePreviewImages(gridData);
        }

        slots[index].UpdateUi();
        slots[index].gameObject.SetActive(true);
    }

    // 슬롯 비활성화
    private void DeactivateSlot(int index)
    {
        slots[index].gameObject.SetActive(false);
    }

    // 드롭 가능 여부 확인
    public bool CanDrop(IDraggable draggable)
    {
        // draggableUnitUI는 드롭할 수 없음
        var draggableUnitUi = draggable.GameObject.GetComponent<DraggableGridUnitUi>();
        if (draggableUnitUi != null)
            return false;

        var unit = draggable.GameObject.GetComponent<GridUnit>();
        if (unit != null)
        {
            // 방금 획득한 유닛은 인벤토리에 올릴 수 없음
            if (!unit.canPlaceInInventory)
            {
                uiManager.UpdateInfoText(RewardUnitBlockedMsg, Color.red);
                return false;
            }

            // 마지막 남은 유닛은 인벤토리에 올릴 수 없음
            if (gridManager != null && gridManager.IsLastUnitOnGrid())
            {
                uiManager.UpdateInfoText(MinUnitRequiredMsg, Color.red);
                return false;
            }
        }

        return CanAddUnit();
    }

    // 드롭 처리 (GridUnit 또는 UnitInventorySlot)
    public void OnDrop(IDraggable draggable)
    {
        var gridUnit = draggable.GameObject.GetComponent<GridUnit>();
        if (gridUnit != null)
        {
            HandleGridUnitDrop(gridUnit);
            return;
        }

        var inventorySlot = draggable.GameObject.GetComponent<UnitInventorySlot>();
        if (inventorySlot != null)
        {
            HandleInventorySlotDrop(inventorySlot);
            return;
        }
    }

    // GridUnit 드롭 처리
    private void HandleGridUnitDrop(GridUnit gridUnit)
    {
        AddUnit(gridUnit.UnitId, gridUnit.StarLevel);
        gridUnit.gameObject.SetActive(false);
        PlayDropAnimation();

        // 그리드에서 인벤토리로 이동 시 카운트 감소
        if (gridManager != null)
            gridManager.DecrementUnitCount();
    }

    // UnitInventorySlot 드롭 처리
    private void HandleInventorySlotDrop(UnitInventorySlot inventorySlot)
    {
        inventorySlot.OnDropFailed();
    }

    // 드롭 애니메이션 재생
    private void PlayDropAnimation()
    {
        dropSequence?.Kill();
        dropSequence = DOTween.Sequence();
        dropSequence.Append(transform.DOScale(1.1f, 0.1f));
        dropSequence.Append(transform.DOScale(1.0f, 0.15f));
    }

    public void OnDragEnter(IDraggable draggable)
    {

    }

    public void OnDragExit(IDraggable draggable)
    {

    }
}
