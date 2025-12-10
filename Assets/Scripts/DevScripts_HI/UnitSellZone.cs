using UnityEngine;
using DG.Tweening;

public class UnitSellZone : MonoBehaviour, IDroppable
{
    [Header("Managers")]
    [SerializeField] private StageUiManager uiManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerStageGold playerGold;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip sellClip;
    [SerializeField] private AudioClip unitSellFailedClip;

    [Header("Animation Settings")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float soldScaleMultiplier = 0.9f;
    [SerializeField] private float scaleDuration = 0.2f;

    private const string RewardUnitBlockedMsg = "레벨업 보상 유닛은 즉시 배치해야 합니다!";
    private const string MinUnitRequiredMsg = "최소 1개의 유닛은 배치되어야 합니다!";
    private const int Star1SellPrice = 25;
    private const int Star2SellPrice = 50;
    private const int Star3SellPrice = 100;

    private AudioSource audioSource;
    private Vector3 originalSize;


    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        originalSize = transform.localScale;
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
        if (gridManager != null && gridManager.IsLastUnitOnGrid())
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
        playerGold.AddGold(gold);
        ShowSellMessage(gold);

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