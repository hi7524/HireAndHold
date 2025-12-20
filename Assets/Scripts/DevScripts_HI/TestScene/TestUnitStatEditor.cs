using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

/// <summary>
/// UnitTable 기반 유닛 스탯 편집기
/// 드롭다운에서 UnitTable의 유닛을 선택하고, DataTable 직접 수정 및 CSV 저장 가능
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

    [Header("DataTable/CSV Buttons")]
    [SerializeField] private Button applyToDataTableButton;  // DataTable 직접 수정 버튼
    [SerializeField] private Button resetDataTableButton;    // DataTable 원본 복구 버튼
    [SerializeField] private Button saveToCsvButton;         // CSV 파일 저장 버튼

    [Header("CSV Settings")]
    [SerializeField] private string csvFilePath = "Assets/DataTables/UnitTable.csv";

    [Header("Unit Slot Spawn")]
    [SerializeField] private Transform unitSlotContainer;    // 유닛 슬롯을 담을 컨테이너
    [SerializeField] private Button spawnUnitSlotButton;     // 유닛 슬롯 생성 버튼

    // 싱글톤
    public static TestUnitStatEditor Instance { get; private set; }

    // 이벤트: DataTable이 수정되었을 때 발생
    public event Action<int> OnDataTableModified;

    // UnitTable 데이터
    private List<UnitData> unitDataList = new List<UnitData>();
    private UnitData selectedUnitData;

    // 씬 유닛 목록 (기존 기능 유지)
    private List<Unit> sceneUnits = new List<Unit>();
    private Unit selectedUnit;

    // DataTable 원본 백업 (복구용)
    private Dictionary<int, UnitStatBackup> originalDataTableValues = new Dictionary<int, UnitStatBackup>();

    // 유닛 슬롯 관리
    private TestSlot currentTestSlot;

    [Serializable]
    public struct UnitStatBackup
    {
        public int attack;
        public float attackCooltime;
        public float attackCritical;
        public float criticalDamage;
        public int boltNum;
    }
    
    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void Initialize()
    {
        SetupDropdown();
        SetupUI();
    }

    private async void SetupDropdown()
    {
        // DataTable 초기화 대기
        while (!DataTableManager.IsInitialized)
        {
            await UniTask.Yield();
        }

        LoadUnitTable();
    }

    private void LoadUnitTable()
    {
        unitDataList.Clear();

        if (unitDropdown != null)
        {
            unitDropdown.ClearOptions();
        }

        var allUnits = DataTableManager.UnitTable.GetAll();
        var options = new List<TMP_Dropdown.OptionData>();

        foreach (var unit in allUnits)
        {
            unitDataList.Add(unit);
            // ID: 이름 (등급/레벨) 형식으로 표시
            string displayName = $"{unit.UNIT_ID}: {unit.StringName} (R{unit.RANK}/L{unit.LEVEL})";
            options.Add(new TMP_Dropdown.OptionData(displayName));
        }

        if (unitDropdown != null)
        {
            unitDropdown.AddOptions(options);
        }

        // 첫 번째 유닛 선택
        if (unitDataList.Count > 0)
        {
            OnUnitTableSelected(0);
        }
    }

    private void SetupUI()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshSceneUnits);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyStatChanges);

        if (addModifierButton != null)
            addModifierButton.onClick.AddListener(AddStatModifiers);

        if (clearModifiersButton != null)
            clearModifiersButton.onClick.AddListener(ClearStatModifiers);

        if (unitDropdown != null)
            unitDropdown.onValueChanged.AddListener(OnUnitTableSelected);

        // DataTable/CSV 버튼 설정
        if (applyToDataTableButton != null)
            applyToDataTableButton.onClick.AddListener(ApplyToDataTable);

        if (resetDataTableButton != null)
            resetDataTableButton.onClick.AddListener(ResetDataTableToOriginal);

        if (saveToCsvButton != null)
            saveToCsvButton.onClick.AddListener(SaveToCsv);

        // 유닛 슬롯 생성 버튼 설정
        if (spawnUnitSlotButton != null)
            spawnUnitSlotButton.onClick.AddListener(SpawnUnitSlot);
    }

    private void OnUnitTableSelected(int index)
    {
        if (index >= 0 && index < unitDataList.Count)
        {
            selectedUnitData = unitDataList[index];
            UpdateInfoText();
            UpdateInputFieldsFromDataTable();
        }
    }

    private void UpdateInfoText()
    {
        if (selectedUnitInfoText == null) return;

        if (selectedUnitData == null)
        {
            selectedUnitInfoText.text = "유닛을 선택하세요";
            return;
        }

        // UnitTable 기본값 표시
        string info = $"이름: {selectedUnitData.StringName}\n";
        info += $"ID: {selectedUnitData.UNIT_ID}\n";
        info += $"등급: {selectedUnitData.RANK} / 레벨: {selectedUnitData.LEVEL}\n";
        info += $"공격력: {selectedUnitData.ATTACK}\n";
        info += $"쿨타임: {selectedUnitData.ATTACK_COOLTIME:F2}\n";
        info += $"치명타율: {selectedUnitData.ATTACK_CRITICAL:F1}%\n";
        info += $"치명타뎀: {selectedUnitData.CRITICAL_DAMAGE:F2}";

        // DataTable이 수정되었으면 표시
        if (originalDataTableValues.ContainsKey(selectedUnitData.UNIT_ID))
        {
            info += "\n\n[DataTable 수정됨]";
        }

        // 씬에 스폰된 해당 유닛 수
        int sceneCount = CountSceneUnitsByID(selectedUnitData.UNIT_ID);
        if (sceneCount > 0)
        {
            info += $"\n\n[씬에 {sceneCount}개 존재]";
        }

        selectedUnitInfoText.text = info;
    }

    private void UpdateInputFieldsFromDataTable()
    {
        if (selectedUnitData == null) return;

        if (attackDamageInput != null)
            attackDamageInput.text = selectedUnitData.ATTACK.ToString();
        if (attackCooltimeInput != null)
            attackCooltimeInput.text = selectedUnitData.ATTACK_COOLTIME.ToString("F2");
        if (critRateInput != null)
            critRateInput.text = selectedUnitData.ATTACK_CRITICAL.ToString("F1");
        if (critDamageInput != null)
            critDamageInput.text = selectedUnitData.CRITICAL_DAMAGE.ToString("F2");
    }

    private int CountSceneUnitsByID(int unitId)
    {
        var allUnits = FindObjectsOfType<Unit>();
        int count = 0;

        foreach (var unit in allUnits)
        {
            if (unit != null && unit.UnitID == unitId)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 씬의 유닛 목록 새로고침 (기존 기능)
    /// </summary>
    public void RefreshSceneUnits()
    {
        sceneUnits.Clear();

        // 씬에서 모든 Unit 찾기
        var allUnits = FindObjectsOfType<Unit>();

        foreach (var unit in allUnits)
        {
            if (unit != null && unit.gameObject.activeSelf)
            {
                sceneUnits.Add(unit);
            }
        }

        // 현재 선택된 UnitData와 같은 ID를 가진 씬 유닛 찾기
        if (selectedUnitData != null)
        {
            selectedUnit = sceneUnits.Find(u => u.UnitID == selectedUnitData.UNIT_ID);
        }

        UpdateInfoText();
    }

    [Obsolete("Use OnUnitTableSelected instead")]
    public void RefreshUnitList()
    {
        RefreshSceneUnits();
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

    #region DataTable/CSV 기능

    /// <summary>
    /// 현재 입력된 스탯을 DataTable의 UnitData에 직접 적용
    /// (런타임 메모리상의 데이터만 변경, 파일은 변경되지 않음)
    /// </summary>
    public void ApplyToDataTable()
    {
        if (selectedUnitData == null)
        {
            Debug.LogWarning("[UnitStatEditor] 유닛을 선택해주세요!");
            return;
        }

        // 원본 백업 (아직 백업되지 않은 경우에만)
        if (!originalDataTableValues.ContainsKey(selectedUnitData.UNIT_ID))
        {
            originalDataTableValues[selectedUnitData.UNIT_ID] = new UnitStatBackup
            {
                attack = selectedUnitData.ATTACK,
                attackCooltime = selectedUnitData.ATTACK_COOLTIME,
                attackCritical = selectedUnitData.ATTACK_CRITICAL,
                criticalDamage = selectedUnitData.CRITICAL_DAMAGE,
                boltNum = selectedUnitData.BOLT_NUM
            };
        }

        // 입력값 파싱 및 적용
        if (attackDamageInput != null && int.TryParse(attackDamageInput.text, out int newAttack))
            selectedUnitData.ATTACK = newAttack;
        if (attackCooltimeInput != null && float.TryParse(attackCooltimeInput.text, out float newCooltime))
            selectedUnitData.ATTACK_COOLTIME = newCooltime;
        if (critRateInput != null && float.TryParse(critRateInput.text, out float newCritRate))
            selectedUnitData.ATTACK_CRITICAL = newCritRate;
        if (critDamageInput != null && float.TryParse(critDamageInput.text, out float newCritDamage))
            selectedUnitData.CRITICAL_DAMAGE = newCritDamage;

        // 이벤트 발생
        OnDataTableModified?.Invoke(selectedUnitData.UNIT_ID);

        UpdateInfoText();
    }

    /// <summary>
    /// 선택된 유닛의 DataTable 값을 원본으로 복구
    /// </summary>
    public void ResetDataTableToOriginal()
    {
        if (selectedUnitData == null)
        {
            Debug.LogWarning("[UnitStatEditor] 유닛을 선택해주세요!");
            return;
        }

        if (originalDataTableValues.TryGetValue(selectedUnitData.UNIT_ID, out var original))
        {
            selectedUnitData.ATTACK = original.attack;
            selectedUnitData.ATTACK_COOLTIME = original.attackCooltime;
            selectedUnitData.ATTACK_CRITICAL = original.attackCritical;
            selectedUnitData.CRITICAL_DAMAGE = original.criticalDamage;
            selectedUnitData.BOLT_NUM = original.boltNum;

            originalDataTableValues.Remove(selectedUnitData.UNIT_ID);

            UpdateInputFieldsFromDataTable();
            UpdateInfoText();
        }
        else
        {
            Debug.LogWarning($"[UnitStatEditor] {selectedUnitData.StringName}의 원본 데이터가 없습니다.");
        }
    }

    /// <summary>
    /// 모든 DataTable 변경사항을 원본으로 복구
    /// </summary>
    public void ResetAllDataTableToOriginal()
    {
        foreach (var kvp in originalDataTableValues)
        {
            var unitData = DataTableManager.UnitTable.Get(kvp.Key);
            if (unitData != null)
            {
                unitData.ATTACK = kvp.Value.attack;
                unitData.ATTACK_COOLTIME = kvp.Value.attackCooltime;
                unitData.ATTACK_CRITICAL = kvp.Value.attackCritical;
                unitData.CRITICAL_DAMAGE = kvp.Value.criticalDamage;
                unitData.BOLT_NUM = kvp.Value.boltNum;
            }
        }

        originalDataTableValues.Clear();
        UpdateInputFieldsFromDataTable();
        UpdateInfoText();
    }

    /// <summary>
    /// DataTable이 수정되었는지 확인
    /// </summary>
    public bool IsDataTableModified(int unitId)
    {
        return originalDataTableValues.ContainsKey(unitId);
    }

    /// <summary>
    /// 현재 UnitTable 데이터를 CSV 파일로 저장
    /// </summary>
    public void SaveToCsv()
    {
        try
        {
            string fullPath = Path.Combine(Application.dataPath, "..", csvFilePath);
            fullPath = Path.GetFullPath(fullPath);

            var sb = new StringBuilder();

            // CSV 헤더 (UnitTable.csv 구조에 맞춤)
            sb.AppendLine("UNIT_ID,NAME,RANK,LEVEL,ATTACK,ATTACK_COOLTIME,BOLT_NUM,ATTACK_CRITICAL,CRITICAL_DAMAGE,UNIT_SKILL1,UNIT_SKILL2,NORMAL_ENFORCEID,HERO_ENFORCEID,UNIT_ICON,UNIT_DESCRIPTION,PREFAB_NAME,GRID_DATA,PROJECTILE");

            // 모든 유닛 데이터 쓰기
            var allUnits = DataTableManager.UnitTable.GetAll();
            foreach (var unit in allUnits)
            {
                string line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}",
                    unit.UNIT_ID,
                    unit.NAME,
                    unit.RANK,
                    unit.LEVEL,
                    unit.ATTACK,
                    unit.ATTACK_COOLTIME,
                    unit.BOLT_NUM,
                    unit.ATTACK_CRITICAL,
                    unit.CRITICAL_DAMAGE,
                    unit.UNIT_SKILL1,
                    unit.UNIT_SKILL2,
                    unit.NORMAL_ENFORCEID,
                    0, // HERO_ENFORCEID (CSV에 있지만 UnitData에 없으면 0으로)
                    EscapeCsvField(unit.UNIT_ICON),
                    unit.UNIT_DESCRIPTION,
                    EscapeCsvField(unit.PREFAB_NAME),
                    EscapeCsvField(unit.GRID_DATA),
                    EscapeCsvField(unit.PROJECTILE));

                sb.AppendLine(line);
            }

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
#if UNITY_EDITOR
            // 에디터에서 에셋 새로고침
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnitStatEditor] CSV 저장 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// CSV 필드 이스케이프 처리 (콤마, 따옴표, 줄바꿈 포함 시)
    /// </summary>
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // 콤마, 따옴표, 줄바꿈이 포함되어 있으면 따옴표로 감싸기
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
        {
            // 따옴표는 두 개로 이스케이프
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }

        return field;
    }

    #endregion

    #region Unit Slot Spawn

    /// <summary>
    /// 선택된 유닛의 TestSlot을 생성하여 드래그 배치 가능하게 함
    /// TestUnitCreator.CreateUnitSlotAsync를 사용하여 Addressables로 스프라이트/그리드 로드
    /// </summary>
    public void SpawnUnitSlot()
    {
        if (selectedUnitData == null)
        {
            Debug.LogWarning("[UnitStatEditor] 유닛을 선택해주세요!");
            return;
        }

        if (unitSlotContainer == null)
        {
            Debug.LogError("[UnitStatEditor] 유닛 슬롯 컨테이너가 설정되지 않았습니다!");
            return;
        }

        // TestUnitCreator를 통해 슬롯 생성
        if (TestUnitCreator.Instance != null)
        {
            SpawnUnitSlotViaCreatorAsync().Forget();
        }
        else
        {
            Debug.LogError("[UnitStatEditor] TestUnitCreator.Instance가 없습니다! 씬에 TestUnitCreator가 필요합니다.");
        }
    }

    /// <summary>
    /// TestUnitCreator를 통해 비동기로 슬롯 생성
    /// </summary>
    private async UniTaskVoid SpawnUnitSlotViaCreatorAsync()
    {
        // 기존 슬롯이 있으면 제거
        if (currentTestSlot != null)
        {
            Destroy(currentTestSlot.gameObject);
            currentTestSlot = null;
        }

        // TestUnitCreator의 CreateUnitSlotAsync 사용
        currentTestSlot = await TestUnitCreator.Instance.CreateUnitSlotAsync(
            selectedUnitData.UNIT_ID,
            unitSlotContainer
        );

        if (currentTestSlot != null)
        {
            currentTestSlot.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 현재 생성된 유닛 슬롯 제거
    /// </summary>
    public void ClearUnitSlot()
    {
        if (currentTestSlot != null)
        {
            Destroy(currentTestSlot.gameObject);
            currentTestSlot = null;
        }
    }

    #endregion
}
