using System;
using UnityEngine;

public class PlayerStageGold : MonoBehaviour
{
    [SerializeField] private StageUiManager uiManager;

    public int Credit { get; private set; }

    public event Action OnChangeGold;


    public void Start()
    {
        uiManager.UpdateCreditText(Credit);
    }

    public void AddCredit(int amount)
    {
        if (amount < 0)
            return;
        
        Credit += amount;
        uiManager.UpdateCreditText(Credit);
        OnChangeGold?.Invoke();
    }

    public bool UseCredit(int amount)
    {
        if (Credit < amount || amount < 0)
            return false;

        Credit -= amount;
        uiManager.UpdateCreditText(Credit);
        OnChangeGold?.Invoke();
        return true;
    }
}
