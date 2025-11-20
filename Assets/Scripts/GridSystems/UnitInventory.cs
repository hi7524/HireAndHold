using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UnitInventory : MonoBehaviour, ITestDroppable
{
    [SerializeField] private UnitInventorySlot slotPrf;
    [SerializeField] private ScrollRect scrollRect;
    [Space]
    [SerializeField] private DragManager dragManager;
    [SerializeField] private PlayerStageGold playerGold;
    [SerializeField] private GridDatasForTesting gridDatas;
    [SerializeField] private StageUiManager uiManager;

    private const int MaxCapacity = 16;

    private List<int> ownedUnitIds = new List<int>();
    private UnitInventorySlot[] slots;
    private int slotIndex;
    private Sequence dropSequence;

    private int sellCost = 10; // 테스트용 **


    async UniTaskVoid Start()
    {
        slots = new UnitInventorySlot[MaxCapacity];

        Create(MaxCapacity);

        await DataTableManager.InitAsync();
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
        // GameObject 파괴 시 트윈 정리
        dropSequence?.Kill();
        transform.DOKill();
    }

    // 유닛 추가
    public void AddUnit(int unitId)
    {
        if (ownedUnitIds.Count >= MaxCapacity)
        {
            Debug.Log("인벤토리 가득 참");
            return;
        }

        ownedUnitIds.Add(unitId);

        slots[slotIndex].gameObject.SetActive(true);
        slots[slotIndex].SetUnit(unitId);
        slots[slotIndex].SetGridData(gridDatas.GridDatas[unitId]);
        slots[slotIndex].UpdateUi();
        slotIndex++;
    }

    // 유닛 제거
    public void RemoveUnit(int unitId)
    {
        if (ownedUnitIds.Count <= 0)
        {
            Debug.Log("보유 유닛 없음");
            return;
        }

        int removeIndex = ownedUnitIds.IndexOf(unitId);
        if (removeIndex == -1)
        {
            Debug.LogWarning($"유닛 ID {unitId}를 찾을 수 없습니다.");
            return;
        }

        ownedUnitIds.RemoveAt(removeIndex);
        slotIndex--;

        UpdateAllSlotsUi();
    }

    private void SellUnits()
    {
        if (ownedUnitIds.Count <= 0)
            return;

        Debug.Log("잔여 유닛 판매");

        int addGold = ownedUnitIds.Count * sellCost;
        playerGold.AddGold(addGold);

        string msg = $"+{addGold}G\n유닛 {ownedUnitIds.Count}개 판매!";
        uiManager.UpdateInfoText(msg);

        ownedUnitIds.Clear();
        slotIndex = 0;
    }

    // 유닛 나타낼 UI 생성
    public void Create(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            var slot = Instantiate(slotPrf, scrollRect.content);
            slot.SetInventory(this);
            slots[i] = slot;
        }
    }

    // 전체 슬롯 UI 갱신
    public void UpdateAllSlotsUi()
    {
        for (int i = 0; i < MaxCapacity; i++)
        {
            if (i < ownedUnitIds.Count)
            {
                int unitId = ownedUnitIds[i];
                slots[i].SetUnit(unitId);
                slots[i].UpdatePreviewImages(gridDatas.GridDatas[unitId]);
                slots[i].UpdateUi();
                slots[i].gameObject.SetActive(true);
            }
            else
            {
                slots[i].gameObject.SetActive(false);
            }
        }
    }

    public bool CanDrop(ITestDraggable draggable)
    {
        return ownedUnitIds.Count < MaxCapacity;
    }

    public void OnDrop(ITestDraggable draggable)
    {
        // GridUnit일 경우
        var gridUnit = draggable.GameObject.GetComponent<GridUnit>();
        if (gridUnit != null)
        {
            AddUnit(gridUnit.UnitId);
            draggable.GameObject.SetActive(false);

            dropSequence?.Kill();
            dropSequence = DOTween.Sequence();
            dropSequence.Append(transform.DOScale(1.1f, 0.1f));
            dropSequence.Append(transform.DOScale(1.0f, 0.15f));
            return;
        }

        // UnitInventorySlot일 경우
        var inventorySlot = draggable.GameObject.GetComponent<UnitInventorySlot>();
        if (inventorySlot != null)
        {
            inventorySlot.OnDropFailed();
            return;
        }
    }

    public void OnDragEnter(ITestDraggable draggable)
    {
        if (ownedUnitIds.Count < MaxCapacity)
        {
            //Debug.Log("드롭 가능");
        }
        else
        {
            //Debug.Log("드롭 불가능");
        }
    }

    public void OnDragExit(ITestDraggable draggable)
    {
    }
}
