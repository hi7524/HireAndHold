using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 간단한 잠금 슬롯 - 클릭하면 알림 패널 표시
/// </summary>
public class LockedSlot : MonoBehaviour
{
    [SerializeField] private Button unlockButton;

    private int slotIndex;
    private int presetIndex;
    private DeckControl deckControl;

    private void Awake()
    {
        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(OnClicked);
        }
    }

    public void Initialize(int slotIdx, int presetIdx, DeckControl control)
    {
        slotIndex = slotIdx;
        presetIndex = presetIdx;
        deckControl = control;
    }

    private void OnClicked()
    {
        if (deckControl != null)
        {
            deckControl.OnLockedSlotClicked(slotIndex, presetIndex);
        }
    }
}
