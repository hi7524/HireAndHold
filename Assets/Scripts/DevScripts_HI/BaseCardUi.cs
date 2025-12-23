using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseCardUi : MonoBehaviour
{
    [SerializeField] protected Image image;
    [SerializeField] protected TextMeshProUGUI text;
    [SerializeField] protected CanvasGroup canvasGroup;

    public CanvasGroup CanvasGroup => canvasGroup;
    public TextMeshProUGUI Text => text;

    protected virtual void Awake()
    {
        if (canvasGroup == null)
        {
            if (!TryGetComponent<CanvasGroup>(out canvasGroup))
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    public virtual void SetImage(Sprite sprite)
    {
        if (image != null)
            image.sprite = sprite;
    }

    public virtual void SetImageColor(Color color)
    {
        if (image != null)
            image.color = color;
    }

    public virtual void SetTitleText(string msg)
    {
        if (text != null)
            text.text = msg;
    }
}