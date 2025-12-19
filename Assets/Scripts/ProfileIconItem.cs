using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ProfileIconItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    private string iconAddress;
    private UnityAction<string> onSelected;

    public void Init(Sprite sprite, string address, UnityAction<string> callback)
    {
        iconImage.sprite = sprite;
        iconAddress = address;
        onSelected = callback;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected?.Invoke(iconAddress));
    }
}
