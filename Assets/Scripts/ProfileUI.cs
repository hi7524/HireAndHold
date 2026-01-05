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

    [SerializeField] private TextMeshProUGUI dungeonProgressText;

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

    public void Refresh()
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

        RefreshDungeonProgress();
    }

    private void RefreshDungeonProgress()
    {
        if (dungeonProgressText == null)
            return;

        if (DatabaseManager.Instance == null ||
            DatabaseManager.Instance.CurrentUser == null)
        {
            dungeonProgressText.text = "광석 던전 -";
            return;
        }

        int highestDungeonStage =
            DatabaseManager.Instance.GetHighestDungeonStage();

        if (highestDungeonStage <= 0)
        {
            dungeonProgressText.text = "광석 던전 미도전";
        }
        else
        {
            dungeonProgressText.text =
                $"광석 던전 {highestDungeonStage}";
        }
    }
}
