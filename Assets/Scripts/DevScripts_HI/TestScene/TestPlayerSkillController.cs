using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

/// <summary>
/// 플레이어 스킬 테스트 컨트롤러
/// 스킬 선택, 사용, 쿨다운 조절 기능
/// </summary>
public class TestPlayerSkillController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown skillDropdown;
    [SerializeField] private Button useSkillButton;
    [SerializeField] private Toggle noCooldownToggle;
    [SerializeField] private Slider cooldownMultiplierSlider;
    [SerializeField] private TMP_Text cooldownMultiplierText;
    
    [Header("Skill Settings")]
    [SerializeField] private TMP_InputField damageInput;
    [SerializeField] private TMP_InputField cooldownInput;
    [SerializeField] private Button applySettingsButton;
    
    [Header("Skill Spawn Point")]
    [SerializeField] private Transform skillSpawnPoint;
    
    [Header("Available Skills")]
    [SerializeField] private PlayerSkillBase[] availableSkills;
    
    private PlayerSkillBase selectedSkill;
    private int selectedSkillIndex = -1;
    private float cooldownMultiplier = 1f;
    private bool noCooldown = false;
    
    public void Initialize()
    {
        SetupDropdown();
        SetupButtons();
        SetupSliders();
    }
    
    private async void SetupDropdown()
    {
        // DataTable 로드 대기
        while (!DataTableManager.IsInitialized)
        {
            await UniTask.Yield();
        }
        
        if (skillDropdown == null) return;
        
        skillDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        
        // 씬에 있는 스킬들 사용
        if (availableSkills != null && availableSkills.Length > 0)
        {
            for (int i = 0; i < availableSkills.Length; i++)
            {
                var skill = availableSkills[i];
                if (skill != null)
                {
                    // 스킬 데이터 테이블에서 이름 가져오기 (StringTable 사용)
                    var skillData = DataTableManager.SkillTable.Get(skill.SkillID);
                    string skillName = $"Skill {skill.SkillID}";
                    if (skillData != null && int.TryParse(skillData.SKILL_NAME, out int nameId))
                        skillName = DataTableManager.StringTable.Get(nameId);
                    options.Add(new TMP_Dropdown.OptionData($"{skill.SkillID}: {skillName}"));
                }
            }
        }
        else
        {
            // 씬에서 PlayerSkillBase 찾기
            availableSkills = FindObjectsOfType<PlayerSkillBase>();
            foreach (var skill in availableSkills)
            {
                var skillData = DataTableManager.SkillTable.Get(skill.SkillID);
                string skillName = $"Skill {skill.SkillID}";
                if (skillData != null && int.TryParse(skillData.SKILL_NAME, out int nameId))
                    skillName = DataTableManager.StringTable.Get(nameId);
                options.Add(new TMP_Dropdown.OptionData($"{skill.SkillID}: {skillName}"));
            }
        }
        
        skillDropdown.AddOptions(options);
        skillDropdown.onValueChanged.AddListener(OnSkillSelected);
        
        // 첫 번째 스킬 선택
        if (availableSkills.Length > 0)
        {
            selectedSkillIndex = 0;
            selectedSkill = availableSkills[0];
            UpdateSkillSettingsUI();
        }
        
        Debug.Log($"[TestPlayerSkill] {availableSkills.Length}개 스킬 로드 완료");
    }
    
    private void SetupButtons()
    {
        if (useSkillButton != null)
        {
            useSkillButton.onClick.AddListener(UseSelectedSkill);
        }
        
        if (applySettingsButton != null)
        {
            applySettingsButton.onClick.AddListener(ApplySkillSettings);
        }
        
        if (noCooldownToggle != null)
        {
            noCooldownToggle.onValueChanged.AddListener(OnNoCooldownChanged);
        }
    }
    
    private void SetupSliders()
    {
        if (cooldownMultiplierSlider != null)
        {
            cooldownMultiplierSlider.minValue = 0.1f;
            cooldownMultiplierSlider.maxValue = 2f;
            cooldownMultiplierSlider.value = 1f;
            cooldownMultiplierSlider.onValueChanged.AddListener(OnCooldownMultiplierChanged);
            UpdateCooldownMultiplierText();
        }
    }
    
    private void OnSkillSelected(int index)
    {
        if (index >= 0 && index < availableSkills.Length)
        {
            selectedSkillIndex = index;
            selectedSkill = availableSkills[index];
            UpdateSkillSettingsUI();
            
            Debug.Log($"[TestPlayerSkill] 선택된 스킬: {selectedSkill.SkillID}");
        }
    }
    
    private void UpdateSkillSettingsUI()
    {
        if (selectedSkill == null) return;
        
        if (damageInput != null)
        {
            // 리플렉션으로 damage 값 가져오기 (protected 필드)
            var damageField = typeof(PlayerSkillBase).GetField("damage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                float damage = (float)damageField.GetValue(selectedSkill);
                damageInput.text = damage.ToString("F1");
            }
        }
        
        if (cooldownInput != null)
        {
            cooldownInput.text = selectedSkill.CoolDown.ToString("F1");
        }
    }
    
    private void UseSelectedSkill()
    {
        if (selectedSkill == null)
        {
            Debug.LogWarning("[TestPlayerSkill] 스킬을 선택해주세요!");
            return;
        }
        
        // 쿨다운 무시 옵션
        if (noCooldown)
        {
            // 쿨다운 강제 리셋
            selectedSkill.isOnCoolTime = false;
            selectedSkill.elapsed = selectedSkill.CoolDown;
        }
        
        // 스킬 사용
        Vector3 spawnPoint = skillSpawnPoint != null ? skillSpawnPoint.position : new Vector3(0, 3, 0);
        selectedSkill.TryUse(spawnPoint);
        
        Debug.Log($"[TestPlayerSkill] 스킬 사용: {selectedSkill.SkillID}");
    }
    
    private void ApplySkillSettings()
    {
        if (selectedSkill == null) return;
        
        // 데미지 설정
        if (damageInput != null && float.TryParse(damageInput.text, out float newDamage))
        {
            var damageField = typeof(PlayerSkillBase).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                damageField.SetValue(selectedSkill, newDamage);
            }
        }
        
        // 쿨다운 설정
        if (cooldownInput != null && float.TryParse(cooldownInput.text, out float newCooldown))
        {
            var cooldownField = typeof(PlayerSkillBase).GetField("cooldown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cooldownField != null)
            {
                cooldownField.SetValue(selectedSkill, newCooldown * cooldownMultiplier);
            }
        }
        
        Debug.Log("[TestPlayerSkill] 스킬 설정 적용됨");
    }
    
    private void OnNoCooldownChanged(bool value)
    {
        noCooldown = value;
        Debug.Log($"[TestPlayerSkill] 쿨다운 무시: {noCooldown}");
    }
    
    private void OnCooldownMultiplierChanged(float value)
    {
        cooldownMultiplier = value;
        UpdateCooldownMultiplierText();
    }
    
    private void UpdateCooldownMultiplierText()
    {
        if (cooldownMultiplierText != null)
        {
            cooldownMultiplierText.text = $"쿨다운 배율: x{cooldownMultiplier:F1}";
        }
    }
    
    // 외부에서 스킬 직접 사용
    public void UseSkillByIndex(int index)
    {
        if (index >= 0 && index < availableSkills.Length)
        {
            Vector3 spawnPoint = skillSpawnPoint != null ? skillSpawnPoint.position : new Vector3(0, 3, 0);
            
            if (noCooldown)
            {
                availableSkills[index].isOnCoolTime = false;
            }
            
            availableSkills[index].TryUse(spawnPoint);
        }
    }
    
    // 모든 스킬 쿨다운 리셋
    public void ResetAllCooldowns()
    {
        foreach (var skill in availableSkills)
        {
            if (skill != null)
            {
                skill.isOnCoolTime = false;
                skill.elapsed = skill.CoolDown;
            }
        }
        
        Debug.Log("[TestPlayerSkill] 모든 스킬 쿨다운 리셋됨");
    }
}
