using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image image;


    public void SetTitleText(string msg)
    {
        if (titleText != null)
            titleText.text = msg;
    }

    public void SetDescriptionText(string msg)
    {
        if (descriptionText != null)
            descriptionText.text = msg;
    }

    public void SetImage(Sprite sprite)
    {
        if (image != null)
            image.sprite = sprite;
    }
}