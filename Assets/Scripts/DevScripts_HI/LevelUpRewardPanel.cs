using UnityEngine;

public class LevelUpRewardPanel : MonoBehaviour
{
    [SerializeField] private DragManager dragManager;

    private void OnEnable()
    {
        // 레벨업시 보상 패널이 활성화 되었을 때만 드래그 가능하도록 하기 위함
        if (dragManager == null)
        {
            Debug.LogError("DragManager를 할당해주세요.");
            return;
        }

        dragManager.SetDragEnabled(true);
    }

    private void OnDisable()
    {
        if (dragManager == null)
        {
            Debug.LogError("DragManager를 할당해주세요.");
            return;
        }

        dragManager.SetDragEnabled(false);
    }
}