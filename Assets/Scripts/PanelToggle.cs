using UnityEngine;
using UnityEngine.UI;

public class PanelToggle : MonoBehaviour
{
    public GameObject targetPanel;
    public GameObject overlay;

    public void TogglePanel()
    {
        bool show = !targetPanel.activeSelf;
        targetPanel.SetActive(show);

        if (show)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(targetPanel.GetComponent<RectTransform>());
        }

        if (overlay != null)
            overlay.SetActive(show);
    }
}
