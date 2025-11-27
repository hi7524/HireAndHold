using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuffDisplay : MonoBehaviour
{
    [SerializeField] private BuffManager buffManager;
    [SerializeField] private TextMeshProUGUI buffText;
    [SerializeField] private TextMeshProUGUI detailBuffText;
    [SerializeField] private GameObject textPanel;

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
        string details = string.Empty;
        foreach (string buffName in activatedBuffs)
        {
            details += $"{buffName}\n";
        }

        detailBuffText.text = details.TrimEnd('\n');

        detailBuffText.ForceMeshUpdate();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(detailBuffText.rectTransform);
    }

    public void OnPointerDownEvent()
    {
        textPanel.SetActive(true);
    }

    public void OnPointerUpEvent()
    {
        textPanel.SetActive(false);
    }
}