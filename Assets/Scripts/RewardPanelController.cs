using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Tutorial;


public class RewardPanelController : MonoBehaviour
{
    [SerializeField] private GameObject boxRoot; // 전체 배경
    [SerializeField] private Button boxButton; // 클릭 가능한 상자 버튼
    [SerializeField] private Image boxImage; // 상자 이미지
    [SerializeField] private Sprite boxSprite; // 기본 상자 스프라이트
    [SerializeField] private Sprite opendBoxSprite; // 열린 후 상자 스프라이트
    [SerializeField] private GameObject canvasEffect; // 상자 열릴 때 나타나는 이펙트
    
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
    [SerializeField] private StageManager stageManager;
    [SerializeField] private DragManager dragManager;

    [Header("골드 보상")]
    [SerializeField] private int defaultGoldReward = 50;
    [SerializeField] private TextMeshProUGUI goldRewardText;  // WARNING_RE 골드 표시

    private List<int> currentRewardSkillIds = new List<int>();
    private List<int> goldRewardSlotIndices = new List<int>();  // 골드 카드로 대체된 슬롯 인덱스
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

    private void OnEnable()
    {
        // 상자 이미지를 기본 스프라이트로 초기화
        if (boxImage != null && boxSprite != null)
        {
            boxImage.sprite = boxSprite;
        }

        // Canvas Effect 비활성화
        if (canvasEffect != null)
        {
            canvasEffect.SetActive(false);
        }

        // 드래그 비활성화
        if (dragManager != null)
            dragManager.SetDragEnabled(false);
    }

    private void OnDisable()
    {
        // 드래그 재활성화
        if (dragManager != null)
            dragManager.SetDragEnabled(true);
    }

    // 워닝 타임 보상 표시
    public void ShowWarningReward()
    {
        currentRewardType = RewardType.Warning;
        currentRewardSkillIds.Clear();
        goldRewardSlotIndices.Clear();

        warningRewardSection.SetActive(true);
        bossRewardSection.SetActive(false);

        if (passiveSkillManager == null)
        {
            return;
        }

        // TutorialOverrides에서 스테이지별 보상 옵션 조회
        int currentStageId = stageManager != null ? stageManager.CurrentStageId : 0;
        var rewardOptions = GameEvents.GetRewardOptions?.Invoke(currentStageId) ?? RewardOptions.Default;

        int skillId;
        if (rewardOptions.ShieldRegenOnly)
        {
            // 방벽 회복 스킬만 반환
            skillId = passiveSkillManager.GetShieldRegenSkillId();

            if (skillId > 0)
            {
                currentRewardSkillIds.Add(skillId);
                warningRewardSlot.SetAsPassiveSkillCard(skillId);
                warningRewardSlot.FillNextStar();
            }
            else
            {
                // 방벽 회복 스킬이 이미 최대(3성)면 골드 카드
                warningRewardSlot.SetAsGoldCard(defaultGoldReward);
                goldRewardSlotIndices.Add(0);
            }
        }
        else
        {
            List<int> randomSkills = passiveSkillManager.GetRandomPassiveSkillsForReward(1);

            if (randomSkills.Count > 0)
            {
                currentRewardSkillIds = randomSkills;
                skillId = randomSkills[0];

                // SkillCardUi에 스킬 ID 설정
                warningRewardSlot.SetAsPassiveSkillCard(skillId);
                warningRewardSlot.FillNextStar();
            }
            else
            {
                // 스킬이 없으면 골드 카드로 대체
                warningRewardSlot.SetAsGoldCard(defaultGoldReward);
                goldRewardSlotIndices.Add(0);
            }
        }

        // 골드 보상 텍스트 업데이트
        UpdateGoldRewardText();

        ShowBox();
    }

    // 보스 보상 표시
    public void ShowBossReward()
    {
        currentRewardType = RewardType.Boss;
        currentRewardSkillIds.Clear();
        goldRewardSlotIndices.Clear();

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
                passiveRewardSlots[i].SetAsPassiveSkillCard(selectedPassiveIds[i]);
                passiveRewardSlots[i].gameObject.SetActive(true);
                passiveRewardSlots[i].FillNextStar();
            }
            else
            {
                // 스킬이 부족하면 골드 카드로 대체
                passiveRewardSlots[i].SetAsGoldCard(defaultGoldReward);
                passiveRewardSlots[i].gameObject.SetActive(true);
                goldRewardSlotIndices.Add(i);
            }
        }

        // 골드 보상 텍스트 업데이트
        UpdateGoldRewardText();

        ShowBox();
    }

    // 골드 보상 텍스트 업데이트
    private void UpdateGoldRewardText()
    {
        if (goldRewardText == null) return;

        int rewardGold = 0;
        if (stageManager != null && stageManager.CurrentStageData != null)
        {
            rewardGold = stageManager.CurrentStageData.WARNING_RE;
        }

        if (rewardGold > 0)
        {
            goldRewardText.gameObject.SetActive(true);
            goldRewardText.text = $"+{rewardGold:N0}G";
        }
        else
        {
            goldRewardText.gameObject.SetActive(false);
        }
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

        // 상자 축소 후 원래 크기로 복귀 애니메이션
        boxImage.transform.DOScale(0.8f, 0.2f)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // 작아진 상태에서 열린 상자 스프라이트로 변경
                if (opendBoxSprite != null)
                {
                    boxImage.sprite = opendBoxSprite;
                }

                // Canvas Effect 활성화
                if (canvasEffect != null)
                {
                    canvasEffect.SetActive(true);
                }

                boxImage.transform.DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        // 0.1초 대기 후 보상 표시
                        DOVirtual.DelayedCall(0.1f, () =>
                        {
                            // 상자 열리고 보상 표시
                            rewardDisplayPanel.SetActive(true);
                            confirmButton.gameObject.SetActive(true);

                            // 보상 패널 확대 애니메이션
                            rewardDisplayPanel.transform.localScale = Vector3.zero;
                            rewardDisplayPanel.transform.DOScale(1f, 0.4f)
                                .SetEase(Ease.OutBack)
                                .SetUpdate(true);
                        }).SetUpdate(true);
                    });
            });
    }
    
    // 확인 버튼 클릭
    private void OnConfirmClick()
    {
        // 보상으로 받은 패시브 스킬들을 실제로 적용
        foreach (int skillId in currentRewardSkillIds)
        {
            passiveSkillManager.AddOrUpgradePassiveSkill(skillId);
        }

        // 골드 카드 보상 지급 (스테이지 클리어 골드로 누적)
        if (goldRewardSlotIndices.Count > 0 && stageManager != null)
        {
            int totalGold = goldRewardSlotIndices.Count * defaultGoldReward;
            stageManager.AddBonusGold(totalGold);
        }

        // 보스 보상이면 SkillSelectUi 열기
        if (currentRewardType == RewardType.Boss)
        {
            // 중간 보스 처치 시 테이블의 WARNING_RE 골드 지급
            if (stageManager != null && stageManager.CurrentStageData != null)
            {
                int bossGold = stageManager.CurrentStageData.WARNING_RE;
                if (bossGold > 0)
                {
                    stageManager.AddBonusGold(bossGold);
                }
            }

            rewardDisplayPanel.SetActive(false);

            if (skillSelectUi != null)
            {
                skillSelectUi.Show();
                gameObject.SetActive(false);
            }
        }
        else
        {
            // 워닝 타임 보상: 테이블의 WARNING_RE 골드 지급
            if (stageManager != null && stageManager.CurrentStageData != null)
            {
                int warningGold = stageManager.CurrentStageData.WARNING_RE;
                if (warningGold > 0)
                {
                    stageManager.AddBonusGold(warningGold);
                }
            }

            // 워닝 타임은 바로 패널 닫기
            rewardDisplayPanel.SetActive(false);
            boxRoot.SetActive(false);
            gameManager.ResumeGame();
            gameObject.SetActive(false);
        }
    }
}
