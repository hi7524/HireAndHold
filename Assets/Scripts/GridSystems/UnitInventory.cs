using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class UnitInventory : MonoBehaviour, ITestDroppable
{
    [SerializeField] private UnitInventorySlot slotPrf;
    [SerializeField] private ScrollRect scrollRect;
    [Space]
    [SerializeField] private DragManager dragManager;
    [SerializeField] private GridDatasForTesting gridDatas;

    private const int MaxCapacity = 5;

    private List<int> ownedUnitIds = new List<int>();
    private UnitInventorySlot[] slots;
    private int slotIndex;


    async UniTaskVoid Start()
    {
        slots = new UnitInventorySlot[MaxCapacity];

        Create(MaxCapacity);

        // DataTable 먼저 초기화
        await DataTableManager.InitAsync();

        // 테스트용 아이디 추가
        AddUnit(11101);
        AddUnit(11104);
        AddUnit(11107);

        UpdateAllSlotsUi();
    }

    private void OnEnable()
    {
        dragManager.SetDragEnabled(true);
    }

    private void OnDisable()
    {
        dragManager.SetDragEnabled(false);
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
                slots[i].SetGridData(gridDatas.GridDatas[unitId]);
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
        Debug.Log("드롭");
    }

    public void OnDragEnter(ITestDraggable draggable)
    {
        if (ownedUnitIds.Count < MaxCapacity)
        {
            Debug.Log("드롭 가능");
        }
        else
        {
            Debug.Log("드롭 불가능");
        }
    }

    public void OnDragExit(ITestDraggable draggable)
    {
        
    }
}
