using UnityEngine;

public class MailNoticeDot : MonoBehaviour
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
        PlayData.OnMailsChanged += UpdateNoticeDot;
    }

    private void OnDisable()
    {
        PlayData.OnMailsChanged -= UpdateNoticeDot;
    }

    public void UpdateNoticeDot()
    {
        if (noticeDotObj == null) return;

        int claimableCount = DatabaseManager.Instance?.GetTotalClaimableMailCount() ?? 0;
        noticeDotObj.SetActive(claimableCount > 0);
    }
}
