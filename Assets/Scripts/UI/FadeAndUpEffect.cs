using DG.Tweening;
using TMPro;
using UnityEngine;

public class FadeAndUpEffect : MonoBehaviour
{
    private Sequence sequence;
    private CanvasGroup canvasGroup;


    private void Awake()
    {
        canvasGroup = gameObject.GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        sequence?.Kill();

        Vector3 startPos = transform.localPosition;
        startPos.y = -80f;
        transform.localPosition = startPos;

        canvasGroup.alpha = 0;

        sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        sequence.Append(transform.DOLocalMoveY(-30, 0.3f));
        sequence.Join(canvasGroup.DOFade(1f, 0.2f));
        sequence.Append(transform.DOLocalMoveY(-40f, 0.1f));
        sequence.AppendInterval(1.0f);
        sequence.Append(canvasGroup.DOFade(0f, 0.8f));
        sequence.OnComplete(() => gameObject.SetActive(false));
    }
}