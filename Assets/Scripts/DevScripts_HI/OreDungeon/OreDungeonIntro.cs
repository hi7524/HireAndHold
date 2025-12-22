using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using System.Linq;

public class OreDungeonIntro : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private OreDungeonManager gameManager;
    [SerializeField] private OreDungeonAssetManager assetManager;

    [Header("UnitCard")]
    [SerializeField] private BaseCardUi cardPrf;
    [SerializeField] private Transform prfTrans;

    [Header("Pop Animation Settings")]
    [SerializeField] private float startScale = 2.0f;
    [SerializeField] private float popScale = 2.3f;             // 최대 크기 배율
    [SerializeField] private float slowGrowDuration = 0.15f;    // 느리게 커지는 시간
    [SerializeField] private float shrinkDuration = 0.2f;       // 정상 크기로 줄어드는 시간
    [SerializeField] private float delayBetweenPopCards = 0.1f; // Pop 카드 간 딜레이

    [Header("Slide Animation Settings")]
    [SerializeField] private float beforeStartSlideAnimDelay = 0.5f; // 슬라이드 애니메이션 시작 전 딜레이 시간
    [SerializeField] private float slideDistance = 100f;             // 슬라이드 거리
    [SerializeField] private float slideAnimDuration = 0.6f;         // 슬라이드 애니메이션 시간
    [SerializeField] private float delayBetweenSlideCards = 0.1f;    // 슬라이드 카드 간 딜레이

    [Header("Roulette Animation Settings")]
    [SerializeField] private float rouletteDuration = 3f;            // 룰렛 총 지속 시간
    [SerializeField] private float rouletteMinInterval = 0.03f;      // 최소 간격 (가장 빠를 때)
    [SerializeField] private float rouletteMaxInterval = 0.3f;       // 최대 간격 (가장 느릴 때)

    private List<BaseCardUi> cardList = new List<BaseCardUi>();
    private HorizontalLayoutGroup layoutGroup;
    private List<int> allUnitIds = new List<int>(); // 룰렛에 사용할 전체 유닛 ID 리스트

    private void Start()
    {
        layoutGroup = prfTrans.GetComponent<HorizontalLayoutGroup>();
        gameManager.OnInitialized += Initialize;
    }

    private void OnDestroy()
    {
        gameManager.OnInitialized -= Initialize;
    }

    private void Initialize()
    {
        int count = gameManager.UnitCount;
        int randomCount = GetRandomUnitCount();

        cardList.Clear();
        List<int> unitIds = gameManager.draftUnitList.ToList();

        // 룰렛에 사용할 전체 유닛 ID 리스트 가져오기
        if (DataTableManager.UnitTable != null)
        {
            allUnitIds = new List<int>(DataTableManager.UnitTable.RawTable.Keys);
        }

        for (int i = 0; i < count; i++)
        {
            BaseCardUi card = Instantiate(cardPrf, prfTrans);
            cardList.Add(card);

            // 유닛 ID로 미리 로드된 스프라이트 가져오기
            int unitId = unitIds[i];
            if (assetManager.UnitSprites.TryGetValue(unitId, out Sprite sprite))
            {
                card.SetImage(sprite);
            }

            // CanvasGroup 미리 설정 및 투명하게 만들기
            if (!card.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                canvasGroup = card.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f; // 모든 카드를 투명하게
        }

        // 프레임 대기 후 애니메이션 시작 (레이아웃 정리)
        DOTween.Sequence()
            .AppendInterval(0.05f)
            .AppendCallback(() => StartAnimations(count, randomCount));
    }

    private void StartAnimations(int count, int randomCount)
    {
        // LayoutGroup 비활성화
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        int popCardCount = count - randomCount;

        // Pop 애니메이션 시작 (기존 유닛들)
        for (int i = 0; i < popCardCount; i++)
        {
            PlayScalePunchAnimation(cardList[i], i);
        }

        // Pop 애니메이션이 모두 끝나는 시간 계산
        float popAnimDuration = slowGrowDuration + shrinkDuration;
        float allPopAnimEndTime = (popCardCount - 1) * delayBetweenPopCards + popAnimDuration;

        // Slide 애니메이션 시작 (랜덤으로 추가된 유닛들)
        for (int i = 0; i < randomCount; i++)
        {
            int cardIndex = popCardCount + i;
            PlaySlideUpAnimation(cardList[cardIndex], i, allPopAnimEndTime);
        }

        // 모든 애니메이션 끝나는 시간 계산 (슬라이드 + 룰렛)
        float slideAnimDuration = (randomCount > 0) ? beforeStartSlideAnimDelay + (randomCount - 1) * delayBetweenSlideCards + this.slideAnimDuration : 0f;
        float totalAnimTime = allPopAnimEndTime + slideAnimDuration + rouletteDuration;

        // 모든 애니메이션 끝난 후 LayoutGroup 다시 활성화
        DOTween.Sequence()
            .AppendInterval(totalAnimTime)
            .AppendCallback(() =>
            {
                if (layoutGroup != null)
                {
                    layoutGroup.enabled = true;
                }
            });
    }

    // 랜덤으로 뽑은 유닛 수 구하기
    // 다른 활성화 애니메이션 적용을 위해
    private int GetRandomUnitCount()
    {
        int existingUnits = gameManager.ExistingUnitCount;
        int totalUnits = gameManager.UnitCount;

        return Mathf.Max(0, totalUnits - existingUnits);
    }

    // 크기 애니메이션 (편성 유닛 애니메이션)
    private void PlayScalePunchAnimation(BaseCardUi card, int index)
    {
        card.TryGetComponent<CanvasGroup>(out var canvasGroup);

        card.transform.localScale = Vector3.one * startScale;
        canvasGroup.alpha = 0f;

        Sequence sequence = DOTween.Sequence();

        sequence.AppendInterval(index * delayBetweenPopCards);

        // 크기 확상
        sequence.Append(card.transform.DOScale(popScale, slowGrowDuration).SetEase(Ease.Linear));

        // 정상 크기로 축소하며 페이드 인
        sequence.Append(card.transform.DOScale(1f, shrinkDuration).SetEase(Ease.InCubic));
        sequence.Join(canvasGroup.DOFade(1f, shrinkDuration));
    }

    // 슬라이드 애니메이션 (랜덤 유닛 애니메이션)
    private void PlaySlideUpAnimation(BaseCardUi card, int index, float startDelay)
    {
        RectTransform rectTransform = card.GetComponent<RectTransform>();
        card.TryGetComponent<CanvasGroup>(out var canvasGroup);

        Vector3 targetPos = rectTransform.anchoredPosition;

        rectTransform.anchoredPosition = targetPos + Vector3.down * slideDistance;
        canvasGroup.alpha = 0f;

        // 슬라이드 시작 시 이미지를 검은색으로 설정
        card.SetImageColor(Color.black);

        float delay = startDelay + beforeStartSlideAnimDelay + index * delayBetweenSlideCards;

        // 슬라이드 애니메이션 시작
        rectTransform.DOAnchorPos(targetPos, slideAnimDuration).SetEase(Ease.OutCubic).SetDelay(delay);
        canvasGroup.DOFade(1f, slideAnimDuration).SetEase(Ease.InQuad).SetDelay(delay);

        int capturedSlideIndex = index;

        // 슬라이드 애니메이션 동안 빠르게 이미지 변경
        int slidePeriodSpinCount = Mathf.FloorToInt(slideAnimDuration / rouletteMinInterval);
        float currentDelay = delay;

        for (int i = 0; i < slidePeriodSpinCount; i++)
        {
            DOVirtual.DelayedCall(currentDelay, () => SetRandomUnitImage(card));
            currentDelay += rouletteMinInterval;
        }

        // 슬라이드 끝난 후 점점 느려지면서 진행
        float rouletteStartDelay = delay + slideAnimDuration;
        currentDelay = rouletteStartDelay;
        float elapsedTime = 0f;

        while (elapsedTime < rouletteDuration)
        {
            // easeOutQuint 커브로 현재 진행도 계산
            float t = elapsedTime / rouletteDuration;
            float easeOutQuint = 1f - Mathf.Pow(1f - t, 5f);
            float currentInterval = Mathf.Lerp(rouletteMinInterval, rouletteMaxInterval, easeOutQuint);

            // 마지막 룰렛인지 확인
            bool isLastSpin = elapsedTime + currentInterval >= rouletteDuration;

            if (isLastSpin)
            {
                DOVirtual.DelayedCall(currentDelay, () =>
                {
                    SetFinalUnitImage(card, capturedSlideIndex);

                    // 이미지를 하얀색으로 복원
                    card.SetImageColor(Color.white);

                    // 마지막 룰렛에서 살짝 커졌다 작아지는 효과
                    card.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
                });
            }
            else
            {
                DOVirtual.DelayedCall(currentDelay, () => SetRandomUnitImage(card));
            }

            currentDelay += currentInterval;
            elapsedTime += currentInterval;

            if (isLastSpin) break;
        }
    }

    // 슬라이드 카드의 최종 유닛 이미지 설정
    private void SetFinalUnitImage(BaseCardUi card, int slideCardIndex)
    {
        int existingCount = gameManager.ExistingUnitCount;
        int cardIndex = existingCount + slideCardIndex;

        List<int> unitIds = gameManager.draftUnitList.ToList();
        if (cardIndex < unitIds.Count)
        {
            int unitId = unitIds[cardIndex];
            if (assetManager.UnitSprites.TryGetValue(unitId, out Sprite sprite))
            {
                card.SetImage(sprite);
            }
        }
    }

    // 랜덤 유닛 이미지 설정
    private void SetRandomUnitImage(BaseCardUi card)
    {
        if (allUnitIds == null || allUnitIds.Count == 0)
            return;

        // 랜덤 유닛 ID 선택
        int randomIndex = Random.Range(0, allUnitIds.Count);
        int randomUnitId = allUnitIds[randomIndex];

        // 미리 로드된 스프라이트 가져오기
        if (assetManager.UnitSprites.TryGetValue(randomUnitId, out Sprite sprite))
        {
            card.SetImage(sprite);
        }
    }
}