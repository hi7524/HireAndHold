using TMPro;
using UnityEngine;

public class OreDungeonResultPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI amountText;

    public void SetResultPanel(bool isSuccess, int amount)
    {
        if (isSuccess)
        {
            resultText.text = "던전 클리어!";

            // 성공 사운드 재생
            if (SoundManager.Instance != null && AddressablePreloader.Instance != null)
            {
                AudioClip successSound = AddressablePreloader.Instance.GetCachedAudioClip("DungeonSuccessedSound");
                if (successSound != null)
                {
                    SoundManager.Instance.PlaySFX(successSound);
                }
            }
        }
        else
        {
            resultText.text = "던전 실패";

            // 실패 사운드 재생
            if (SoundManager.Instance != null && AddressablePreloader.Instance != null)
            {
                AudioClip failedSound = AddressablePreloader.Instance.GetCachedAudioClip("DungeonFailedSound");
                if (failedSound != null)
                {
                    SoundManager.Instance.PlaySFX(failedSound);
                }
            }
        }

        amountText.text = $"강화석 {amount}개 획득";
    }
}
