using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaResultCard : MonoBehaviour
{
    [SerializeField] private Image unitIcon;
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private Image rarityBackground;

    public void Setup(GachaItem item)
    {
        if (unitIcon != null && item.unitIcon != null)
        {
            unitIcon.sprite = item.unitIcon;
        }

        if (unitNameText != null)
        {
            unitNameText.text = item.unitId.ToString();
        }

        
    }

    
}
