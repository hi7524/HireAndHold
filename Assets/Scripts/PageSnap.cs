using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;

public class PageSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public static int SelectedStageId { get; set; } = 701;
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

        if (DatabaseManager.Instance != null && DatabaseManager.Instance.IsInitialized)
        {
            unlockedStage = DatabaseManager.Instance.CurrentUser?.profile?.highestStage ?? 701;
            Debug.Log($"[PageSnap] 유저 최고 스테이지: {unlockedStage}");
        }
        else
        {
            Debug.LogWarning("[PageSnap] DatabaseManager 초기화 안됨. 기본값 사용");
        }
        totalStages = DataTableManager.StageTable.GetAll().Count();
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

        // InitStages();

        Canvas.ForceUpdateCanvases();

        SetContentPadding();

        Canvas.ForceUpdateCanvases();
        CalculatePagePositions();
        int highestStage = Mathf.Clamp(unlockedStage - 701, 0, totalStages - 1);
        SnapToPage(highestStage, true);
        if (playButton != null)
        {
            Button btn = playButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(OnPlayButtonClick);
            }
        }
    }

    private async void OnPlayButtonClick()
    {
        StageCard selectedCard = stageCards[currentIndex];

        if (selectedCard.isLocked)
        {
            Debug.LogWarning("잠긴 스테이지입니다.");
            return;
        }

        SelectedStageId = selectedCard.stageIndex;

        // Stage 씬으로 이동
        LoadingRequest request = new LoadingRequest("Stage");
        await LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }

    private void OnDestroy()
    {
        if (playButton != null)
        {
            Button btn = playButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnPlayButtonClick);
            }
        }
    }


    private void CreateStageCards()
    {
        var stageTable = DataTableManager.StageTable;
        if (stageTable == null)
        {
            Debug.LogError("[PageSnap] StageTable을 찾을 수 없습니다.");
            return;
        }

        for (int i = 0; i < totalStages; i++)
        {
            int stageId = i + 701;
            StageData stageData = stageTable.Get(stageId);

            if (stageData == null)
            {
                Debug.LogWarning($"[PageSnap] Stage ID {stageId} 데이터를 찾을 수 없습니다.");
                continue;
            }

            // 카드 생성
            GameObject cardObj = Instantiate(stageCardPrefab, content);
            cardObj.name = $"StageCard_{stageId}";

            StageCard card = cardObj.GetComponent<StageCard>();
            if (card != null)
            {
                // stageDataList가 있으면 UI 데이터 우선 사용
                if (stageDataList != null && i < stageDataList.Length)
                {
                    card.ApplyData(stageDataList[i]);
                }
                else
                {
                    // StageDataList가 없으면 DataTable에서 이름만 가져오기
                    card.stageName.text = stageData.STAGE_NAME;
                    card.stageIndex = stageId;

                    // 잠금 여부는 유저 데이터에서 확인
                    bool isLocked = stageId > unlockedStage;
                    card.SetLocked(isLocked);
                    Debug.Log($"[PageSnap] Stage ID {stageId} 잠금 여부: {isLocked}");

                    // TODO: stageImage는 Addressable로 로드하거나 리소스에서 가져오기
                    // 예: card.stageImage.sprite = await Addressables.LoadAssetAsync<Sprite>($"Stage_{i}").Task;
                }
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
