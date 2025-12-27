using UnityEngine;
using DG.Tweening;

public class UnitSellZone : MonoBehaviour, IDroppable
{
    [Header("Managers")]
    [SerializeField] private StageUiManager uiManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerStageGold playerGold;
    [SerializeField] private DragManager dragManager;
    [SerializeField] private LevelUpRewardController levelUpRewardController;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip sellClip;
    [SerializeField] private AudioClip unitSellFailedClip;

    [Header("Animation Settings")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float soldScaleMultiplier = 0.9f;
    [SerializeField] private float scaleDuration = 0.2f;
    [SerializeField] private float showHideAnimDuration = 0.3f;

    private const string RewardUnitBlockedMsg = "레벨업 보상 유닛은 즉시 배치해야 합니다!";
    private const string MinUnitRequiredMsg = "최소 1개의 유닛은 배치되어야 합니다!";
    private const int Star1SellPrice = 25;
    private const int Star2SellPrice = 50;
    private const int Star3SellPrice = 100;

    private AudioSource audioSource;
    private Vector3 originalSize;
    private CanvasGroup canvasGroup;
    private bool isVisible = false;
    private Tween showHideTween;


    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        originalSize = transform.localScale;
        canvasGroup = GetComponent<CanvasGroup>();

        // CanvasGroup이 없으면 추가
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // 드래그 이벤트 구독
        if (dragManager != null)
        {
            dragManager.OnDragStarted += OnDragStarted;
            dragManager.OnDragEnded += OnDragEnded;
        }

        // 초기 상태 설정 (LevelUpRewardController가 활성화되어 있으면 활성화)
        UpdateVisibility();
    }

    private void Update()
    {
        // LevelUpRewardController의 활성화 상태가 변경되면 UnitSellZone 표시 업데이트
        // 드래그 중이 아닐 때만 체크
        if (!dragManager.IsDragging)
        {
            UpdateVisibility();
        }
    }

    // 현재 상태에 따라 표시/숨김 업데이트
    private void UpdateVisibility()
    {
        if (IsLevelUpRewardActive())
        {
            SetVisible(true);
        }
        else
        {
            SetVisible(false);
        }
    }

    private void OnDestroy()
    {
        // 드래그 이벤트 구독 해제
        if (dragManager != null)
        {
            dragManager.OnDragStarted -= OnDragStarted;
            dragManager.OnDragEnded -= OnDragEnded;
        }

        // 애니메이션 정리
        if (showHideTween != null && showHideTween.IsActive())
        {
            showHideTween.Kill();
        }
    }

    // 드래그 시작 시 호출
    private void OnDragStarted()
    {
        // LevelUpRewardController가 활성화되어 있으면 이미 활성화되어 있으므로 무시
        if (IsLevelUpRewardActive())
            return;

        // GridUnit 드래그 시 활성화
        if (dragManager != null && dragManager.IsDragging)
        {
            SetVisible(true);
        }
    }

    // 드래그 종료 시 호출
    private void OnDragEnded()
    {
        // LevelUpRewardController가 활성화되어 있으면 계속 활성화 유지
        if (IsLevelUpRewardActive())
            return;

        SetVisible(false);
    }

    // LevelUpRewardController가 활성화되어 있는지 확인
    private bool IsLevelUpRewardActive()
    {
        return levelUpRewardController != null && levelUpRewardController.gameObject.activeInHierarchy;
    }

    // UnitSellZone 표시/숨김
    private void SetVisible(bool visible)
    {
        // 이미 같은 상태면 무시
        if (isVisible == visible)
            return;

        isVisible = visible;

        // 기존 애니메이션이 있으면 중지
        if (showHideTween != null && showHideTween.IsActive())
        {
            showHideTween.Kill();
        }

        if (canvasGroup == null)
            return;

        if (visible)
        {
            // 활성화: 작은 크기에서 원래 크기로 커지면서 페이드 인
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            transform.localScale = originalSize * 0.5f;
            canvasGroup.alpha = 0f;

            Sequence showSequence = DOTween.Sequence();
            showSequence.Append(transform.DOScale(originalSize, showHideAnimDuration).SetEase(Ease.OutBack));
            showSequence.Join(canvasGroup.DOFade(1f, showHideAnimDuration).SetEase(Ease.OutQuad));

            showHideTween = showSequence;
        }
        else
        {
            // 비활성화: 원래 크기에서 작아지면서 페이드 아웃
            Sequence hideSequence = DOTween.Sequence();
            hideSequence.Append(transform.DOScale(originalSize * 0.5f, showHideAnimDuration).SetEase(Ease.InBack));
            hideSequence.Join(canvasGroup.DOFade(0f, showHideAnimDuration).SetEase(Ease.InQuad));
            hideSequence.OnComplete(() =>
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            });

            showHideTween = hideSequence;
        }
    }

    public bool CanDrop(IDraggable draggable)
    {
        // DraggableGridUnitUi는 인벤토리에 올릴 수 없음 (레벨업 보상)
        if (draggable.GameObject.GetComponent<DraggableGridUnitUi>() != null)
            return false;

        // 유닛이 아닐 경우 드롭할 수 없음
        var unit = draggable.GameObject.GetComponent<GridUnit>();
        if (unit == null)
        {
            PlayFailSound();
            return false;
        }

        // 방금 획득한 유닛은 인벤토리에 올릴 수 없음
        if (!unit.canPlaceInInventory)
        {
            uiManager.UpdateInfoText(RewardUnitBlockedMsg, Color.red);
            PlayFailSound();
            return false;
        }

        // 마지막 남은 유닛은 판매할 수 없음
        if (gridManager != null && gridManager.IsLastUnitOnGrid)
        {
            uiManager.UpdateInfoText(MinUnitRequiredMsg, Color.red);
            PlayFailSound();
            return false;
        }

        return true;
    }

    public void OnDragEnter(IDraggable draggable)
    {
        // DraggableGridUnitUi는 애니메이션 없이 리턴
        if (draggable.GameObject.GetComponent<DraggableGridUnitUi>() != null)
            return;

        transform.DOScale(originalSize * scaleMultiplier, scaleDuration)
            .SetEase(Ease.OutBack);
    }

    public void OnDragExit(IDraggable draggable)
    {
        transform.DOScale(originalSize, scaleDuration)
            .SetEase(Ease.InBack);
    }

    public void OnDrop(IDraggable draggable)
    {
        if (!CanDrop(draggable))
            return;

        var unit = draggable.GameObject.GetComponent<GridUnit>();
        HandleGridUnitDrop(unit);
        PlaySellSound();
    }

    // GridUnit 드롭 처리
    private void HandleGridUnitDrop(GridUnit gridUnit)
    {
        int gold = CalculateSellValue(gridUnit);
        playerGold.AddCredit(gold);
        ShowSellMessage(gold);

        // 유닛 제거 전에 카운트 감소
        if (gridManager != null)
        {
            gridManager.DecrementUnitCount();
        }

        gridUnit.gameObject.SetActive(false);
        //PlayDropAnimation();
    }

    // 판매 금액 계산
    private int CalculateSellValue(GridUnit gridUnit)
    {
        if (gridUnit == null)
            return 0;

        return gridUnit.StarLevel switch
        {
            1 => Star1SellPrice,
            2 => Star2SellPrice,
            3 => Star3SellPrice,
            _ => 0
        };
    }

    // 판매 완료 메시지 표시
    private void ShowSellMessage(int gold)
    {
        string msg = $"+{gold}G";
        uiManager.UpdateInfoText(msg);
    }

    // 유닛 판매 효과음
    private void PlaySellSound()
    {
        if (audioSource != null && sellClip != null)
        {
            audioSource.PlayOneShot(sellClip);
        }
    }

    // 유닛 판매 실패 효과음
    private void PlayFailSound()
    {
        if (audioSource != null && unitSellFailedClip != null)
        {
            audioSource.PlayOneShot(unitSellFailedClip);
        }
    }
}