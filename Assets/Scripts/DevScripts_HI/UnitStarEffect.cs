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

        // 이전 애니메이션 정리 (DOKill로 즉시 중단)
        foreach (var star in starImages)
        {
            if (star != null)
            {
                // 애니메이션을 즉시 중단
                star.transform.DOKill(true);
                star.DOKill(true);
            }
        }

        // 별 3개 모두 애니메이션 (StarLevel만큼 노란색, 나머지는 회색)
        int activeStarCount = Mathf.Clamp(starLevel, 1, 3);

        // 모든 별을 한 번에 초기화 (애니메이션 시작 전)
        for (int i = 0; i < starImages.Length && i < 3; i++)
        {
            if (starImages[i] != null)
            {
                Color targetColor = (i < activeStarCount) ? activeStarColor : inactiveStarColor;
                starImages[i].gameObject.SetActive(true);
                starImages[i].transform.localScale = Vector3.one;
                starImages[i].color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
            }
        }

        // 각 별 애니메이션 시작
        for (int i = 0; i < starImages.Length && i < 3; i++)
        {
            if (starImages[i] != null)
            {
                bool isActive = i < activeStarCount; // StarLevel만큼만 노란색
                PlaySingleStarAnimation(starImages[i], i * starDelay, isActive, i);
            }
        }
    }

    private void PlaySingleStarAnimation(Image star, float delay, bool isActive, int starIndex)
    {
        Color targetColor = isActive ? activeStarColor : inactiveStarColor;

        Sequence sequence = DOTween.Sequence();

        // 지연
        sequence.AppendInterval(delay);

        // 크게 튀어나옴 (Scale + 색상 변경) + 활성화된 별이면 사운드 재생
        if (isActive)
        {
            sequence.AppendCallback(() => PlayStarSound(starIndex + 1)); // starIndex는 0-based이므로 +1
        }
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

    // 별 등급에 따른 사운드 재생
    private void PlayStarSound(int starLevel)
    {
        Debug.Log($"[UnitStarEffect] PlayStarSound called with starLevel: {starLevel}");

        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[UnitStarEffect] SoundManager.Instance is null");
            return;
        }

        if (AddressablePreloader.Instance == null)
        {
            Debug.LogWarning("[UnitStarEffect] AddressablePreloader.Instance is null");
            return;
        }

        string soundName = starLevel switch
        {
            1 => "Star1",
            2 => "Star2",
            3 => "Star3",
            _ => null
        };

        Debug.Log($"[UnitStarEffect] Looking for sound: {soundName}");

        if (!string.IsNullOrEmpty(soundName))
        {
            var starSound = AddressablePreloader.Instance.GetCachedAudioClip(soundName);
            if (starSound != null)
            {
                Debug.Log($"[UnitStarEffect] Playing {soundName}");
                SoundManager.Instance.PlaySFX(starSound);
            }
            else
            {
                Debug.LogWarning($"[UnitStarEffect] {soundName} AudioClip not found in cache");
            }
        }
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
