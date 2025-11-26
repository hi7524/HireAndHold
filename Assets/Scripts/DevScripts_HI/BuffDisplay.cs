using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuffDisplay : MonoBehaviour
{
    [SerializeField] private BuffManager buffManager;
    [SerializeField] private TextMeshProUGUI buffText;
    [SerializeField] private TextMeshProUGUI detailBuffText;

    private void OnEnable()
    {
        buffManager.OnBuffPercentageChanged += UpdateDisplay;
        buffManager.OnActivatedBuffsChanged += UpdateDetailText;
    }

    private void OnDisable()
    {
        buffManager.OnBuffPercentageChanged -= UpdateDisplay;
        buffManager.OnActivatedBuffsChanged -= UpdateDetailText;
    }

    private void UpdateDisplay(float buffPercentage)
    {
        buffText.text = $"공격력 +{buffPercentage}%";
    }

    private void UpdateDetailText(HashSet<string> activatedBuffs)
    {
        if (detailBuffText == null)
            return;

        if (activatedBuffs.Count == 0)
        {
            detailBuffText.gameObject.SetActive(false);
            return;
        }

        detailBuffText.gameObject.SetActive(true);
        string details = "활성화된 버프:\n";
        foreach (string buffName in activatedBuffs)
        {
            details += $"{buffName}\n";
        }

        detailBuffText.text = details.TrimEnd('\n');
    }
}