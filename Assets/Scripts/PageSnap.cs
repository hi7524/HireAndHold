using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class PageSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    //drag setting
    public ScrollRect scrollRect;
    public RectTransform content;
    public float snapDuration = 0.3f;

    public GameObject stageCardPrefab;
    public int totalStages = 10;

    public int unlockedStage = 3;

    public StageUIData[] stageDataList;


    //Play Button

    public GameObject playButton;

    private RectTransform[] pages;
    private StageCard[] stageCards;
    private float[] pagePositions;
    private int currentIndex = 0;
    private bool isDragging = false;


    private void Awake()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }


        if (content == null)
        {
            content = scrollRect.content;
        }

    }

    private void Start()
    {
        if (stageCardPrefab != null && content.childCount == 0)
        {
            CreateStageCards();
            CollectStageCards();
        }

        if (pages.Length == 0)
        {
            Debug.Log("X 프리팹");
            return;
        }

        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.035f;

        InitStages();

        Canvas.ForceUpdateCanvases();

        SetContentPadding();

        Canvas.ForceUpdateCanvases();
        CalculatePagePositions();
        SnapToPage(0, true);
    }

    private void CreateStageCards()
    {
        for (int i = 0; i < totalStages; i++)
        {
            GameObject cardObj = Instantiate(stageCardPrefab, content);
            cardObj.name = $"StageCard_{i + 1}";

            StageCard card = cardObj.GetComponent<StageCard>();

            if (stageDataList != null && i < stageDataList.Length)
            {
                card.ApplyData(stageDataList[i]);
            }
        }
    }


    private void CollectStageCards()
    {
        stageCards = content.GetComponentsInChildren<StageCard>(true);
        pages = stageCards.Select(c => c.GetComponent<RectTransform>()).ToArray();
    }

    private void InitStages()
    {
        for (int i = 0; i < stageCards.Length; i++)
        {
            StageCard card = stageCards[i];
            card.stageIndex = i + 1;
            card.SetLocked((i + 1) > unlockedStage);
        }
    }

    private void SetContentPadding()
    {
        if (pages.Length == 0)
        {
            return;
        }

        RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
        float viewportWidth = viewport.rect.width;

        float cardWidth = pages[0].rect.width;

        HorizontalOrVerticalLayoutGroup layoutGroup = content.GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            Debug.LogError("Content에 Horizontal Layout Group이 없습니다. 중앙 정렬이 작동하려면 필수입니다!");
            return;
        }

        float leftPadding = (viewportWidth - cardWidth) / 2f;
        float rightPadding = (viewportWidth - cardWidth) / 2f;

        layoutGroup.padding.left = Mathf.RoundToInt(leftPadding);
        layoutGroup.padding.right = Mathf.RoundToInt(rightPadding);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private void CalculatePagePositions()
    {
        pagePositions = new float[pages.Length];
        for (int i = 0; i < pages.Length; i++)
        {
            pagePositions[i] = -pages[i].anchoredPosition.x;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        DOTween.Kill(content);

        scrollRect.inertia = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateNearestPage();
    }
    private void UpdateNearestPage()
    {
        int nearest = GetNearestPageIndex();

        if (nearest != currentIndex)
        {
            currentIndex = nearest;

            ApplySelection(currentIndex);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        int nearest = GetNearestPageIndex();

        SnapToPage(nearest, false);
    }

    private int GetNearestPageIndex()
    {
        float currentX = content.anchoredPosition.x;
        int nearest = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < pagePositions.Length; i++)
        {
            float dist = Mathf.Abs(currentX - pagePositions[i]);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }
        return nearest;
    }

    private void SnapToPage(int index, bool immediate)
    {
        if (index < 0 || index >= pagePositions.Length)
        {
            return;
        }

        bool samePage = IsSamePage(index);
        currentIndex = index;

        Vector2 targetPos = new Vector2(pagePositions[index], content.anchoredPosition.y);

        if (immediate)
        {
            MoveContentImmediate(targetPos);
            OnSnapStart(index);
            return;
        }

        MoveContentSmooth(targetPos, index, samePage);
    }


    private bool IsSamePage(int index)
    {
        return currentIndex == index;
    }

    private void MoveContentImmediate(Vector2 targetPos)
    {
        content.anchoredPosition = targetPos;
        scrollRect.inertia = false;
    }


    private void MoveContentSmooth(Vector2 targetPos, int index, bool samePage)
    {
        DOTween.Kill(content);

        content.DOAnchorPos(targetPos, snapDuration).SetEase(Ease.OutCubic).OnStart(() => OnSnapStart(index)).OnComplete(() => OnSnapComplete(index, samePage));
    }

    private void OnSnapStart(int index)
    {
        ApplySelection(index);
    }

    private void OnSnapComplete(int index, bool samePage)
    {
        scrollRect.inertia = false;
        scrollRect.velocity = Vector2.zero;

        if (samePage)
        {
            ApplySelection(index);
        }
    }


    private void ApplySelection(int index)
    {
        if (index < 0 || index >= stageCards.Length)
        {
            return;
        }


        StageCard selectedCard = stageCards[index];

        if (selectedCard.isLocked)
        {
            playButton?.SetActive(false);

            foreach (var card in stageCards)
            {
                if (card == selectedCard)
                {
                    card.Select();
                }
                else
                {
                    card.Deselect();
                }
            }
            return;
        }

        foreach (var card in stageCards)
        {
            if (card == selectedCard)
            {
                card.Select();
            }
            else
            {
                card.Deselect();
            }
        }

        playButton?.SetActive(true);
    }
}
