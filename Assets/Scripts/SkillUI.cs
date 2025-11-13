using UnityEngine;
using UnityEngine.UI;

public class SkillUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownMask;
    [SerializeField] private Button button;

    private PlayerSkillBase skill;
    private Vector3 spawnPosition;

    public void Initialize(PlayerSkillBase skill, Vector3 spawnPosition)
    {
        this.skill = skill;
        this.spawnPosition = spawnPosition;

        button.onClick.AddListener(OnClick);

        skill.OnCooldownProgress += UpdateCooldown;
        skill.OnCooldownEnd += ResetCooldown;
    }

    private void OnClick()
    {
        skill.TryUse(spawnPosition);
    }

    private void UpdateCooldown(float progress)
    {
        cooldownMask.fillAmount = 1f - progress;
    }

    private void ResetCooldown()
    {
        cooldownMask.fillAmount = 0f;
    }

    private void OnDestroy()
    {
        if (skill != null)
        {
            skill.OnCooldownProgress -= UpdateCooldown;
            skill.OnCooldownEnd -= ResetCooldown;
        }
    }
}
