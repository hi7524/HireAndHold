using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public enum DraggableUnitType
{
    LevelUp,
    Inventory,
}

public class DraggableGridUnitUi : MonoBehaviour, IDraggable
{
    private const float TransparentAlpha = 0f;
    private const float OpaqueAlpha = 1f;
    private const float PreviewInitialScale = 0.5f;
    private const float PreviewFinalScale = 1.0f;
    private const float PreviewScaleDuration = 0.15f;

    [SerializeField] private Image unitImg;
    [SerializeField] private Transform previewObjTrans; // 드래그 중에 보여질 프리뷰 오브젝트

    public GameObject GameObject => gameObject;
    public bool IsDraggable => isDraggable;

    public int UnitId { get; private set; }
    public int StarLevel { get; private set; } = 1; // 성급 (1~3성)
    public UnitGridData GridData { get; private set; }
    public DraggableUnitType DraggableUnitType { get; private set; }

    public event Action OnUnitDroppedSuccessfully;

    private GridPreviewHelper previewHelper;
    private bool isDraggable = true;
    private Vector3 initialLocalPosition;
    private UnitInventory inventory; // Inventory 타입일 때만 사용
    private bool dropFailed = false;


    private void Awake()
    {
        // 초기 로컬 위치 저장
        initialLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        // 활성화 시 초기 위치로 복구
        transform.localPosition = initialLocalPosition;
    }

    // 드래그 가능 여부를 설정
    public void SetDraggableState(bool value)
    {
        isDraggable = value;
    }

    // 유닛 ID와 성급을 설정
    public void SetUnit(int unitId, int starLevel = 1)
    {
        UnitId = unitId;
        StarLevel = Mathf.Clamp(starLevel, 1, 3);
    }

    public void SetDraggableUnitType(DraggableUnitType type)
    {
        DraggableUnitType = type;
    }

    // Inventory 타입일 때 UnitInventory 참조 설정
    public void SetInventory(UnitInventory inventory)
    {
        this.inventory = inventory;
    }

    // 그리드 데이터 설정 및 프리뷰 초기화
    public void SetGridData(UnitGridData gridData)
    {
        GridData = gridData;

        if (previewHelper == null)
        {
            previewHelper = new GridPreviewHelper(previewObjTrans, GameConstants.previewCellSizeUi);
        }

        if (previewHelper.HasCells)
        {
            UpdatePreviewImages(gridData);
        }
        else
        {
            previewHelper.CreatePreview(gridData);
            previewHelper.Hide();
        }
    }

    // 기존 프리뷰 이미지를 새로운 그리드 데이터로 업데이트
    public void UpdatePreviewImages(UnitGridData newGridData)
    {
        if (newGridData == null)
            return;

        GridData = newGridData;

        if (previewHelper == null)
        {
            previewHelper = new GridPreviewHelper(previewObjTrans, GameConstants.previewCellSizeUi);
        }

        previewHelper.UpdatePreview(newGridData);
        previewHelper.Hide();
    }


    // 드래그 시작
    public void OnDragStart()
    {
        // 프리뷰 셀 표시 및 유닛 이미지 투명화
        SetImageAlpha(TransparentAlpha);
        ShowPreviewCells();
    }

    // 드래그 중
    public void OnDrag()
    {
    }

    // 드래그 종료
    public void OnDragEnd()
    {
        // 프리뷰 셀 숨김 및 메인 이미지 불투명화
        HidePreviewCells();
        SetImageAlpha(OpaqueAlpha);

        // Inventory 타입이고 드롭 실패하지 않았으면 인벤토리에서 제거
        if (DraggableUnitType == DraggableUnitType.Inventory && !dropFailed && inventory != null)
        {
            inventory.RemoveUnit(UnitId);
        }
        dropFailed = false;
    }

    // 드롭 실패
    public void OnDropFailed()
    {
        dropFailed = true;
    }

    // 드롭 성공
    public void OnDropSuccess()
    {
        // 유닛을 비활성화하고 성공 이벤트를 발생
        gameObject.SetActive(false);
        OnUnitDroppedSuccessfully?.Invoke();
    }

    // 유닛 이미지의 투명도 조정
    private void SetImageAlpha(float alpha)
    {
        unitImg.color = new Color(1, 1, 1, alpha);
    }

    // 프리뷰 셀 활성화
    private void ShowPreviewCells()
    {
        if (previewHelper == null || !previewHelper.HasCells)
            return;

        previewObjTrans.localScale = PreviewInitialScale * Vector3.one;
        previewHelper.Show();
        previewObjTrans.DOScale(PreviewFinalScale, PreviewScaleDuration);
    }

    // 프리뷰 셀 숨기기
    private void HidePreviewCells()
    {
        previewHelper?.Hide();
    }
}