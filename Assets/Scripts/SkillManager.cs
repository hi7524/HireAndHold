using UnityEngine;

public class SkillManager : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
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
        skillUi.AddSkill(selectedSkill, spawnPoint.position);
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
    private void TestUseSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skills.Length)
        {
            Debug.LogWarning($"[SkillManager] 잘못된 스킬 인덱스: {skillIndex}");
            return;
        }

        var skill = skills[skillIndex];
        if (skill == null)
        {
            Debug.LogWarning($"[SkillManager] 스킬 슬롯 {skillIndex}이(가) 비어있습니다!");
            return;
        }

        Vector3 usePosition = spawnPoint != null ? spawnPoint.position : transform.position;
        
        skill.TryUse(usePosition);
    }

    public void UseSkillByIndex(int skillIndex)
    {
        TestUseSkill(skillIndex);
    }
    private void OnDestroy()
    {
        skillSelectUi.OnSkillSelected -= HandleSkillSelected;
    }
   

}
