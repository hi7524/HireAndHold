using UnityEngine;

public class LevelUpCardUI : MonoBehaviour
{
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private GameObject[] cards;

    private void Start()
    {
        playerExperience.OnLevelUp += ActiveCardUIs;
    }

    private void ActiveCardUIs()
    {
        // 임시, 후에 수정 필요
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].SetActive(true);
        }

        Time.timeScale = 0;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
         
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].SetActive(false);
        }
    }

    private void OnDestroy()
    {
        playerExperience.OnLevelUp -= ActiveCardUIs;
    }
}