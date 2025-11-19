using DG.Tweening;
using UnityEngine;

public class ScaleAnimator : MonoBehaviour
{
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float duration = 0.25f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void PlaySelect()
    {
        transform.DOScale(selectedScale,duration).SetEase(Ease.OutBack);
    }

    public void PlayDeselect()
    {
        transform.DOScale(originalScale, duration).SetEase(Ease.OutBack);
    }
}
