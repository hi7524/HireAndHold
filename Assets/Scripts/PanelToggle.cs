using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    public GameObject targetPanel;

    public void TogglePanel()
    {
        targetPanel.SetActive(!targetPanel.activeSelf);
    }
}
