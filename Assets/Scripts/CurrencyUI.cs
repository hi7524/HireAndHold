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
        goldText.text = NumberFormatUtil.ToShortNumber(PlayData.Gold);
        diamondText.text = NumberFormatUtil.ToShortNumber(PlayData.Diamond);
        staminaText.text = PlayData.Stamina.ToString();
    }

    public static class NumberFormatUtil
    {
        public static string ToShortNumber(long value)
        {
            if (value >= 1_000_000_000)
            {
                return (value / 1_000_000_000f).ToString("0.#") + "B";
            }

            if (value >= 1_000_000)
            {
                return (value / 1_000_000f).ToString("0.#") + "M";
            }

            if (value >= 1_000)
            {
                return (value / 1_000f).ToString("0.#") + "k";
            }

            return value.ToString("N0");
        }
    }

}
