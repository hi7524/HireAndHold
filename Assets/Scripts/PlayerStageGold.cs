using System;
using UnityEngine;
using Tutorial;

public class PlayerStageGold : MonoBehaviour
{
    [SerializeField] private StageUiManager uiManager;

    public int Credit { get; private set; }

    public event Action OnChangeGold;

    private void Awake()
    {
        // GameEvents에 등록
        GameEvents.PlayerGold = this;
    }

    public void Start()
    {
        uiManager.UpdateCreditText(Credit);
    }

    private void OnDestroy()
    {
        // GameEvents에서 해제
        if (GameEvents.PlayerGold == this)
            GameEvents.PlayerGold = null;
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
