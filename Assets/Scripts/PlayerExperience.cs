using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 플레이어 레벨 및 경험치 관리
public class PlayerExperience : MonoBehaviour
{
    [SerializeField] private Slider expBar;
    [SerializeField] private TextMeshProUGUI levelText;

    public int Level { get; private set; }

    public event Action OnLevelUp;

    private float curPlayerExp;
    private float expRequired = 15;
    private float expGrowthRate = 1.2f; 


    private void Start()
    {
        Level = 1;
        curPlayerExp = 0f;

        UpdateLevelTextUI();
        UpdateExpBarUI();
    }

    public void AddExp(int amount)
    {
        curPlayerExp += amount;

        while (curPlayerExp >= expRequired)
        {
            LevelUp();
        }

        UpdateExpBarUI();
    }

    // ** 테스트 후 지울 것
    public void Cheat_LevelUp()
    {
        LevelUp();
    }

    private void LevelUp()
    {
        Level++;
        curPlayerExp -= expRequired;
        expRequired *= expGrowthRate;
        OnLevelUp?.Invoke();

        UpdateLevelTextUI();
    }

    private void UpdateLevelTextUI()
    {
        levelText.text = $"Lv.{Level}";
    }

    private void UpdateExpBarUI()
    {
        expBar.value = curPlayerExp / expRequired;
    }
}