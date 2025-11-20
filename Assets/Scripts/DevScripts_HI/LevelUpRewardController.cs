using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpRewardController : MonoBehaviour
{
    public GridDatasForTesting gridDatasForTesting;

    [Header("UI Prefabs")]
    [SerializeField] private UnitCardUi unitCardUiPrf;
    [SerializeField] private SkillCardUi skillCardPrf;
    [Header("UI")]
    [SerializeField] private Button reRollBtn;
    [Header("Others")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private UnitInventory inventory;
    [SerializeField] private PlayerStageGold playerStageGold;
    [SerializeField] private PlayerExperience playerExp;

    private int rerollCost = 50;

    private UnitCardUi[] unitCardUIs;
    private SkillCardUi[] skillCardUIs;

    private List<int> ownedUnitIdForTesting = new List<int> { 11101, 11104, 11107, 11110, 11113 }; // 테스트용***

    // 초기 세팅
    private void Start()
    {
        // 각 프리팹 카드 3장씩 생성
        CreateUnitCardPrf(3);
        CreateSkillCardPrf(3);

        // 레기업시 보상 뽑기
        playerExp.OnLevelUp += DrawLevelUpReward;

        // 스테이지 시작 전, 초기 배치를 위한 활성화
        SelectUnitOnGameStart();
    }

    private void OnDestroy()
    {
        playerExp.OnLevelUp -= DrawLevelUpReward;

        // UnitCardUI 이벤트 구독 해제
        if (unitCardUIs != null)
        {
            foreach (var card in unitCardUIs)
            {
                if (card != null)
                {
                    card.OnUnitDropSuccess -= OnUnitCardDropSuccess;
                }
            }
        }
    }

    // 스테이지 시작 전
    public void SelectUnitOnGameStart()
    {
        // 유닛뽑기
        DrawUnitID();
        SetActiveCards(unitCardUIs, true);

        // 관련 UI 활성화
        inventory.gameObject.SetActive(true);
        reRollBtn.gameObject.SetActive(true);
        UpdateRerollBtn();
    }

    // 레벌업시 보상 랜덤 뽑기
    public void DrawLevelUpReward()
    {
        // 관련 UI 업데이트
        inventory.gameObject.SetActive(true);
        reRollBtn.gameObject.SetActive(true);
        UpdateRerollBtn();

        // ** 수정 필요 
        for (int i = 0; i < unitCardUIs.Length; i++)
        {
            unitCardUIs[i].SetDragState(true);
            unitCardUIs[i].SetColor(Color.white);
        }

        DrawReward();

        gameManager.PauseGame();
    }

    // 실제 보상 뽑기
    private void DrawReward()
    {
        // 기존 카드들을 모두 비활성화
        SetActiveCards(skillCardUIs, false);
        SetActiveCards(unitCardUIs, false);

        // 플레이어 레벨이 3의 배수일 때 스킬 뽑기
        if (playerExp.Level % 3 == 0)
        {
            SetActiveCards(skillCardUIs, true);
        }
        // 그 외에는 유닛 뽑기
        else
        {
            DrawUnitID();
            SetActiveCards(unitCardUIs, true);
        }
    }

    // 완료 버튼 클릭
    public void OnClickConfirmBtn()
    {
        // 관련 UI 비활성화
        SetActiveCards(skillCardUIs, false);
        SetActiveCards(unitCardUIs, false);
        reRollBtn.gameObject.SetActive(false);

        if (!gameManager.IsGameStarted)
            gameManager.StartGame();
        else
            gameManager.ResumeGame();
    }

    // 유닛 3개 중복 없이 뽑기
    public void DrawUnitID()
    {
        List<int> tempList = new List<int>(ownedUnitIdForTesting);

        for (int i = tempList.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            int temp = tempList[i];
            tempList[i] = tempList[randomIndex];
            tempList[randomIndex] = temp;
        }

        for (int i = 0; i < 3 && i < unitCardUIs.Length; i++)
        {
            int unitId = tempList[i];
            unitCardUIs[i].SetUnitID(unitId);
            unitCardUIs[i].SetGridData(gridDatasForTesting.GridDatas[unitId]);
        }
    }

    public int DrawSkill()
    {
        return 1; // 수정 필요**
    }

    // 리롤 버튼 상호작용 상태 설정
    private void UpdateRerollBtn()
    {
        if (playerStageGold.Gold < rerollCost)
            reRollBtn.interactable = false;
        else
            reRollBtn.interactable = true;
    }

    // 리롤
    public void OnClickRerollBtn()
    {
        if (playerStageGold.UseGold(rerollCost))
        {
            DrawReward();
            UpdateRerollBtn();

            // 리롤 시 카드 드래그 다시 활성화
            for (int i = 0; i < unitCardUIs.Length; i++)
            {
                unitCardUIs[i].SetDragState(true);
                unitCardUIs[i].SetColor(Color.white);
            }
        }
    }

    // UnitCardUI 생성
    private void CreateUnitCardPrf(int amount)
    {
        unitCardUIs = new UnitCardUi[amount];

        for (int i = 0; i < amount; i++)
        {
            var card = Instantiate(unitCardUiPrf, transform);
            unitCardUIs[i] = card;
            card.gameObject.SetActive(false);

            // 각 카드의 드롭 성공 이벤트 구독
            card.OnUnitDropSuccess += OnUnitCardDropSuccess;
        }
    }

    // SkillCardUI 생성
    private void CreateSkillCardPrf(int amount)
    {
        skillCardUIs = new SkillCardUi[amount];

        for (int i = 0; i < amount; i++)
        {
            var card = Instantiate(skillCardPrf, transform);
            skillCardUIs[i] = card;
            card.gameObject.SetActive(false);
        }
    }

    // 일부 활성화
    private void SetActiveCards(BaseCardUi[] cardArray, bool value)
    {
        for (int i = 0; i < cardArray.Length; i++)
        {
            cardArray[i].gameObject.SetActive(value);
        }
    }

    // 유닛 카드의 드롭 성공 처리
    private void OnUnitCardDropSuccess()
    {
        for (int i = 0; i < unitCardUIs.Length; i++)
        {
            unitCardUIs[i].SetDragState(false);
            unitCardUIs[i].SetColor(new Color(0.267f, 0.267f, 0.267f));
        }
    }
}