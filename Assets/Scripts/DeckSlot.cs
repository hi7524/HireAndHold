using System;
using UnityEngine;
using UnityEngine.UI;

public class DeckSlot : MonoBehaviour
{
    [Header("UI References")]
    public Image icon;
    public Sprite emptySprite;

    [Header("Lock UI")]
    public GameObject lockOverlay;     
    public Button slotButton;           

    [Header("Slot Settings")]
    public int slotIndex;  

    private DeckUnitModel committed;
    private DeckUnitModel pending;
    private DeckControl deck;

    private bool isLocked = false;

    public bool HasCommitted => committed != null;
    public bool HasPending => pending != null;
    public bool IsLocked => isLocked;

    public Action<DeckSlot> onSlotClickedExternal;

    public void SetDeckControl(DeckControl control)
    {
        deck = control;
    }

    private void Awake()
    {
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnClick);
        }
    }

    private void Start()
    {

        UpdateButtonInteractable(false);
    }

    /// <summary>
    /// 슬롯 잠금 상태 업데이트
    /// </summary>
    public void UpdateLockState(int presetIndex)
    {
        if (slotIndex < 2)
        {
            isLocked = false;
        }
        else
        {
            var userData = DatabaseManager.Instance.CurrentUser;
            if (userData?.presetSlotUnlocks == null)
            {
                isLocked = true;
            }
            else
            {
                isLocked = !userData.presetSlotUnlocks.IsSlotUnlocked(presetIndex, slotIndex);
            }
        }

        // 잠금 UI 표시/숨김
        if (lockOverlay != null)
        {
            lockOverlay.SetActive(isLocked);
        }

        // 잠긴 상태면 아이콘 숨김
        if (isLocked)
        {
            icon.sprite = emptySprite;
        }

        // 잠긴 슬롯은 즉시 활성화
        if (isLocked)
        {
            UpdateButtonInteractable(false);
        }
    }

    public void BeginEdit()
    {
        pending = null;
        ApplyCommittedToUI();
    }

    public void CancelPending()
    {
        pending = null;
        ApplyCommittedToUI();
    }

    public void OnClick()
    {
        // 잠긴 슬롯이면 DeckControl에 잠금 해제 요청
        if (isLocked)
        {
            if (deck != null)
            {
                deck.OnLockedSlotClicked(slotIndex, deck.GetActivePresetIndex());
            }
            return;
        }

        // 일반 슬롯 클릭 처리
        if (onSlotClickedExternal != null)
        {
            onSlotClickedExternal(this);
            return;
        }

        deck.OnSlotClicked(this);
    }

    public void SetPending(DeckUnitModel model)
    {
        if (isLocked)
            return;

        pending = model;
        if (pending != null)
        {
            pending.FixMissingAddress();
        }
        ApplyPendingToUI();
    }

    public void ClearPending()
    {
        if (pending != null)
        {
            deck.NotifyUnitCleared(pending);
        }
        pending = null;
        ApplyPendingToUI();
    }

    public void CommitPending()
    {
        if (committed != null)
        {
            deck.NotifyUnitCleared(committed);
        }
        committed = pending;
        pending = null;
        if (committed != null)
        {
            committed.FixMissingAddress();
        }
        ApplyCommittedToUI();
    }

    public DeckUnitModel GetCommitted() => committed;
    public DeckUnitModel GetPending() => pending;

    public void SetCommittedExternal(DeckUnitModel model)
    {
        if (isLocked)
        {
            committed = null;
            pending = null;
        }
        else
        {
            committed = model;
            if (committed != null)
            {
                committed.FixMissingAddress();
            }
            pending = null;
        }

        ApplyCommittedToUI();
    }

    void ApplyCommittedToUI()
    {
        if (isLocked)
        {
            icon.sprite = emptySprite;
        }
        else
        {
            icon.sprite = committed != null ? committed.icon : emptySprite;
        }
    }

    void ApplyPendingToUI()
    {
        if (isLocked)
        {
            icon.sprite = emptySprite;
        }
        else
        {
            icon.sprite = pending != null ? pending.icon : emptySprite;
        }
    }

    private void UpdateButtonInteractable(bool isEditMode)
    {
        if (slotButton != null)
        {

            slotButton.interactable = isLocked || isEditMode;
        }
    }

    public void SetInteractable(bool interactable)
    {
        UpdateButtonInteractable(interactable);
    }
}
