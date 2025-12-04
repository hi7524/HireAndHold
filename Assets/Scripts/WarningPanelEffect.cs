using UnityEngine;

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
    private bool isPlaying = false;

    // Scale animation state
    private int currentScaleLoop = 0;
    private float scaleTimer = 0f;
    private Vector3 scaleStart;
    private Vector3 scaleTarget;
    private bool isScalingUp = true;
    private bool isScaleFinishing = false;
    private bool scaleAnimationComplete = false;

    // Blink animation state
    private int currentBlinkCount = 0;
    private float blinkTimer = 0f;
    private float alphaStart;
    private float alphaTarget;
    private bool isFadingOut = true;
    private bool blinkAnimationComplete = false;

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
        StartWarningEffect();
    }

    private void StartWarningEffect()
    {
        isPlaying = true;

        // Reset scale animation state
        currentScaleLoop = 0;
        scaleTimer = 0f;
        isScalingUp = true;
        isScaleFinishing = false;
        scaleAnimationComplete = false;
        scaleStart = originalScale;
        scaleTarget = originalScale * scaleUpSize;

        // Reset blink animation state
        currentBlinkCount = 0;
        blinkTimer = 0f;
        isFadingOut = true;
        blinkAnimationComplete = false;
        alphaStart = 1f;
        alphaTarget = 0.3f;

        // Reset visual state
        targetTransform.localScale = originalScale;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        bool scaleComplete = UpdateScaleAnimation();
        bool blinkComplete = UpdateBlinkAnimation();

        if (scaleComplete && blinkComplete)
        {
            isPlaying = false;
        }
    }

    private bool UpdateScaleAnimation()
    {
        if (scaleAnimationComplete) return true;

        scaleTimer += Time.unscaledDeltaTime;

        float duration = isScaleFinishing ? scaleDuration * 0.5f : scaleDuration;
        float t = scaleTimer / duration;
        float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);

        targetTransform.localScale = Vector3.Lerp(scaleStart, scaleTarget, easeT);

        if (scaleTimer >= duration)
        {
            targetTransform.localScale = scaleTarget;
            scaleTimer = 0f;

            if (isScaleFinishing)
            {
                scaleAnimationComplete = true;
                return true;
            }

            if (isScalingUp)
            {
                // Switch to scale down
                scaleStart = scaleTarget;
                scaleTarget = originalScale * scaleDownSize;
                isScalingUp = false;
            }
            else
            {
                // Complete one loop
                currentScaleLoop++;

                if (currentScaleLoop >= scaleLoopCount)
                {
                    // Start finishing animation
                    scaleStart = scaleTarget;
                    scaleTarget = originalScale;
                    isScaleFinishing = true;
                }
                else
                {
                    // Switch to scale up
                    scaleStart = scaleTarget;
                    scaleTarget = originalScale * scaleUpSize;
                    isScalingUp = true;
                }
            }
        }

        return false;
    }

    private bool UpdateBlinkAnimation()
    {
        if (canvasGroup == null || blinkAnimationComplete) return true;

        blinkTimer += Time.unscaledDeltaTime;
        float t = blinkTimer / blinkDuration;

        canvasGroup.alpha = Mathf.Lerp(alphaStart, alphaTarget, t);

        if (blinkTimer >= blinkDuration)
        {
            canvasGroup.alpha = alphaTarget;
            blinkTimer = 0f;

            if (isFadingOut)
            {
                // Switch to fade in
                alphaStart = 0.3f;
                alphaTarget = 1f;
                isFadingOut = false;
            }
            else
            {
                // Complete one blink
                currentBlinkCount++;

                if (currentBlinkCount >= blinkCount)
                {
                    canvasGroup.alpha = 1f;
                    blinkAnimationComplete = true;
                    return true;
                }
                else
                {
                    // Switch to fade out
                    alphaStart = 1f;
                    alphaTarget = 0.3f;
                    isFadingOut = true;
                }
            }
        }

        return false;
    }

    private void OnDisable()
    {
        isPlaying = false;

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
