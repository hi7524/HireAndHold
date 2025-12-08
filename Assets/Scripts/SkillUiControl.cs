using UnityEngine;

public class SkillUiControl: MonoBehaviour
{
    [SerializeField] private Transform skillSlotParent;
    [SerializeField] private SkillUI skillSlot;
    public void AddSkill(PlayerSkillBase skill, Vector3 spawnPosition)
    {
        skillSlot.Initialize(skill, spawnPosition);
    }
}
