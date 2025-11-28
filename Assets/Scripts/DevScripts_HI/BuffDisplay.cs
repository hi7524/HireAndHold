using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuffDisplay : MonoBehaviour
{
    [SerializeField] private BuffManager buffManager;
    [SerializeField] private TextMeshProUGUI buffText;
    [SerializeField] private TextMeshProUGUI detailBuffText;
    [SerializeField] private GameObject textPanel;

    private void Start()
    {
        // 초기 버프 상태 표시
        if (buffManager != null)
        {
            UpdateDisplay(buffManager.GlobalBuffPercentage);
            UpdateDetailText(buffManager.ActivatedBuffs);
        }
    }

    private void OnEnable()
    {
        if (buffManager != null)
        {
            buffManager.OnBuffPercentageChanged += UpdateDisplay;
            buffManager.OnActivatedBuffsChanged += UpdateDetailText;
            Debug.Log("[BuffDisplay] 이벤트 구독 완료");

            // 현재 버프 상태를 즉시 반영
            UpdateDisplay(buffManager.GlobalBuffPercentage);
            UpdateDetailText(buffManager.ActivatedBuffs);
        }
    }

    private void OnDisable()
    {
        buffManager.OnBuffPercentageChanged -= UpdateDisplay;
        buffManager.OnActivatedBuffsChanged -= UpdateDetailText;
    }

    private void UpdateDisplay(float buffPercentage)
    {
        if (buffText == null)
            return;

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