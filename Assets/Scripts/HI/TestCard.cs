using UnityEngine;

public class TestCard : BaseCardUi
{
    private bool isUnitCard;

    private void OnEnable()
    {
        
    }

    public void SetIsUnitCard(bool value)
    {
        isUnitCard = value;

    }
}