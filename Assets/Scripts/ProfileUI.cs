using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI stageProgressText;

    private async void OnEnable()
    {
        await UniTask.WaitUntil(() => PlayData.IsInitialized);
        Refresh();
        PlayData.OnProfileChanged += Refresh;
    }


    private void OnDisable()
    {
        PlayData.OnProfileChanged -= Refresh;
    }

    public async void Refresh()
    {
        int level = PlayData.Level;
        int exp = PlayData.Exp;
        int maxExp = level * 100;

        nicknameText.text = PlayData.Nickname;
        levelText.text = $"{level}";
        expText.text = $"{exp} / {maxExp}";

        expSlider.maxValue = maxExp;
        expSlider.value = exp;
        stageProgressText.text =
        $"스테이지 {PlayData.LastClearedStageNumber}";
    }
}
