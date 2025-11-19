
using UnityEngine;
using UnityEngine.UI;

public class SkillCardUi : BaseCardUi
{
    public int count; // 테스트용 수정 필요 (PlayerData와 연결 필요)** 
    
    [SerializeField] private Image[] starIcons;
    [Space]
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color filledColor;


    private void Start()
    {
        // 초기 색상 설정
        for (int i = 0; i < count; i++)
        {
            SetIconColor(starIcons[i], filledColor);
        }
    }

    public void UpdateStarUI()
    {
        for (int i = 0; i < count; i++)
        {
            SetIconColor(starIcons[i], filledColor);
        }
    }

    public void SelectSkill()
    {
        Debug.Log("스킬 선택");
    }

    private void SetIconColor(Image img, Color color)
    {
        img.color = color;
    }
}