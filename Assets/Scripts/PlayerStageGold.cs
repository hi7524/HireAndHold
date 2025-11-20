using UnityEngine;

public class PlayerStageGold : MonoBehaviour
{
    [SerializeField] private StageUiManager uiManager;

    public int Gold { get; private set; }


    public void AddGold(int amount)
    {
        if (amount < 0)
            return;
        
        Gold += amount;
        uiManager.UpdateStageGoldText(Gold);
    }

    public bool UseGold(int amount)
    {
        if (Gold < amount || amount < 0)
            return false;

        Gold -= amount;
        uiManager.UpdateStageGoldText(Gold);
        return true;
    }
}