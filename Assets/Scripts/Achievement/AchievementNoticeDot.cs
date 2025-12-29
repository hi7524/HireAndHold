using UnityEngine;

public class AchievementNoticeDot : MonoBehaviour
{
    [SerializeField] private GameObject noticeDotObj;

    private async void Start()
    {
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

    private void OnEnable()
    {
        AchievementManager.OnAchievementCompleted += OnAchievementChanged;
        AchievementManager.OnAchievementRewardClaimed += OnAchievementChanged;
    }

    private void OnDisable()
    {
        AchievementManager.OnAchievementCompleted -= OnAchievementChanged;
        AchievementManager.OnAchievementRewardClaimed -= OnAchievementChanged;
    }

    private void OnAchievementChanged(int achievementId) => UpdateNoticeDot();

    public void UpdateNoticeDot()
    {
        if (noticeDotObj == null) return;

        int claimableCount = AchievementManager.GetClaimableCount();
        noticeDotObj.SetActive(claimableCount > 0);
    }
}
