using TMPro;
using UnityEngine;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI text;
    [Header("일반 데미지 효과")]
    public float floatSpeed = 40f;
    public float lifetime = 1f;
    public float stayDuration = 0.15f;
    public Color normalDamageColor = Color.white; // 일반 데미지 색상
    [Header("치명타 데미지 효과")]
    public Color criticalDamageColor = Color.yellow; // 치명타 데미지 색상
    public float criticalPunchScale = 1.3f; // 치명타시 커지는 배율
    public float criticalPunchDuration = 0.3f; // 펀치 애니메이션 시간

    private Color currentColor;
    private float originalFontSize;
    private RectTransform rect;
    private float timer = 0f;
    private Sequence criticalSequence;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        originalFontSize = text.fontSize;
    }

    public void SetText(string value)
    {
        SetText(value, false);
    }

    public void SetText(string value, bool isCritical)
    {
        text.text = value;

        // 기존 트윈 정리
        criticalSequence?.Kill();
        rect.localScale = Vector3.one;

        if (isCritical)
        {
            currentColor = criticalDamageColor;
            text.color = currentColor;
            text.fontSize = 28;

            // 치명타 펀치 효과: 원래크기 → 커짐 → 원래크기
            criticalSequence = DOTween.Sequence();
            criticalSequence.Append(rect.DOScale(criticalPunchScale, criticalPunchDuration / 2f).SetEase(Ease.OutQuad))
                           .Append(rect.DOScale(1f, criticalPunchDuration / 2f).SetEase(Ease.InQuad));
        }
        else
        {
            currentColor = normalDamageColor;
            text.color = currentColor;
            text.fontSize = originalFontSize;
        }

        timer = 0f;
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer > stayDuration)
        {
            rect.anchoredPosition += Vector2.up * floatSpeed * Time.deltaTime;
        }


        float fadeStartTime = lifetime * 0.5f;
        if (timer > fadeStartTime)
        {
            float fadeT = (timer - fadeStartTime) / (lifetime - fadeStartTime);
            Color c = currentColor;
            c.a = Mathf.Lerp(1f, 0, fadeT);
            text.color = c;
        }

        if (timer >= lifetime)
        {
            criticalSequence?.Kill();
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // 오브젝트가 비활성화될 때 트윈 정리
        criticalSequence?.Kill();
        rect.localScale = Vector3.one;
    }
}
