using System.Collections.Generic;
using UnityEngine;

public class LevelUpRewardController : MonoBehaviour
{
    [SerializeField] private UnitCardUi unitCardUiPrf;
    [SerializeField] private SkillCardUi skillCardPrf;
    [Space]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerExperience playerExp;
    [SerializeField] private UnitInventory inventory;
    public GridDatasForTesting gridDatasForTesting;

    private UnitCardUi[] unitCardUIs;
    private SkillCardUi[] skillCardUIs;

    private List<int> ownedUnitIdForTesting = new List<int> { 11101, 11104, 11107, 11110, 11113 }; // 테스트용***


    private void Start()
    {
        CreateUnitCardPrf(3);
        CreateSkillCardPrf(3);

        playerExp.OnLevelUp += DrawReward;

        SelectUnitOnGameStart();
    }

    private void OnDestroy()
    {
        playerExp.OnLevelUp -= DrawReward;
    }

    public void SelectUnitOnGameStart()
    {
        DrawUnitID();
        SetActiveCards(unitCardUIs, true);
        inventory.gameObject.SetActive(true);
        gameManager.PauseGame();
    }

    public void DrawReward()
    {
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

        inventory.gameObject.SetActive(true);

        gameManager.PauseGame();
    }

    public void OnClickConfirmBtn()
    {
        SetActiveCards(skillCardUIs, false);
        SetActiveCards(unitCardUIs, false);

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

    // UnitCardUI 생성
    private void CreateUnitCardPrf(int amount)
    {
        unitCardUIs = new UnitCardUi[amount];

        for (int i = 0; i < amount; i++)
        {
            var card = Instantiate(unitCardUiPrf, transform);
            unitCardUIs[i] = card;
            card.gameObject.SetActive(false);
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
}