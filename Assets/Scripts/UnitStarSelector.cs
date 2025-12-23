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

            int starIndex = i + 1; 

            starButtons[i].interactable = (starIndex <= maxStar);

            starButtons[i].onClick.RemoveAllListeners();
            starButtons[i].onClick.AddListener(() => SelectStar(starIndex));
        }

        SelectStar(1);
    }

    private void SelectStar(int star)
    {
        if (star < 1 || star > maxStar)
            return;

        currentStar = star;

        if (starHighlights != null)
        {
            for (int i = 0; i < starHighlights.Length; i++)
            {
                if (starHighlights[i] != null)
                    starHighlights[i].enabled = (i + 1 == star);
            }
        }

        Debug.Log($"[UnitStarSelector] Star selected: {star}");
        OnStarChanged?.Invoke(star);
    }
}
