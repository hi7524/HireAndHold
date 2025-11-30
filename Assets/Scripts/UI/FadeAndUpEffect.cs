using DG.Tweening;
using TMPro;
using UnityEngine;

public class FadeAndUpEffect : MonoBehaviour
{
    [SerializeField] private float startOffset = 50f;
    private Sequence sequence;
    private CanvasGroup canvasGroup;


    private void Awake()
    {
        canvasGroup = gameObject.GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        PlayAnimation();
    }

    public void PlayAnimation()
    {
        sequence?.Kill();
        Vector3 targetPos = transform.localPosition;

        Vector3 startPos = targetPos;
        startPos.y -= startOffset;
        transform.localPosition = startPos;

        canvasGroup.alpha = 0;

        sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        sequence.Append(transform.DOLocalMoveY(targetPos.y, 0.3f));
        sequence.Join(canvasGroup.DOFade(1f, 0.3f));
        sequence.AppendInterval(1.0f);
        sequence.Append(canvasGroup.DOFade(0f, 0.8f));
        sequence.OnComplete(() => gameObject.SetActive(false));
    }
}