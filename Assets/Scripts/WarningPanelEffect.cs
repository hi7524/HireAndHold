using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class WarningPanelEffect : MonoBehaviour
{
    [SerializeField] private float scaleUpSize = 1.2f;
    [SerializeField] private float scaleDownSize = 0.8f;
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private int scaleLoopCount = 3;

    [SerializeField] private float blinkDuration = 0.2f;
    [SerializeField] private int blinkCount = 5;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform targetTransform;

    private Vector3 originalScale;

    private void Awake()
    {
        if (targetTransform == null)
        {
            targetTransform = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        originalScale = targetTransform.localScale;
    }

    private void OnEnable()
    {
        PlayWarningEffect().Forget();
    }

    private async UniTaskVoid PlayWarningEffect()
    {
        targetTransform.localScale = originalScale;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }


        await UniTask.WhenAll( PlayScaleAnimation(), PlayBlinkAnimation());
    }

    private async UniTask PlayScaleAnimation()
    {
        for (int i = 0; i < scaleLoopCount; i++)
        {

            await ScaleTo(originalScale * scaleUpSize, scaleDuration);

            await ScaleTo(originalScale * scaleDownSize, scaleDuration);
        }


        await ScaleTo(originalScale, scaleDuration * 0.5f);
    }

    private async UniTask ScaleTo(Vector3 targetScale, float duration)
    {
        Vector3 startScale = targetTransform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);

            targetTransform.localScale = Vector3.Lerp(startScale, targetScale, easeT);

            await UniTask.Yield();
        }

        targetTransform.localScale = targetScale;
    }

    private async UniTask PlayBlinkAnimation()
    {
        if (canvasGroup == null)
        {
            return;
        }

        for (int i = 0; i < blinkCount; i++)
        {
            await FadeTo(0.3f, blinkDuration);


            await FadeTo(1f, blinkDuration);
        }

        canvasGroup.alpha = 1f;
    }

    private async UniTask FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null)
        {
            return;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            await UniTask.Yield();
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void OnDisable()
    {

        if (targetTransform != null)
        {
            targetTransform.localScale = originalScale;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }
}
