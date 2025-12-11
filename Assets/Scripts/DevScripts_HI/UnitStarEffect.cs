using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UnitStarEffect : MonoBehaviour
{
    [Header("Star References")]
    [SerializeField] private Image[] starImages; // 별 이미지 3개

    [Header("Animation Settings")]
    [SerializeField] private float starDelay = 0.2f; // 별 간 시작 간격
    [SerializeField] private float popDuration = 0.2f; // 별이 커지는 시간
    [SerializeField] private float shrinkDuration = 0.2f; // 별이 작아지는 시간
    [SerializeField] private float displayDuration = 1.2f; // 별이 유지되는 시간
    [SerializeField] private float fadeOutDuration = 0.5f; // 페이드 아웃 시간
    [SerializeField] private float popScale = 2.3f; // 최대 크기 배율

    [Header("Star Colors")]
    [SerializeField] private Color activeStarColor = Color.white; // 활성화된 별 색상
    [SerializeField] private Color inactiveStarColor = new Color(0.3f, 0.3f, 0.3f); // 비활성화된 별 색상 (어두운 회색)

    private void Awake()
    {
        if (starImages != null)
        {
            foreach (var star in starImages)
            {
                if (star != null)
                {
                    star.gameObject.SetActive(true);
                    star.transform.localScale = Vector3.one;
                    // 완전 투명
                    star.color = new Color(inactiveStarColor.r, inactiveStarColor.g, inactiveStarColor.b, 0f);
                }
            }
        }
    }

    public void PlayStarEffect(int starLevel)
    {
        if (starImages == null || starImages.Length == 0)
        {
            Debug.LogWarning("유닛 별 이미지 할당해주세요.");
            return;
        }

        // 이전 애니메이션 정리
        StopAllCoroutines();
        foreach (var star in starImages)
        {
            if (star != null)
            {
                star.transform.DOKill();
            }
        }

        // 모든 별을 일단 완전 투명 상태로 리셋
        foreach (var star in starImages)
        {
            if (star != null)
            {
                star.gameObject.SetActive(true);
                star.transform.localScale = Vector3.one;
                // 완전 투명
                star.color = new Color(inactiveStarColor.r, inactiveStarColor.g, inactiveStarColor.b, 0f);
            }
        }

        // 별 3개 모두 애니메이션 (StarLevel만큼 노란색, 나머지는 회색)
        int activeStarCount = Mathf.Clamp(starLevel, 1, 3);

        for (int i = 0; i < starImages.Length && i < 3; i++)
        {
            if (starImages[i] != null)
            {
                bool isActive = i < activeStarCount; // StarLevel만큼만 노란색
                PlaySingleStarAnimation(starImages[i], i * starDelay, isActive);
            }
        }
    }

    private void PlaySingleStarAnimation(Image star, float delay, bool isActive)
    {
        Color targetColor = isActive ? activeStarColor : inactiveStarColor;

        Sequence sequence = DOTween.Sequence();

        // 지연
        sequence.AppendInterval(delay);

        // 크게 튀어나옴 (Scale + 색상 변경)
        sequence.Append(star.transform.DOScale(popScale, popDuration).SetEase(Ease.OutBack));
        sequence.Join(star.DOColor(targetColor, popDuration));

        // 정상 크기로 축소
        sequence.Append(star.transform.DOScale(1f, shrinkDuration).SetEase(Ease.InCubic));

        // 유지 (대기)
        sequence.AppendInterval(displayDuration);

        //  페이드 아웃
        Color transparentColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
        sequence.Append(star.DOColor(transparentColor, fadeOutDuration));
    }

    private void OnDestroy()
    {
        // 애니메이션 정리
        if (starImages != null)
        {
            foreach (var star in starImages)
            {
                if (star != null)
                {
                    star.transform.DOKill();
                }
            }
        }
    }
}
