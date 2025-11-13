using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] private PlayerExperience playerExp;
    [SerializeField] private CardUI[] cardUis;

    private bool[] types; // true: 유닛, false: 패시브 스킬


    private void Start()
    {
        playerExp.OnLevelUp += HandleLevelUp;

        types = new bool[cardUis.Length];
    }

    public void HandleLevelUp()
    {
        // 5레벨 까지는 유닛만 나오도록 함
        if (playerExp.Level <= 5)
        {
            for (int i = 0; i < cardUis.Length; i++)
            {
                types[i] = true;
            }
        }
        // 아닐때는 그냥 뽑기, 하지만 같은거 3종류로 뽑히지 않도록
        else
        {
            ShuffleCardTypes();
        }

        UpdateCardUIs();
    }

    // 랜덤으로 유닛을 뽑을지 패시브 스킬을 뽑을지 결정
    private void ShuffleCardTypes()
    {
        int halfLength = cardUis.Length / 2;
        for (int i = 0; i < cardUis.Length; i++)
        {
            types[i] = i < halfLength;
        }

        for (int i = types.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            bool temp = types[i];
            types[i] = types[randomIndex];
            types[randomIndex] = temp;
        }
    }

    // 뽑힌 정보에 따라 카드 UI 업데이트
    private void UpdateCardUIs()
    {
        for (int i = 0; i < cardUis.Length; i++)
        {
            if (types[i])
            {
                cardUis[i].SetTitleText($"랜덤 유닛 {SelectRandomUnit()}");
            }
            else
            {
                cardUis[i].SetTitleText($"랜덤 패시브 스킬 {SelectRandomPassive()}");
            }
        }
    }

    // 유닛 랜덤 뽑기
    private int SelectRandomUnit()
    {
        // 이후 현재 소지중인 유닛중에서 뽑을 수 있도록 수정하기 **
        return 1;
    }

    // 패시브 스킬 랜덤뽑기
    private int SelectRandomPassive()
    {
        // 이후 현재 소지중인 패시브 스킬중에서 뽑을 수 있도록 수정하기 **
        return 1;
    }

    private void OnDestroy()
    {
        playerExp.OnLevelUp -= ShuffleCardTypes;
    }
}