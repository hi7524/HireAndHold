using UnityEngine;
using UnityEngine.UI;
using System; 

public class SkillSelectUi : MonoBehaviour
{
    [SerializeField] private GameObject skillSelectPanel;
    [SerializeField] private Button skillSelectButton;
    [SerializeField] private TopUi topUi;

    public bool isBossPriceActive = false;
    public event Action<int> OnSkillSelected; 

    private int selectedSkillIndex = 0; 

    private void Awake()
    {
        skillSelectPanel.SetActive(false);
        skillSelectButton.onClick.AddListener(ConfirmSelection);
    }

    public void Show()
    {
      
        skillSelectPanel.SetActive(true);
        Time.timeScale = 0f; 
        
    }


    public void SelectSkill(int index)
    {
        selectedSkillIndex = index;
    }

    private void ConfirmSelection()
    {
        skillSelectPanel.SetActive(false);
        Time.timeScale = topUi.GetSpeedLevel[topUi.GetCurrentIndex];

        OnSkillSelected?.Invoke(selectedSkillIndex);
    }
}
