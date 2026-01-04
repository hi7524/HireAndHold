using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MergePanel : MonoBehaviour
{
    [SerializeField] private Image unitImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject[] stars;
    [Header("Animation Settings")]
    [SerializeField] private float slideDistance = 500f;
    [SerializeField] private float slideInDuration = 0.5f;
    [SerializeField] private float pauseDuration = 1.5f;
    [SerializeField] private float slideOutDuration = 0.5f;
    [SerializeField] private float starScaleMultiplier = 1.3f;
    [SerializeField] private float starAnimDuration = 0.3f;
    [SerializeField] private float starDelayBetween = 0.1f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // 원래 위치 저장
        originalPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        PlaySlideAnimation();
    }

    // Unit의 ID에 맞게 이미지 및 대사 설정
    public void Init(int unitId)
    {
        // UnitData 가져오기
        var unitData = DataTableManager.UnitTable.Get(unitId);
        if (unitData == null)
        {
            Debug.LogError($"[MergePanel] Unit ID {unitId}를 찾을 수 없습니다.");
            return;
        }

        // 유닛 이미지 설정
        if (!string.IsNullOrEmpty(unitData.UNIT_ICON))
        {
            var sprite = AddressablePreloader.Instance.GetCachedSprite(unitData.UNIT_ICON);
            if (sprite != null)
                unitImage.sprite = sprite;
            else
                Debug.LogWarning($"[MergePanel] {unitData.UNIT_ICON} 스프라이트를 찾을 수 없습니다.");
        }

        // 유닛 이름 설정
        if (!string.IsNullOrEmpty(unitData.StringName))
        {
            nameText.text = unitData.StringName;
        }

        // 유닛 설명 설정
        if (!string.IsNullOrEmpty(unitData.StringVoiceText))
        {
            dialogueText.text = unitData.StringVoiceText;
        }

        // 보이스 재생 (2배 볼륨)
        if (!string.IsNullOrEmpty(unitData.VOICE) && unitData.VOICE != "0")
        {
            var voiceClip = AddressablePreloader.Instance.GetCachedVoice(unitData.VOICE);
            if (voiceClip != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayVoice(voiceClip, 5f);
                Debug.Log($"[MergePanel] 보이스 재생: {unitData.VOICE}");
            }
            else if (voiceClip == null)
            {
                Debug.LogWarning($"[MergePanel] {unitData.VOICE} 보이스가 캐시에 없습니다. Addressable의 UnitVoices 그룹을 확인하세요.");
            }
        }
    }

    private void PlaySlideAnimation()
    {
        // 초기 위치를 왼쪽 화면 밖으로 설정
        rectTransform.anchoredPosition = new Vector2(originalPosition.x - slideDistance, originalPosition.y);

        // 초기 알파값 0으로 설정
        canvasGroup.alpha = 0f;

        // 별들 초기 스케일 설정
        foreach (var star in stars)
        {
            if (star != null)
            {
                star.transform.localScale = Vector3.one;
            }
        }

        // 시퀀스 생성
        Sequence sequence = DOTween.Sequence();

        // 왼쪽에서 중앙으로 슬라이드 인 + 페이드 인
        sequence.Append(rectTransform.DOAnchorPosX(originalPosition.x, slideInDuration)
            .SetEase(Ease.OutCubic));
        sequence.Join(canvasGroup.DOFade(1f, slideInDuration)
            .SetEase(Ease.OutCubic));

        // 중간에 잠시 멈춤과 동시에 별 애니메이션
        sequence.AppendCallback(() => PlayStarAnimations());
        sequence.AppendInterval(pauseDuration);

        // 오른쪽으로 슬라이드 아웃 + 페이드 아웃
        sequence.Append(rectTransform.DOAnchorPosX(originalPosition.x + slideDistance, slideOutDuration)
            .SetEase(Ease.InCubic));
        sequence.Join(canvasGroup.DOFade(0f, slideOutDuration)
            .SetEase(Ease.InCubic));

        // 애니메이션 완료 후 비활성화
        sequence.OnComplete(() => gameObject.SetActive(false));
    }

    private void PlayStarAnimations()
    {
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null && stars[i].activeSelf)
            {
                float delay = i * starDelayBetween;

                // 커졌다가 원래 크기로 돌아오는 펀치 애니메이션
                stars[i].transform.DOPunchScale(Vector3.one * (starScaleMultiplier - 1f), starAnimDuration, 1, 0.5f)
                    .SetDelay(delay);
            }
        }
    }
}