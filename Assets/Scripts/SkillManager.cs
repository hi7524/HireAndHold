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
     /// <summary>
    /// 테스트용 스킬 직접 발동
    /// </summary>
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
        
        // Debug.Log($"[SkillManager] 테스트: {skill.GetType().Name} 발동! (키: {testKeys[skillIndex]})");
        skill.TryUse(usePosition);
    }

    /// <summary>
    /// 외부에서 스킬을 직접 발동할 수 있는 public 메서드
    /// </summary>
    public void UseSkillByIndex(int skillIndex)
    {
        TestUseSkill(skillIndex);
    }
    private void OnDestroy()
    {
        skillSelectUi.OnSkillSelected -= HandleSkillSelected;
    }
   

}
