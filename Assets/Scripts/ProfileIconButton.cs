using UnityEngine;
using UnityEngine.UI;

public class ProfileIconButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    private void Start()
    {
        Refresh();
        PlayData.OnProfileChanged += Refresh;
    }

    private void OnDestroy()
    {
        PlayData.OnProfileChanged -= Refresh;
    }

    private void Refresh()
    {
        var address = PlayData.ProfileIconAddress;
        if (string.IsNullOrEmpty(address)) return;

        var sprite = AddressablePreloader.Instance.GetCachedSprite(address);
        if (sprite != null)
            iconImage.sprite = sprite;
    }
}
