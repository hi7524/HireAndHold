using TMPro;
using UnityEngine;

public class OreDungeonResultPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI amountText;

    public void SetResultPanel(bool isSuccess, int amount)
    {
        if (isSuccess)
            resultText.text = "던전 클리어!";
        else
            resultText.text = "던전 실패";

        amountText.text = $"강화석 {amount}개 획득";
    }
}