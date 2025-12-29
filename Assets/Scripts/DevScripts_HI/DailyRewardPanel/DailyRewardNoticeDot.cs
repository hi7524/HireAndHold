using UnityEngine;

public class DailyRewardNoticeDot : MonoBehaviour
{
    [SerializeField] private GameObject noticeDotObj;

    private async void Start()
    {
        // DatabaseManager 초기화 대기
        if (DatabaseManager.Instance != null)
        {
            await DatabaseManager.Instance.WaitForInitializationAsync();
            UpdateNoticeDot();
        }
        else
        {
            if (noticeDotObj != null)
                noticeDotObj.SetActive(false);
        }
    }

    public void UpdateNoticeDot()
    {
        if (noticeDotObj == null)
        {
            return;
        }

        if (DatabaseManager.Instance == null)
        {
            noticeDotObj.SetActive(false);
            return;
        }

        // 28일 이후에는 알림 비활성화
        if (System.DateTime.Now.Day > 28)
        {
            noticeDotObj.SetActive(false);
            return;
        }

        // 오늘 보상을 받았는지 체크
        bool hasClaimed = DatabaseManager.Instance.HasClaimedToday();
        noticeDotObj.SetActive(!hasClaimed);
    }
}
