using System.Collections.Generic;
using UnityEngine;

public class LevelUpRewardController : MonoBehaviour
{
    [SerializeField] private UnitCardUi unitCardUiPrf;
    [SerializeField] private SkillCardUi skillCardPrf;

    private UnitCardUi[] unitCardUIs;
    private SkillCardUi[] skillCardUIs;

    private List<int> ownedUnitIdForTesting = new List<int>{11101, 11104, 11107, 11110, 11113}; // 테스트용***


    private void Start()
    {
        CreateUnitCardPrf(3);
        CreateSkillCardPrf(3);

    }

    public void DrawReward(int curPlayerLv)
    {
        // 플레이어 레벨이 3의 배수일 때 유닛 뽑기
        if (curPlayerLv % 3 == 0)
        {
            
            SetActiveCards(unitCardUIs, true);
        }
        // 그 외에는 스킬 뽑기
        else
        {
            DrawSkill();
            SetActiveCards(skillCardUIs, true);
        }
    }

    public int DrawUnitID()
    {
        int randomIdx = Random.Range(0, ownedUnitIdForTesting.Count);
        return ownedUnitIdForTesting[randomIdx];
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

    // 전체 활성화 및 비활성화
    private void SetActiveCards(BaseCardUi[] cardArray, bool value)
    {
        for (int i = 0; i < cardArray.Length; i++)
        {
            cardArray[i].gameObject.SetActive(value);
        }
    }
}