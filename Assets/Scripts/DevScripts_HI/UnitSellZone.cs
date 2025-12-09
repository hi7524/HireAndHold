using UnityEngine;

public class UnitSellZone : MonoBehaviour, IDroppable
{
    [SerializeField] private StageUiManager uiManager;
    [SerializeField] private GridManager gridManager;

    private const string RewardUnitBlockedMsg = "레벨업 보상 유닛은 즉시 배치해야 합니다!";
    private const string MinUnitRequiredMsg = "최소 1개의 유닛은 배치되어야 합니다!";

    private Vector3 orginalSize;


    public bool CanDrop(IDraggable draggable)
    {
        // DraggableGridUnitUi는 인벤토리에 올릴 수 없음 (레벨업 보상)
        if (draggable.GameObject.GetComponent<DraggableGridUnitUi>() != null)
            return false;

        // 유닛이 아닐 경우 드롭할 수 없음
        var unit = draggable.GameObject.GetComponent<GridUnit>();
        if (unit == null)
            return false;

        // 방금 획득한 유닛은 인벤토리에 올릴 수 없음
        if (!unit.canPlaceInInventory)
        {
            uiManager.UpdateInfoText(RewardUnitBlockedMsg, Color.red);
            return false;
        }

        // 마지막 남은 유닛은 판매할 수 없음
        if (gridManager != null && gridManager.IsLastUnitOnGrid())
        {
            uiManager.UpdateInfoText(MinUnitRequiredMsg, Color.red);
            return false;
        }

        return true;
    }

    public void OnDragEnter(IDraggable draggable)
    {
        throw new System.NotImplementedException();
    }

    public void OnDragExit(IDraggable draggable)
    {
        throw new System.NotImplementedException();
    }

    public void OnDrop(IDraggable draggable)
    {
        throw new System.NotImplementedException();
    }
}