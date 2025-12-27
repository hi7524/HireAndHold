using UnityEngine;
using DG.Tweening;

public class PanelOpenEffect : MonoBehaviour
{
    [SerializeField] private bool playOnEnable;

    private Tween scaleTween;

    public void OnEnable()
    {
        if (playOnEnable)
        {
            PlayEffect();
        }
    }

    public void PlayEffect()
    {
        if (scaleTween != null)
        {
            scaleTween.Kill();
        }

        transform.localScale = Vector3.one * 0.8f;
        scaleTween = transform.DOScale(1.0f, 0.15f);
    }
}
