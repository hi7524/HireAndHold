using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

public class PauseController : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private GameObject panelRoot;

    [Header("UI Elements")]

    [SerializeField] private Button lobbbyButton;

    private int currentStageId;
    private int currentStars;
    private int currentGold;
    private int currentExp;
    private float currentClearTime;

    private void Awake()
    {
        if (lobbbyButton != null)
        {
            lobbbyButton.onClick.AddListener(OnConfirmButtonClick);
        }
       
    }

    public void Show(string stageName, int expReward, int goldReward, int stars = 3)
    {
        if (stageManager != null)
        {
            currentStageId = stageManager.CurrentStageId;
            currentClearTime = gameManager?.ElapsedTime ?? 0f;
        }

        currentStars = stars;
        currentGold = goldReward;
        currentExp = expReward;

        panelRoot.SetActive(true);
        gameManager?.PauseGame();
    }


   

    public void Hide()
    {
        panelRoot?.SetActive(false);
    }

    private void OnDestroy()
    {
        if (lobbbyButton != null)
        {
            lobbbyButton.onClick.RemoveListener(OnConfirmButtonClick);
        }
       
    }

    private async void OnConfirmButtonClick()
    {
        Hide();
        Time.timeScale = 1f;

        // 로비로 이동 (로딩씬 사용)
        LoadingRequest request = new LoadingRequest("Lobby");
        await LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }

}
