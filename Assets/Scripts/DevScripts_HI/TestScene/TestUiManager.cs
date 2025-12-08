using UnityEngine;

public class TestUiManager : MonoBehaviour
{
    [SerializeField] private GameObject unitPanel;
    [SerializeField] private GameObject monsterPanel;
    [SerializeField] private GameObject bossPanel;

    private void Start()
    {
        DeactivateAll();
    }

    public void DeactivateAll()
    {
        unitPanel.SetActive(false);
        monsterPanel.SetActive(false);
        bossPanel.SetActive(false);
    }

    public void ActivePanel(GameObject panel)
    {
        DeactivateAll();
        panel.SetActive(true);
    }
}