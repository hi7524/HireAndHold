using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 패시브 스킬 레벨 조절 UI
/// 6개 패시브 스킬의 레벨(0~3성)을 조절
/// </summary>
public class TestPassiveSkillController : MonoBehaviour
{
    [Header("UI Container")]
    [SerializeField] private Transform passiveSkillContainer;
    [SerializeField] private GameObject passiveSkillItemPrefab;
    
    [Header("Manual UI References (Optional)")]
    [SerializeField] private PassiveSkillUIItem[] manualUIItems;
    
    [Header("Status Display")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button resetAllButton;
    
    private PassiveSkillManager passiveSkillManager;
    private Dictionary<PassiveSkillType, PassiveSkillUIItem> uiItems = new Dictionary<PassiveSkillType, PassiveSkillUIItem>();
    
    // 패시브 스킬 타입별 정보
    private readonly PassiveSkillInfo[] skillInfos = new PassiveSkillInfo[]
    {
        new PassiveSkillInfo(PassiveSkillType.Damage, "피해량 증가", 22070),
        new PassiveSkillInfo(PassiveSkillType.CritRate, "치명타 확률", 22071),
        new PassiveSkillInfo(PassiveSkillType.CritDamage, "치명타 피해량", 22072),
        new PassiveSkillInfo(PassiveSkillType.Exp, "경험치 증가", 22073),
        new PassiveSkillInfo(PassiveSkillType.ShieldRegen, "방벽 회복", 22074),
        new PassiveSkillInfo(PassiveSkillType.BossDamage, "보스 피해량", 22075),
    };
    
    public void Initialize(PassiveSkillManager manager)
    {
        passiveSkillManager = manager;
        
        if (passiveSkillManager != null)
        {
            passiveSkillManager.OnPassiveSkillChanged += UpdateStatusDisplay;
        }
        
        SetupUI();
        UpdateStatusDisplay();
    }
    
    private void SetupUI()
    {
        // 수동 UI 아이템이 있으면 사용
        if (manualUIItems != null && manualUIItems.Length > 0)
        {
            for (int i = 0; i < manualUIItems.Length && i < skillInfos.Length; i++)
            {
                var item = manualUIItems[i];
                var info = skillInfos[i];
                
                if (item != null)
                {
                    item.Setup(info.type, info.displayName, OnSkillLevelChanged);
                    uiItems[info.type] = item;
                }
            }
        }
        // 동적 생성
        else if (passiveSkillContainer != null && passiveSkillItemPrefab != null)
        {
            foreach (var info in skillInfos)
            {
                GameObject itemObj = Instantiate(passiveSkillItemPrefab, passiveSkillContainer);
                var item = itemObj.GetComponent<PassiveSkillUIItem>();
                
                if (item != null)
                {
                    item.Setup(info.type, info.displayName, OnSkillLevelChanged);
                    uiItems[info.type] = item;
                }
            }
        }
        
        // 리셋 버튼
        if (resetAllButton != null)
        {
            resetAllButton.onClick.AddListener(ResetAllSkills);
        }
    }
    
    private void OnSkillLevelChanged(PassiveSkillType type, int level)
    {
        if (passiveSkillManager == null) return;
        
        // 현재 레벨 가져오기
        int currentLevel = GetCurrentSkillLevel(type);
        
        // 레벨 조절
        if (level > currentLevel)
        {
            // 레벨업
            for (int i = currentLevel; i < level; i++)
            {
                int skillId = GetSkillIdByTypeAndStar(type, i + 1);
                passiveSkillManager.AddOrUpgradePassiveSkill(skillId);
            }
        }
        else if (level < currentLevel)
        {
            // 레벨 다운은 리셋 후 재적용
            passiveSkillManager.ResetAllPassiveSkills();
            
            // 다른 스킬들 복원
            foreach (var kvp in uiItems)
            {
                if (kvp.Key != type)
                {
                    int otherLevel = kvp.Value.CurrentLevel;
                    for (int i = 0; i < otherLevel; i++)
                    {
                        int skillId = GetSkillIdByTypeAndStar(kvp.Key, i + 1);
                        passiveSkillManager.AddOrUpgradePassiveSkill(skillId);
                    }
                }
            }
            
            // 현재 스킬 새 레벨로 적용
            for (int i = 0; i < level; i++)
            {
                int skillId = GetSkillIdByTypeAndStar(type, i + 1);
                passiveSkillManager.AddOrUpgradePassiveSkill(skillId);
            }
        }
        
        Debug.Log($"[TestPassiveSkill] {type} 레벨 변경: {level}");
    }
    
    private int GetCurrentSkillLevel(PassiveSkillType type)
    {
        if (passiveSkillManager == null) return 0;
        
        var ownedSkills = passiveSkillManager.GetOwnedPassiveSkills();
        foreach (var skill in ownedSkills)
        {
            if (skill.skillType == type)
            {
                return skill.currentStar;
            }
        }
        return 0;
    }
    
    private int GetSkillIdByTypeAndStar(PassiveSkillType type, int star)
    {
        // PassiveSkillType enum 값 기반으로 스킬 ID 계산
        // 22070 ~ 22087 범위
        int baseId = 22070 + ((int)type - 2206);
        return baseId + (star - 1) * 6;
    }
    
    private void ResetAllSkills()
    {
        if (passiveSkillManager != null)
        {
            passiveSkillManager.ResetAllPassiveSkills();
        }
        
        // UI 리셋
        foreach (var item in uiItems.Values)
        {
            item.SetLevel(0);
        }
        
        Debug.Log("[TestPassiveSkill] 모든 패시브 스킬 리셋됨");
    }
    
    private void UpdateStatusDisplay()
    {
        if (statusText == null || passiveSkillManager == null) return;
        
        var effects = passiveSkillManager.GetCurrentEffects();
        
        statusText.text = $"패시브 효과:\n" +
                         $"피해량: +{effects.damageBonus:F1}%\n" +
                         $"치명타율: +{effects.critRateBonus:F1}%\n" +
                         $"치명타뎀: +{effects.critDamageBonus:F1}%\n" +
                         $"경험치: +{effects.expBonus:F1}%\n" +
                         $"방벽회복: +{effects.shieldRegenBonus:F1}\n" +
                         $"보스뎀: +{effects.bossDamageBonus:F1}%";
    }
    
    private void OnDestroy()
    {
        if (passiveSkillManager != null)
        {
            passiveSkillManager.OnPassiveSkillChanged -= UpdateStatusDisplay;
        }
    }
    
    // 패시브 스킬 정보 구조체
    private struct PassiveSkillInfo
    {
        public PassiveSkillType type;
        public string displayName;
        public int baseSkillId;
        
        public PassiveSkillInfo(PassiveSkillType type, string displayName, int baseSkillId)
        {
            this.type = type;
            this.displayName = displayName;
            this.baseSkillId = baseSkillId;
        }
    }
}

/// <summary>
/// 개별 패시브 스킬 UI 아이템
/// </summary>
[System.Serializable]
public class PassiveSkillUIItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button decreaseButton;
    [SerializeField] private Button increaseButton;
    [SerializeField] private Slider levelSlider;
    
    public PassiveSkillType SkillType { get; private set; }
    public int CurrentLevel { get; private set; } = 0;
    
    private System.Action<PassiveSkillType, int> onLevelChanged;
    
    public void Setup(PassiveSkillType type, string displayName, System.Action<PassiveSkillType, int> callback)
    {
        SkillType = type;
        onLevelChanged = callback;
        
        if (nameText != null)
            nameText.text = displayName;
            
        if (levelSlider != null)
        {
            levelSlider.minValue = 0;
            levelSlider.maxValue = 3;
            levelSlider.wholeNumbers = true;
            levelSlider.value = 0;
            levelSlider.onValueChanged.AddListener(OnSliderChanged);
        }
        
        if (decreaseButton != null)
            decreaseButton.onClick.AddListener(() => ChangeLevel(-1));
            
        if (increaseButton != null)
            increaseButton.onClick.AddListener(() => ChangeLevel(1));
            
        UpdateDisplay();
    }
    
    private void OnSliderChanged(float value)
    {
        SetLevel(Mathf.RoundToInt(value));
    }
    
    private void ChangeLevel(int delta)
    {
        SetLevel(CurrentLevel + delta);
    }
    
    public void SetLevel(int level)
    {
        int newLevel = Mathf.Clamp(level, 0, 3);
        if (newLevel != CurrentLevel)
        {
            CurrentLevel = newLevel;
            UpdateDisplay();
            onLevelChanged?.Invoke(SkillType, CurrentLevel);
        }
    }
    
    private void UpdateDisplay()
    {
        if (levelText != null)
            levelText.text = CurrentLevel > 0 ? $"{CurrentLevel}성" : "없음";
            
        if (levelSlider != null)
            levelSlider.value = CurrentLevel;
    }
}
