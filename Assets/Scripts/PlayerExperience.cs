using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 플레이어 레벨 및 경험치 관리
public class PlayerExperience : MonoBehaviour
{
    [SerializeField] private Slider expBar;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private float expBarFillDuration = 0.5f;

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
        expBar.value = 0f;
    }

    public void AddExp(int amount)
    {
        curPlayerExp += amount;
        AnimateExpBar();
    }

    // ** 테스트 후 지울 것
    public void Cheat_LevelUp()
    {
        LevelUp();
    }

    private void AnimateExpBar()
    {
        expBar.DOKill();

        if (curPlayerExp >= expRequired)
        {
            expBar.DOValue(1f, expBarFillDuration).SetEase(Ease.OutCubic).OnComplete(() =>
            {
                LevelUp();
                AnimateExpBar();
            });
        }
        else
        {
            float targetValue = curPlayerExp / expRequired;
            expBar.DOValue(targetValue, expBarFillDuration).SetEase(Ease.OutCubic);
        }
    }

    private void LevelUp()
    {
        Level++;
        curPlayerExp -= expRequired;
        expRequired *= expGrowthRate;
        OnLevelUp?.Invoke();

        UpdateLevelTextUI();
        expBar.value = 0f; // 경험치바 리셋
    }

    private void UpdateLevelTextUI()
    {
        levelText.text = $"Lv.{Level}";
    }
}