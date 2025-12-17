using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDProfileUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private Slider levelExpSlider;

    private void Awake()
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
        if (!PlayData.IsInitialized)
            return;

        int level = PlayData.Level;
        int exp = PlayData.Exp;
        int maxExp = level * 100;

        nicknameText.text = PlayData.Nickname;
        levelText.text = $"{level}";

        if (expText != null)
            expText.text = $"{exp} / {maxExp}";

        levelExpSlider.minValue = 0;
        levelExpSlider.maxValue = maxExp;
        levelExpSlider.value = exp;
    }
}
