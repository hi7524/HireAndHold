using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

public class DailyRewardSlot : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private GameObject rewardClaimedObj;
    [SerializeField] private GameObject rewardClaimedIconObj;
    [SerializeField] private GameObject focusObj;

    // 아이템 아이콘 설정
    public async void SetItemIcon(string iconAddress)
    {
        if (string.IsNullOrEmpty(iconAddress))
            return;

        try
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(iconAddress).ToUniTask();
            if (itemIcon != null && sprite != null)
            {
                itemIcon.sprite = sprite;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DailyRewardSlot] 아이콘 로드 실패: {iconAddress}, {ex.Message}");
        }
    }

    // 해당 날짜에 맞는 보상을 받았을 때
    public void MarkAsClaimed()
    {
        rewardClaimedObj.SetActive(true);
        rewardClaimedIconObj.SetActive(true);
    }

    // 보상은 못받았지만 해당 날짜가 넘어갔을 때
    public void MarkAsMissed()
    {
        rewardClaimedObj.SetActive(true);
        rewardClaimedIconObj.SetActive(false);
    }

    // 오늘 날짜 표시
    public void MarkAsToday()
    {
        focusObj.SetActive(true);
    }
}