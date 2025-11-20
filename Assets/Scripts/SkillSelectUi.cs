using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class SkillSelectUi : MonoBehaviour
{
    [SerializeField] private GameObject skillSelectPanel;
    [SerializeField] private Button[] skillSelectButtons;
    [SerializeField] private Button skillSelectButton;
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private GameManager gameManager;

    public bool isBossPriceActive = false;
    public event Action<int> OnSkillSelected;

    private int selectedSkillIndex = 0;
    private List<int> randomSkillIndices = new List<int>();

    private void Awake()
    {
        skillSelectPanel.SetActive(false);


        // 각 버튼에 클릭 이벤트 연결
        for (int i = 0; i < skillSelectButtons.Length; i++)
        {
            int buttonIndex = i;
            skillSelectButtons[i].onClick.AddListener(() => SelectSkill(buttonIndex));
        }
    }

    public void Show()
    {

        RandomSkill();
        skillSelectPanel.SetActive(true);

    }
    private void RandomSkill()
    {
        int totalSkillCount = skillManager.GetTotalSkillCount();
        List<int> allIndices = Enumerable.Range(0, totalSkillCount).ToList();
        randomSkillIndices.Clear();

        for (int i = 0; i < 3; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, allIndices.Count);
            randomSkillIndices.Add(allIndices[randomIndex]);
            allIndices.RemoveAt(randomIndex);
        }

        UpdateButtonsWithSkills();
    }


    public void SelectSkill(int index)
    {
        selectedSkillIndex = randomSkillIndices[index];
        ConfirmSelection();

    }
    private void UpdateButtonsWithSkills()
    {
        for (int i = 0; i < skillSelectButtons.Length && i < randomSkillIndices.Count; i++)
        {
            int skillIndex = randomSkillIndices[i];

            TextMeshProUGUI buttonText = skillSelectButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = skillManager.GetSkillID(skillIndex).ToString();
            }


        }
    }

    private void ConfirmSelection()
    {
        skillSelectPanel.SetActive(false);
        OnSkillSelected?.Invoke(selectedSkillIndex);
        gameManager.ResumeGame();
    }
}
