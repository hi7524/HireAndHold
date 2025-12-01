using UnityEngine;

public class SkillManager : MonoBehaviour
{
    [SerializeField] private SkillUiControl skillUi;
    [SerializeField] private PlayerSkillBase[] skills;
    [SerializeField] private SkillSelectUi skillSelectUi;

    private void Start()
    {
        skillUi.gameObject.SetActive(false);

        skillSelectUi.OnSkillSelected += HandleSkillSelected;
        
    }

    private void HandleSkillSelected(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skills.Length)
        {
            return;
        }

        var selectedSkill = skills[skillIndex];
        skillUi.gameObject.SetActive(true);
        skillUi.AddSkill(selectedSkill, new Vector3(0,3,0));
        Debug.Log($"[SkillManager] 스킬 선택됨:");
    }

    public int GetTotalSkillCount()
    {
        return skills.Length;
    }

    public int GetSkillID(int index)
    {
        if (index < 0 || index >= skills.Length)
        {
            Debug.LogWarning($"[SkillManager] 잘못된 스킬 인덱스: {index}");
            return -1;
        }
        return skills[index].SkillID;
    }
    
    private void OnDestroy()
    {
        skillSelectUi.OnSkillSelected -= HandleSkillSelected;
    }
   

}
