using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseCardUi : MonoBehaviour
{
    [SerializeField] protected Image image;
    [SerializeField] protected TextMeshProUGUI text;

    public virtual void SetImage(Sprite sprite)
    {
        if (image != null)
            image.sprite = sprite;
    }
    
    public virtual void SetTitleText(string msg)
    {
        if (text != null)
            text.text = msg;
    }

    // public virtual void SetDescriptionText(string msg)
    // {   
    //     if (descriptionText != null)
    //         descriptionText.text = msg;
    // }

}