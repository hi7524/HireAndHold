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

    private List<BaseCardUi> cardList = new List<BaseCardUi>();
    private HorizontalLayoutGroup layoutGroup;

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

        for (int i = 0; i < count; i++)
        {
            BaseCardUi card = Instantiate(cardPrf, prfTrans);
            cardList.Add(card);

            // 유닛 ID로 UnitData 가져오기
            int unitId = unitIds[i];
            UnitData unitData = DataTableManager.UnitTable.Get(unitId);

            if (unitData != null && !string.IsNullOrEmpty(unitData.UNIT_ICON))
            {
                // Addressables로 유닛 아이콘 로드 및 설정
                Addressables.LoadAssetAsync<Sprite>(unitData.UNIT_ICON).Completed += (op) =>
                {
                    if (op.Result != null)
                    {
                        card.SetImage(op.Result);
                    }
                };
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

        // 모든 애니메이션 끝나는 시간 계산
        float slideAnimDuration = (randomCount > 0) ? beforeStartSlideAnimDelay + (randomCount - 1) * delayBetweenSlideCards + this.slideAnimDuration : 0f;
        float totalAnimTime = allPopAnimEndTime + slideAnimDuration;

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

        float delay = startDelay + beforeStartSlideAnimDelay + index * delayBetweenSlideCards;

        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(delay);

        // 위로 올라오며 페이드 인
        sequence.Append(rectTransform.DOAnchorPos(targetPos, slideAnimDuration).SetEase(Ease.OutCubic));
        sequence.Join(canvasGroup.DOFade(1f, slideAnimDuration).SetEase(Ease.InQuad));
    }
}