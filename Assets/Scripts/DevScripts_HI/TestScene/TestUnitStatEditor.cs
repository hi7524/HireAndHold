using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 배치된 유닛의 스탯을 실시간으로 수정하는 에디터
/// </summary>
public class TestUnitStatEditor : MonoBehaviour
{
    [Header("Unit Selection")]
    [SerializeField] private TMP_Dropdown unitDropdown;
    [SerializeField] private Button refreshButton;
    [SerializeField] private TMP_Text selectedUnitInfoText;
    
    [Header("Stat Inputs")]
    [SerializeField] private TMP_InputField attackDamageInput;
    [SerializeField] private TMP_InputField critRateInput;
    [SerializeField] private TMP_InputField critDamageInput;
    [SerializeField] private TMP_InputField attackCooltimeInput;
    
    [Header("Stat Modifiers")]
    [SerializeField] private TMP_InputField attackModifierInput;
    [SerializeField] private TMP_InputField critRateModifierInput;
    [SerializeField] private TMP_InputField critDamageModifierInput;
    
    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button addModifierButton;
    [SerializeField] private Button clearModifiersButton;
    
    private List<Unit> sceneUnits = new List<Unit>();
    private Unit selectedUnit;
    
    public void Initialize()
    {
        SetupUI();
        RefreshUnitList();
    }
    
    private void SetupUI()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshUnitList);
            
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyStatChanges);
            
        if (addModifierButton != null)
            addModifierButton.onClick.AddListener(AddStatModifiers);
            
        if (clearModifiersButton != null)
            clearModifiersButton.onClick.AddListener(ClearStatModifiers);
            
        if (unitDropdown != null)
            unitDropdown.onValueChanged.AddListener(OnUnitSelected);
    }
    
    public void RefreshUnitList()
    {
        sceneUnits.Clear();
        
        if (unitDropdown != null)
            unitDropdown.ClearOptions();
        
        // 씬에서 모든 Unit 찾기
        var allUnits = FindObjectsOfType<Unit>();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        
        foreach (var unit in allUnits)
        {
            if (unit != null && unit.gameObject.activeSelf)
            {
                sceneUnits.Add(unit);
                
                // 유닛 데이터 가져오기
                var unitData = DataTableManager.UnitTable.Get(unit.UnitID);
                string unitName = unitData != null ? unitData.StringName : "Unit " + unit.UnitID.ToString();
                options.Add(new TMP_Dropdown.OptionData(unit.UnitID.ToString() + ": " + unitName));
            }
        }
        
        if (unitDropdown != null)
            unitDropdown.AddOptions(options);
        
        // 첫 번째 유닛 선택
        if (sceneUnits.Count > 0)
        {
            selectedUnit = sceneUnits[0];
            UpdateStatDisplay();
        }
        else
        {
            selectedUnit = null;
            ClearStatDisplay();
        }
        
        Debug.Log($"[TestUnitStatEditor] {sceneUnits.Count}개 유닛 발견");
    }
    
    private void OnUnitSelected(int index)
    {
        if (index >= 0 && index < sceneUnits.Count)
        {
            selectedUnit = sceneUnits[index];
            UpdateStatDisplay();
        }
    }
    
    private void UpdateStatDisplay()
    {
        if (selectedUnit == null) return;
        
        // 유닛 정보 표시
        var unitData = DataTableManager.UnitTable.Get(selectedUnit.UnitID);
        if (selectedUnitInfoText != null && unitData != null)
        {
            selectedUnitInfoText.text = "유닛: " + unitData.StringName + "\n" +
                                       "ID: " + unitData.UNIT_ID.ToString() + "\n" +
                                       "등급: " + unitData.RANK.ToString() + "\n" +
                                       "레벨: " + unitData.LEVEL.ToString();
        }
        
        // 스탯 값 표시
        var attackStat = selectedUnit.GetAttackDamageStat();
        var critRateStat = selectedUnit.GetCriticalRateStat();
        var critDamageStat = selectedUnit.GetCriticalDamageStat();
        var attackCooltimeStat = selectedUnit.GetAttackCooltimeStat();
        
        if (attackDamageInput != null && attackStat != null)
            attackDamageInput.text = attackStat.Value.ToString("F1");
            
        if (critRateInput != null && critRateStat != null)
            critRateInput.text = critRateStat.Value.ToString("F1");
            
        if (critDamageInput != null && critDamageStat != null)
            critDamageInput.text = critDamageStat.Value.ToString("F2");
            
        if (attackCooltimeInput != null && attackCooltimeStat != null)
            attackCooltimeInput.text = attackCooltimeStat.Value.ToString("F2");
    }
    
    private void ClearStatDisplay()
    {
        if (selectedUnitInfoText != null)
            selectedUnitInfoText.text = "유닛 없음";
            
        if (attackDamageInput != null)
            attackDamageInput.text = "";
        if (critRateInput != null)
            critRateInput.text = "";
        if (critDamageInput != null)
            critDamageInput.text = "";
        if (attackCooltimeInput != null)
            attackCooltimeInput.text = "";
    }
    
    private void ApplyStatChanges()
    {
        if (selectedUnit == null)
        {
            Debug.LogWarning("[TestUnitStatEditor] 유닛을 선택해주세요!");
            return;
        }
        
        // 공격력 변경
        if (attackDamageInput != null && float.TryParse(attackDamageInput.text, out float newAttack))
        {
            var attackStat = selectedUnit.GetAttackDamageStat();
            if (attackStat != null)
            {
                attackStat.SetBaseValue(newAttack);
            }
        }
        
        // 치명타율 변경
        if (critRateInput != null && float.TryParse(critRateInput.text, out float newCritRate))
        {
            var critRateStat = selectedUnit.GetCriticalRateStat();
            if (critRateStat != null)
            {
                critRateStat.SetBaseValue(newCritRate);
            }
        }
        
        // 치명타 데미지 변경
        if (critDamageInput != null && float.TryParse(critDamageInput.text, out float newCritDamage))
        {
            var critDamageStat = selectedUnit.GetCriticalDamageStat();
            if (critDamageStat != null)
            {
                critDamageStat.SetBaseValue(newCritDamage);
            }
        }
        
        // 공격 쿨타임 변경
        if (attackCooltimeInput != null && float.TryParse(attackCooltimeInput.text, out float newCooltime))
        {
            var attackCooltimeStat = selectedUnit.GetAttackCooltimeStat();
            if (attackCooltimeStat != null)
            {
                attackCooltimeStat.SetBaseValue(newCooltime);
            }
        }
        
        Debug.Log("[TestUnitStatEditor] 스탯 변경 적용됨");
        UpdateStatDisplay();
    }
    
    private void AddStatModifiers()
    {
        if (selectedUnit == null)
        {
            Debug.LogWarning("[TestUnitStatEditor] 유닛을 선택해주세요!");
            return;
        }
        
        // 공격력 모디파이어 추가
        if (attackModifierInput != null && float.TryParse(attackModifierInput.text, out float attackMod))
        {
            var attackStat = selectedUnit.GetAttackDamageStat();
            if (attackStat != null && attackMod != 0)
            {
                attackStat.AddModifier(new StatModifier(attackMod, ModifierType.Flat));
            }
        }

        // 치명타율 모디파이어 추가
        if (critRateModifierInput != null && float.TryParse(critRateModifierInput.text, out float critRateMod))
        {
            var critRateStat = selectedUnit.GetCriticalRateStat();
            if (critRateStat != null && critRateMod != 0)
            {
                critRateStat.AddModifier(new StatModifier(critRateMod, ModifierType.Flat));
            }
        }

        // 치명타 데미지 모디파이어 추가
        if (critDamageModifierInput != null && float.TryParse(critDamageModifierInput.text, out float critDamageMod))
        {
            var critDamageStat = selectedUnit.GetCriticalDamageStat();
            if (critDamageStat != null && critDamageMod != 0)
            {
                critDamageStat.AddModifier(new StatModifier(critDamageMod, ModifierType.Flat));
            }
        }
        
        Debug.Log("[TestUnitStatEditor] 모디파이어 추가됨");
        UpdateStatDisplay();
    }
    
    private void ClearStatModifiers()
    {
        if (selectedUnit == null)
        {
            Debug.LogWarning("[TestUnitStatEditor] 유닛을 선택해주세요!");
            return;
        }

        // 모디파이어 제거는 현재 Stat 클래스에서 지원하지 않음
        // 대신 기본값으로 리셋
        Debug.LogWarning("[TestUnitStatEditor] 모디파이어 개별 제거는 지원되지 않습니다. 유닛을 재배치하면 모디파이어가 초기화됩니다.");
        UpdateStatDisplay();
    }
    
    // 유닛이 배치될 때 자동 갱신
    private void Update()
    {
        // 3초마다 유닛 리스트 자동 갱신 (옵션)
        // 필요시 주석 해제
        // if (Time.frameCount % 180 == 0)
        // {
        //     RefreshUnitList();
        // }
    }
}
