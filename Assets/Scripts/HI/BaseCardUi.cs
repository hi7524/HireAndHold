using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseCardUi : MonoBehaviour
{
    [SerializeField] protected TextMeshProUGUI titleText;
    [SerializeField] protected TextMeshProUGUI descriptionText;
    [SerializeField] protected Image image;


    public virtual void SetTitleText(string msg)
    {
        if (titleText != null)
            titleText.text = msg;
    }

    public virtual void SetDescriptionText(string msg)
    {   
        if (descriptionText != null)
            descriptionText.text = msg;
    }

    public virtual void SetImage(Sprite sprite)
    {
        if (image != null)
            image.sprite = sprite;
    }
}