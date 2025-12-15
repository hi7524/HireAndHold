using TMPro;
using UnityEngine;

public class ProfileUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI mainNicknameText;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        nicknameText.text = PlayData.Nickname;
        mainNicknameText.text = PlayData.Nickname;
    }
}
