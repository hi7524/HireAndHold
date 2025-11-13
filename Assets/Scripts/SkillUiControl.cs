using UnityEngine;

public class SkillUiControl: MonoBehaviour
{
    [SerializeField] private Transform skillSlotParent;
    [SerializeField] private SkillUI skillSlotPrefab;
    public void AddSkill(PlayerSkillBase skill, Vector3 spawnPosition)
    {
        var slot = Instantiate(skillSlotPrefab, skillSlotParent);
        slot.Initialize(skill, spawnPosition);
    }
}
