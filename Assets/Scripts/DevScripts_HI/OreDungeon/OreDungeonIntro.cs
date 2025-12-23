using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class OreDungeonIntro : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private OreDungeonManager gameManager;
    [SerializeField] private OreDungeonAssetManager assetManager;

    [Header("UnitCard")]
    [SerializeField] private BaseCardUi cardPrf;
    [SerializeField] private Transform prfTrans;

    [Header("AttackPowerText")]
    [SerializeField] private TextMeshProUGUI attackPowerText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private CanvasGroup attackPowerTextCG;

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

    [Header("Card Slide Down Settings")]
    [SerializeField] private float cardSlideUpDistance = 50f;        // 위로 올라가는 거리
    [SerializeField] private float cardSlideUpDuration = 0.3f;       // 위로 올라가는 시간
    [SerializeField] private float cardSlideDownDuration = 0.5f;     // 아래로 내려가는 시간
    [SerializeField] private float delayBetweenCardSlideDown = 0.1f; // 카드별 슬라이드 다운 딜레이

    [Header("Roulette Animation Settings")]
    [SerializeField] private float rouletteDuration = 3f;            // 룰렛 총 지속 시간
    [SerializeField] private float rouletteMinInterval = 0.03f;      // 최소 간격 (가장 빠를 때)
    [SerializeField] private float rouletteMaxInterval = 0.3f;       // 최대 간격 (가장 느릴 때)

    private List<BaseCardUi> cardList = new List<BaseCardUi>();
    private HorizontalLayoutGroup layoutGroup;
    private List<int> allUnitIds = new List<int>(); // 룰렛에 사용할 전체 유닛 ID 리스트
    private CanvasGroup canvasGroup;
    private RectTransform prfRectTransform; // prfTrans의 RectTransform


    private void Awake()
    {
        if (!TryGetComponent<CanvasGroup>(out canvasGroup))
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        layoutGroup = prfTrans.GetComponent<HorizontalLayoutGroup>();
        prfRectTransform = prfTrans.GetComponent<RectTransform>();
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

            // 모든 카드를 투명하게 설정
            card.CanvasGroup.alpha = 0f;
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

        // 모든 애니메이션 끝난 후 LayoutGroup 다시 활성화 및 공격력 텍스트 애니메이션
        Sequence finalSequence = DOTween.Sequence();
        finalSequence.AppendInterval(totalAnimTime)
            .AppendCallback(() =>
            {
                if (layoutGroup != null)
                {
                    layoutGroup.enabled = true;
                }

                // 총 공격력 계산 및 표시
                int totalAttack = CalculateTotalAttack();
                attackPowerText.text = totalAttack.ToString();

                // attackPowerTextCG 페이드 인
                attackPowerTextCG.alpha = 0f;
            })
            .Append(attackPowerTextCG.DOFade(1f, 0.5f).SetEase(Ease.InQuad))
            .AppendInterval(0.6f)
            .AppendCallback(() =>
            {
                // 터치 회수로 변경하면서 살짝 커지는 효과
                titleText.text = "총 터치 가능 횟수";
                attackPowerText.text = gameManager.RemainTouchCount.ToString();
                attackPowerText.color = Color.yellow;
            })
            .Append(attackPowerText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f))
            .AppendInterval(1f) // 딜레이
            .AppendCallback(() =>
            {
                // 카드 슬라이드 다운 애니메이션 시작
                PlayCardSlideDownAnimation();

                // canvasGroup 페이드 아웃 후 비활성화
                canvasGroup.DOFade(0f, 0.5f).SetEase(Ease.OutQuad).OnComplete(() =>
                {
                    canvasGroup.gameObject.SetActive(false);
                });
            });
    }

    // 총 공격력 계산
    private int CalculateTotalAttack()
    {
        int totalAttack = 0;
        List<int> unitIds = gameManager.draftUnitList.ToList();

        foreach (int unitId in unitIds)
        {
            UnitData unitData = DataTableManager.UnitTable?.Get(unitId);
            if (unitData != null)
            {
                totalAttack += unitData.ATTACK;
            }
        }

        return totalAttack;
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
        card.transform.localScale = Vector3.one * startScale;
        card.CanvasGroup.alpha = 0f;

        Sequence sequence = DOTween.Sequence();

        sequence.AppendInterval(index * delayBetweenPopCards);

        // 크기 확상
        sequence.Append(card.transform.DOScale(popScale, slowGrowDuration).SetEase(Ease.Linear));

        // 정상 크기로 축소하며 페이드 인
        sequence.Append(card.transform.DOScale(1f, shrinkDuration).SetEase(Ease.InCubic));
        sequence.Join(card.CanvasGroup.DOFade(1f, shrinkDuration));

        // 애니메이션 끝난 후 텍스트 애니메이션 적용
        sequence.OnComplete(() =>
        {
            // 유닛 데미지 설정
            List<int> unitIds = gameManager.draftUnitList.ToList();
            if (index < unitIds.Count)
            {
                int unitId = unitIds[index];
                UnitData unitData = DataTableManager.UnitTable?.Get(unitId);
                if (unitData != null)
                {
                    card.SetTitleText(unitData.ATTACK.ToString());
                }
            }

            if (card.Text != null)
            {
                UnitDamageTextAnimation(card.Text);
            }
        });
    }

    // 슬라이드 애니메이션 (랜덤 유닛 애니메이션)
    private void PlaySlideUpAnimation(BaseCardUi card, int index, float startDelay)
    {
        RectTransform rectTransform = card.GetComponent<RectTransform>();

        Vector3 targetPos = rectTransform.anchoredPosition;

        rectTransform.anchoredPosition = targetPos + Vector3.down * slideDistance;
        card.CanvasGroup.alpha = 0f;

        // 슬라이드 시작 시 이미지를 검은색으로 설정
        card.SetImageColor(Color.black);

        float delay = startDelay + beforeStartSlideAnimDelay + index * delayBetweenSlideCards;

        // 슬라이드 애니메이션 시작 시 텍스트 페이드 인
        DOVirtual.DelayedCall(delay, () =>
        {
            if (card.Text != null)
            {
                UnitDamageTextAnimation(card.Text);
            }
        });

        // 슬라이드 애니메이션 시작
        rectTransform.DOAnchorPos(targetPos, slideAnimDuration).SetEase(Ease.OutCubic).SetDelay(delay);
        card.CanvasGroup.DOFade(1f, slideAnimDuration).SetEase(Ease.InQuad).SetDelay(delay);

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
                    // 검은색 상태에서 실제 유닛 이미지 먼저 적용
                    SetFinalUnitImage(card, capturedSlideIndex);

                    // 몇 초 후 팝 이펙트 발생
                    card.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f)
                        .SetDelay(0.2f) // 이미지 적용 후 0.2초 딜레이
                        .OnStart(() =>
                        {
                            // 팝 이펙트 시작 시 이미지를 하얀색으로 변경
                            card.SetImageColor(Color.white);
                        })
                        .OnComplete(() =>
                        {
                            // 펀치 애니메이션 끝난 후 유닛 공격력으로 텍스트 변경
                            int existingCount = gameManager.ExistingUnitCount;
                            int cardIndex = existingCount + capturedSlideIndex;
                            List<int> unitIds = gameManager.draftUnitList.ToList();
                            if (cardIndex < unitIds.Count)
                            {
                                int unitId = unitIds[cardIndex];
                                UnitData unitData = DataTableManager.UnitTable?.Get(unitId);
                                if (unitData != null)
                                {
                                    card.SetTitleText(unitData.ATTACK.ToString());
                                }
                            }
                        });
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

    // 유닛 공격력 텍스트의 애니메이션
    // 페이드 인 적용
    private void UnitDamageTextAnimation(TextMeshProUGUI tmp)
    {
        // 초기 투명도 설정
        tmp.alpha = 0f;

        // 페이드 인 애니메이션
        tmp.DOFade(1f, 0.5f).SetEase(Ease.InQuad);
    }

    // 카드들이 순차적으로 위로 올라갔다가 아래로 내려가는 애니메이션
    private void PlayCardSlideDownAnimation()
    {
        // prfTrans를 canvasGroup 밖으로 분리
        Vector3 worldPos = prfRectTransform.position;
        prfRectTransform.SetParent(canvasGroup.transform.parent, false);
        prfRectTransform.position = worldPos;

        // 앵커를 화면 맨 아래로 변경 (가로는 stretch 유지, 세로만 아래로)
        Vector3 currentWorldPos = prfRectTransform.position;
        prfRectTransform.anchorMin = new Vector2(0f, 0f);
        prfRectTransform.anchorMax = new Vector2(1f, 0f);
        prfRectTransform.pivot = new Vector2(0.5f, 0f);
        prfRectTransform.position = currentWorldPos;

        // prfTrans의 현재 Y 위치 저장
        float containerY = prfRectTransform.anchoredPosition.y;

        // 각 카드를 순차적으로 아래로 내리기
        for (int i = 0; i < cardList.Count; i++)
        {
            BaseCardUi card = cardList[i];
            RectTransform cardRect = card.GetComponent<RectTransform>();
            float originalY = cardRect.anchoredPosition.y;
            float cardHeight = cardRect.rect.height;

            Sequence sequence = DOTween.Sequence();

            // 카드별 딜레이
            sequence.AppendInterval(i * delayBetweenCardSlideDown);

            // 아래로 내려가기
            sequence.Append(cardRect.DOAnchorPosY(originalY - containerY + cardHeight, cardSlideDownDuration).SetEase(Ease.InBack));
        }
    }
}