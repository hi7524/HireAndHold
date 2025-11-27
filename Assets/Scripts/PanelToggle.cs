using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    public GameObject targetPanel;
    public GameObject overlay;   // 전체 화면 클릭 감지용 투명 배경

    public void TogglePanel()
    {
        bool isActive = !targetPanel.activeSelf;

        targetPanel.SetActive(isActive);
        if (overlay != null)
            overlay.SetActive(isActive);
    }
}
