using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardSlot : MonoBehaviour
{
    [SerializeField] private Image skillIcon;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillDescText;
    [SerializeField] private GameObject starContainer; // 별 등급 표시용 (선택사항)
    
    private SkillData currentSkillData;

    public void SetSkillData(SkillData skillData)
    {
        currentSkillData = skillData;
        
        if (skillData != null)
        {
            if (skillNameText != null)
                skillNameText.text = skillData.SKILL_NAME;
            
            // TODO: 스킬 아이콘 로드
            // if (skillIcon != null)
            //     skillIcon.sprite = Resources.Load<Sprite>($"SkillIcons/{skillData.SKILL_ID}");
        }
    }

    public SkillData GetSkillData()
    {
        return currentSkillData;
    }
}
