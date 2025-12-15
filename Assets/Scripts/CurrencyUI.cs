using TMPro;
using UnityEngine;

public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI diamondText;
    [SerializeField] private TextMeshProUGUI staminaText;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        goldText.text = PlayData.Gold.ToString("N0");
        diamondText.text = PlayData.Diamond.ToString("N0");
        staminaText.text = $"{PlayData.Stamina}";
    }
}
