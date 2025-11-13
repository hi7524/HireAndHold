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
}
