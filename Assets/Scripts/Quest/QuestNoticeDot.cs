using UnityEngine;

public class QuestNoticeDot : MonoBehaviour
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
        QuestManager.OnQuestCompleted += OnQuestChanged;
        QuestManager.OnQuestRewardClaimed += OnQuestChanged;
        QuestManager.OnQuestsReset += OnQuestsReset;
    }

    private void OnDisable()
    {
        QuestManager.OnQuestCompleted -= OnQuestChanged;
        QuestManager.OnQuestRewardClaimed -= OnQuestChanged;
        QuestManager.OnQuestsReset -= OnQuestsReset;
    }

    private void OnQuestChanged(int questId) => UpdateNoticeDot();
    private void OnQuestsReset() => UpdateNoticeDot();

    public void UpdateNoticeDot()
    {
        if (noticeDotObj == null) return;

        int claimableCount = QuestManager.GetClaimableCount();
        noticeDotObj.SetActive(claimableCount > 0);
    }
}
