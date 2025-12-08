using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class TestUnitCreator : MonoBehaviour
{
    [SerializeField] private ScrollRect unitScrollRect; 
    [SerializeField] private ScrollRect monsterScrollRect; 
    [SerializeField] private TestSlot slotPrf;

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
}