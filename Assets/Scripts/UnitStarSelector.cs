using UnityEngine;
using UnityEngine.UI;
using System;

public class UnitStarSelector : MonoBehaviour
{
    [SerializeField] private Button[] starButtons;
    [SerializeField] private Image[] starHighlights;

    private int currentStar = 1;
    private int maxStar = 3;

    public int CurrentStar => currentStar;

    public event Action<int> OnStarChanged;

    public void Initialize(int availableMaxStar)
    {
        maxStar = availableMaxStar;

        if (starButtons == null)
            return;

        for (int i = 0; i < starButtons.Length; i++)
        {
            if (starButtons[i] == null)
                continue;

            int star = i + 1;

            starButtons[i].interactable = (star <= maxStar);

            starButtons[i].onClick.RemoveAllListeners();
            starButtons[i].onClick.AddListener(() => SelectStar(star));
        }

        SelectStar(1);
    }

    private void SelectStar(int star)
    {
        currentStar = star;

        if (starHighlights != null)
        {
            for (int i = 0; i < starHighlights.Length; i++)
            {
                if (starHighlights[i] != null)
                    starHighlights[i].enabled = (i + 1 == star);
            }
        }

        OnStarChanged?.Invoke(star);
    }
}
