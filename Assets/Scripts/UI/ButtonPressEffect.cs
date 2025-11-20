using DG.Tweening;
using UnityEngine;

public class ButtonPressEffect : MonoBehaviour
{
    private Sequence sequence;
    
    public void PressedEffect()
    {
        sequence?.Kill();
        sequence = DOTween.Sequence();
        sequence.Append(transform.DOScale(0.9f, 0.08f));
        sequence.Append(transform.DOScale(1.0f, 0.12f));
    }
}
