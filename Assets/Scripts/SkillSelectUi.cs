using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class SkillSelectUi : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject skillSelectPanel;
    [SerializeField] private Button confirmButton;

    [Header("Skill Card Prefab")]
    [SerializeField] private SkillCardUi skillCardPrefab;
    [SerializeField] private Transform cardContainer;

    [Header("Managers")]
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private GameManager gameManager;

    [Header("Skill Slot UI")]
    [SerializeField] private SkillUiControl skillSlotUI;  // 스킬 사용 UI 슬롯들

    public bool isBossPriceActive = false;

    public event Action<int> OnSkillSelected;

    private SkillCardUi[] skillCardUIs;
    private SkillCardUi selectedSkillCard = null;
    private int selectedSkillIndex = -1;
    private List<int> randomSkillIndices = new List<int>();

    private void Awake()
    {

        skillCardUIs = new SkillCardUi[3];


        if (skillCardPrefab == null)
        {
            return;
        }


        CreateSkillCards(3);

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnClickConfirmButton);
        }
    }

    private void OnEnable()
    {
        Show();
    }

    private void OnDisable()
    {
        // 선택 상태 초기화
        if (selectedSkillCard != null)
        {
            selectedSkillCard.SetFocus(false);
            selectedSkillCard = null;
        }
        selectedSkillIndex = -1;
    }

    public void Show()
    {
        RandomSkill();
        if (skillSelectPanel != null)
        {
            skillSelectPanel.SetActive(true);
        }
    }

    // 스킬 카드 UI 생성
    private void CreateSkillCards(int amount)
    {

        Transform parent = cardContainer != null ? cardContainer : transform;

        for (int i = 0; i < amount; i++)
        {
            var card = Instantiate(skillCardPrefab, parent);
            skillCardUIs[i] = card;

            int cardIndex = i;
            card.OnCardClickedCallback = () => OnSkillCardSelected(cardIndex);

            card.gameObject.SetActive(false);
        }

    }

    // 랜덤 스킬 뽑기
    private void RandomSkill()
    {
        if (skillManager == null)
        {
            return;
        }

        int totalSkillCount = skillManager.GetTotalSkillCount();

        if (totalSkillCount == 0)
        {
            return;
        }

        List<int> allIndices = Enumerable.Range(0, totalSkillCount).ToList();
        randomSkillIndices.Clear();

        int selectCount = Mathf.Min(3, totalSkillCount);
        for (int i = 0; i < selectCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, allIndices.Count);
            randomSkillIndices.Add(allIndices[randomIndex]);
            allIndices.RemoveAt(randomIndex);
        }


        UpdateButtonsWithSkills();
    }

    // 스킬 카드 UI 업데이트
    private async void UpdateButtonsWithSkills()
    {
        if (skillCardUIs == null)
        {
            return;
        }

        if (randomSkillIndices == null || randomSkillIndices.Count == 0)
        {
            return;
        }

        await DataTableManager.InitAsync();

        for (int i = 0; i < skillCardUIs.Length && i < randomSkillIndices.Count; i++)
        {
            if (skillCardUIs[i] == null)
            {

                continue;
            }

            if (skillManager == null)
            {
                return;
            }

            int skillIndex = randomSkillIndices[i];
            int skillId = skillManager.GetSkillID(skillIndex);

            if (skillId == -1)
            {

                continue;
            }


            skillCardUIs[i].SetPlayerSkillId(skillId);
            skillCardUIs[i].gameObject.SetActive(true);
        }


    }

    public void OnSkillCardSelected(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= skillCardUIs.Length)
        {

            return;
        }


        if (selectedSkillCard != null)
        {
            selectedSkillCard.SetFocus(false);
        }

        // 새로운 카드 선택
        selectedSkillCard = skillCardUIs[cardIndex];
        selectedSkillCard.SetFocus(true);


        selectedSkillIndex = randomSkillIndices[cardIndex];
        if (confirmButton == null)
        {
            ConfirmSelection();
        }
    }

    public void OnClickConfirmButton()
    {
        if (selectedSkillCard == null || selectedSkillIndex == -1)
        {
            return;
        }

        ConfirmSelection();
    }

    private void ConfirmSelection()
    {
        if (selectedSkillCard != null)
        {
            selectedSkillCard.SetFocus(false);
            selectedSkillCard = null;
        }
        
        OnSkillSelected?.Invoke(selectedSkillIndex);
        
        if (skillSelectPanel != null)
        {
            skillSelectPanel.SetActive(false);
        }
        SetActiveCards(false);
        if (gameManager != null)
        {
            gameManager.ResumeGame();
        }

    }

    private void SetActiveCards(bool value)
    {
        if (skillCardUIs == null) return;

        foreach (var card in skillCardUIs)
        {
            if (card != null)
            {
                card.gameObject.SetActive(value);
            }
        }
    }

    private void OnDestroy()
    {

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
        }


        if (skillCardUIs != null)
        {
            foreach (var card in skillCardUIs)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
        }
    }
}