using UnityEngine;
using UnityEngine.UI;


public class InGameUi : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Wall wall;

    private void Awake()
    {
        if(wall != null)
        {
            wall.OnHpChanged += UpdateValue;
            wall.OnExpChanged += UpdateExp;
        }

        hpSlider.minValue = 0;
        if (wall != null)
        {
            hpSlider.maxValue = wall.MaxHp();
            expSlider.maxValue = wall.MaxExp();
        }
    }

    private void UpdateValue(float currenthp, float maxHp)
    {
        hpSlider.maxValue = maxHp;
        hpSlider.value = currenthp;
    }

    private void UpdateExp(float currentExp, float maxExp)
    {
        expSlider.maxValue = maxExp;
        expSlider.value = currentExp;
    }
}
