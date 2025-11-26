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
    
    [Header("보스 보상 (패시브 3개 + 플레이어 스킬 1개)")]
    [SerializeField] private GameObject bossRewardSection;
    [SerializeField] private SkillCardUi[] passiveRewardSlots; // 3개
    [SerializeField] private SkillCardUi playerSkillSlot;
    
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
        else
        {
            Debug.LogWarning("[RewardPanelController] 획득 가능한 패시브 스킬이 없습니다.");
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
            Debug.LogError("[RewardPanelController] PassiveSkillManager가 할당되지 않았습니다!");
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
        
        // 플레이어 스킬 1개 랜덤 선택
        int playerSkillId = GetRandomPlayerSkill();
        if (playerSkillId != -1)
        {
            playerSkillSlot.SetPassiveSkillId(playerSkillId);
        }
        
        ShowBox();
    }

    // 상자 표시
    private void ShowBox()
    {
        boxRoot.SetActive(true);
        rewardDisplayPanel.SetActive(false);
        isBoxOpened = false;

    }

    // 상자 클릭 이벤트
    private void OnBoxClick()
    {
        if (isBoxOpened) return;
        
        isBoxOpened = true;
        boxButton.interactable = false;
        
        // 상자 열리고 보상 표시
        rewardDisplayPanel.SetActive(true);
    }
    


    // 랜덤 플레이어 스킬 선택
    private int GetRandomPlayerSkill()
    {
        // TODO: 플레이어 스킬 풀 정의 필요
        // 임시로 액티브 스킬 중 랜덤 선택
        List<SkillData> activeSkills = new List<SkillData>();
        foreach (var skillData in DataTableManager.SkillTable.GetAll())
        {
            if (skillData.SKILL_ACTIVATE == 1) // 액티브 스킬 (SKILL_ACTIVATE: 1=액티브, 2=패시브)
            {
                activeSkills.Add(skillData);
            }
        }
        
        if (activeSkills.Count > 0)
        {
            return activeSkills[Random.Range(0, activeSkills.Count)].SKILL_ID;
        }
        return -1; // 유효한 스킬 ID가 없을 경우
    }

    // 확인 버튼 클릭
    private void OnConfirmClick()
    {
        // 보상으로 받은 패시브 스킬들을 실제로 적용
        foreach (int skillId in currentRewardSkillIds)
        {
            passiveSkillManager.AddOrUpgradePassiveSkill(skillId);
        }
        
        boxRoot.SetActive(false);
        gameManager.ResumeGame();
        gameObject.SetActive(false);
        
        Debug.Log($"[RewardBox] 보상 획득 완료: {currentRewardSkillIds.Count}개의 패시브 스킬");
    }

   
}
