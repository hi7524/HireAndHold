using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class RewardPanelController : MonoBehaviour
{
    
    [SerializeField] private GameObject boxRoot; // 전체 배경
    [SerializeField] private Button boxButton; // 클릭 가능한 상자 버튼
    [SerializeField] private Image boxImage; // 상자 이미지
    
    [Header("보상 표시 UI")]
    [SerializeField] private GameObject rewardDisplayPanel;
    [SerializeField] private CanvasGroup rewardCanvasGroup;
    
    [Header("워닝 타임 보상 (패시브 1개)")]
    [SerializeField] private GameObject warningRewardSection;
    [SerializeField] private SkillCardUi warningRewardSlot;
    
    [Header("보스 보상 (패시브 3개 + 플레이어 스킬 선택)")]
    [SerializeField] private GameObject bossRewardSection;
    [SerializeField] private SkillCardUi[] passiveRewardSlots; // 3개
    [SerializeField] private SkillSelectUi skillSelectUi;
    
    [Header("확인 버튼")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PassiveSkillManager passiveSkillManager;
    
    private List<int> currentRewardSkillIds = new List<int>();
    private bool isBoxOpened = false;
    private RewardType currentRewardType;
    
    private enum RewardType
    {
        Warning,
        Boss
    }

    private void Awake()
    {
        boxButton?.onClick.AddListener(OnBoxClick);
        confirmButton?.onClick.AddListener(OnConfirmClick);
        
        boxRoot.SetActive(false);
        rewardDisplayPanel.SetActive(false);
    }

    // 워닝 타임 보상 표시
    public void ShowWarningReward()
    {
        currentRewardType = RewardType.Warning;
        currentRewardSkillIds.Clear();
        
        if (passiveSkillManager == null)
        {
            return;
        }
        
        List<int> randomSkills = passiveSkillManager.GetRandomPassiveSkillsForReward(1);
        
        if (randomSkills.Count > 0)
        {
            currentRewardSkillIds = randomSkills;
            int skillId = randomSkills[0];
            
            warningRewardSection.SetActive(true);
            bossRewardSection.SetActive(false);
            
            // SkillCardUi에 스킬 ID 설정
            warningRewardSlot.SetPassiveSkillId(skillId);
        }
        
        ShowBox();
    }

    // 보스 보상 표시
    public void ShowBossReward()
    {
        currentRewardType = RewardType.Boss;
        currentRewardSkillIds.Clear();
        
        if (passiveSkillManager == null)
        {
            return;
        }
          
        warningRewardSection.SetActive(false);
        bossRewardSection.SetActive(true);
        

        List<int> selectedPassiveIds = passiveSkillManager.GetRandomPassiveSkillsForReward(3);
        currentRewardSkillIds = selectedPassiveIds;
        
        for (int i = 0; i < passiveRewardSlots.Length; i++)
        {
            if (i < selectedPassiveIds.Count)
            {
                passiveRewardSlots[i].SetPassiveSkillId(selectedPassiveIds[i]);
                passiveRewardSlots[i].gameObject.SetActive(true);
            }
            else
            {
                passiveRewardSlots[i].gameObject.SetActive(false);
            }
        }
        
        ShowBox();
    }

    // 상자 표시
    private void ShowBox()
    {
        boxRoot.SetActive(true);
        rewardDisplayPanel.SetActive(false);
        confirmButton.gameObject.SetActive(false);
        isBoxOpened = false;
        boxButton.interactable = true; // 버튼 다시 활성화
    }

    // 상자 클릭 이벤트
    private void OnBoxClick()
    {
        if (isBoxOpened) return;

        isBoxOpened = true;
        boxButton.interactable = false;

        // 상자 열리고 보상 표시
        rewardDisplayPanel.SetActive(true);
        confirmButton.gameObject.SetActive(true);
    }
    


    // 확인 버튼 클릭
    private void OnConfirmClick()
    {
        // 보상으로 받은 패시브 스킬들을 실제로 적용
        foreach (int skillId in currentRewardSkillIds)
        {
            passiveSkillManager.AddOrUpgradePassiveSkill(skillId);
        }
        
        
        // 보스 보상이면 SkillSelectUi 열기
        if (currentRewardType == RewardType.Boss)
        {
            rewardDisplayPanel.SetActive(false);
            
            if (skillSelectUi != null)
            {
                skillSelectUi.Show();
                gameObject.SetActive(false);
            }
        }
        else
        {
            // 워닝 타임은 바로 패널 닫기
            rewardDisplayPanel.SetActive(false);
            boxRoot.SetActive(false);
            gameManager.ResumeGame();
            gameObject.SetActive(false);
        }
    }

   
}
