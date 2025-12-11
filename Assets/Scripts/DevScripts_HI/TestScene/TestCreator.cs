using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class TestUnitCreator : MonoBehaviour
{
    public static TestUnitCreator Instance { get; private set; }

    [SerializeField] private ScrollRect unitScrollRect;
    [SerializeField] private ScrollRect monsterScrollRect;
    [SerializeField] private TestSlot slotPrf;

    /// <summary>
    /// TestSlot 프리팹 참조 (외부에서 사용 가능)
    /// </summary>
    public TestSlot SlotPrefab => slotPrf;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private async void Start()
    {
        await DataTableManager.InitAsync();
        var unitTable = DataTableManager.UnitTable.GetAll();
        var monsterTable = DataTableManager.MonsterTable.GetAll();

        // 유닛
        foreach (var unit in unitTable)
        {
            var slot = Instantiate(slotPrf, unitScrollRect.content);
            var unitGrid = await Addressables.LoadAssetAsync<UnitGridData>(unit.GRID_DATA).Task;

            slot.SetID(unit.UNIT_ID);
            slot.SetNameText(unit.StringName);
            slot.SetDraggableUnitData(unit.UNIT_ID, unit.LEVEL, unitGrid);

            var sprite = await Addressables.LoadAssetAsync<Sprite>(unit.UNIT_ICON).Task;
            slot.SetSprite(sprite);
        }

        // 몬스터
        foreach (var monster in monsterTable)
        {
            var slot = Instantiate(slotPrf, monsterScrollRect.content);

            slot.SetID(monster.MON_ID);
            slot.SetNameText(monster.MON_NAME);
        }
    }

    /// <summary>
    /// 특정 유닛 ID로 TestSlot을 생성하고 부모에 배치
    /// </summary>
    public async UniTask<TestSlot> CreateUnitSlotAsync(int unitId, Transform parent)
    {
        if (slotPrf == null)
        {
            Debug.LogError("[TestUnitCreator] slotPrf가 설정되지 않았습니다!");
            return null;
        }

        var unitData = DataTableManager.UnitTable.Get(unitId);
        if (unitData == null)
        {
            Debug.LogError($"[TestUnitCreator] 유닛 ID {unitId}를 찾을 수 없습니다!");
            return null;
        }

        var slot = Instantiate(slotPrf, parent);

        // ID 및 이름 설정
        slot.SetID(unitData.UNIT_ID);
        slot.SetNameText(unitData.StringName);

        // GridData 로드 및 설정
        try
        {
            var unitGrid = await Addressables.LoadAssetAsync<UnitGridData>(unitData.GRID_DATA).Task;
            slot.SetDraggableUnitData(unitData.UNIT_ID, unitData.LEVEL, unitGrid);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TestUnitCreator] GridData 로드 실패: {unitData.GRID_DATA}, {e.Message}");
        }

        // Sprite 로드 및 설정
        try
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(unitData.UNIT_ICON).Task;
            slot.SetSprite(sprite);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TestUnitCreator] Sprite 로드 실패: {unitData.UNIT_ICON}, {e.Message}");
        }

        return slot;
    }
}