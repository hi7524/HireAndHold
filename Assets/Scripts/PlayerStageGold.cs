using System;
using UnityEngine;

public class PlayerStageGold : MonoBehaviour
{
    [SerializeField] private StageUiManager uiManager;

    public int Gold { get; private set; }

    public event Action OnChangeGold;


    public void Start()
    {
        uiManager.UpdateStageGoldText(Gold);
    }

    public void AddGold(int amount)
    {
        if (amount < 0)
            return;
        
        Gold += amount;
        uiManager.UpdateStageGoldText(Gold);
        OnChangeGold?.Invoke();
    }

    public bool UseGold(int amount)
    {
        if (Gold < amount || amount < 0)
            return false;

        Gold -= amount;
        uiManager.UpdateStageGoldText(Gold);
        OnChangeGold?.Invoke();
        return true;
    }
}
