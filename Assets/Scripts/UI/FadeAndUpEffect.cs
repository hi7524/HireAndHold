using DG.Tweening;
using TMPro;
using UnityEngine;

public class FadeAndUpEffect : MonoBehaviour
{
    [SerializeField] private float startOffset = 50f;
    private Sequence sequence;
    private CanvasGroup canvasGroup;
    private Vector3 originalLocalPos;
    private Vector3 startLocalPos;

    private void Awake()
    {
        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        originalLocalPos = transform.localPosition;
        startLocalPos = originalLocalPos;
        startLocalPos.y -= startOffset;
    }

    private void OnEnable()
    {
        PlayAnimation();
    }

    public void PlayAnimation()
    {
        sequence?.Kill();

        // 항상 고정된 시작 위치에서 시작
        transform.localPosition = startLocalPos;
        canvasGroup.alpha = 0;

        sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        // 원래 위치로 올라오면서 페이드 인
        sequence.Append(transform.DOLocalMoveY(originalLocalPos.y, 0.3f));
        sequence.Join(canvasGroup.DOFade(1f, 0.3f));
        sequence.AppendInterval(1.0f);
        sequence.Append(canvasGroup.DOFade(0f, 0.8f));
        sequence.OnComplete(() =>
        {
            // 복구: 원래 위치로 돌아감
            transform.localPosition = originalLocalPos;
            gameObject.SetActive(false);
        });
    }
}