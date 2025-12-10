using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 테스트 씬 - MonsterTable 기반 몬스터 스탯 편집기
/// 드롭다운에서 MonsterTable의 몬스터를 선택하고, 씬에 스폰된 해당 몬스터들에 스탯 적용
/// </summary>
public class TestMonsterStatEditor : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown monsterDropdown;
    [SerializeField] private Button refreshButton;
    [SerializeField] private TMP_Text selectedMonsterInfoText;
    [SerializeField] private TMP_InputField hpInput;
    [SerializeField] private TMP_InputField speedInput;
    [SerializeField] private TMP_InputField expInput;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button applyToAllButton;
    [SerializeField] private Button applyToDataTableButton;  // DataTable 직접 수정 버튼
    [SerializeField] private Button resetDataTableButton;    // DataTable 원본 복구 버튼
    [SerializeField] private Button saveToCsvButton;         // CSV 파일 저장 버튼

    [Header("CSV Settings")]
    [SerializeField] private string csvFilePath = "Assets/DataTables/MonsterTable.csv";

    // 싱글톤 (SpawnController에서 접근용)
    public static TestMonsterStatEditor Instance { get; private set; }

    // 이벤트: 스탯이 변경되었을 때 발생 (monsterId, hp, speed, exp)
    public event Action<int, float, float, int> OnMonsterStatChanged;

    // 이벤트: DataTable이 수정되었을 때 발생
    public event Action<int> OnDataTableModified;

    // MonsterTable 데이터
    private List<MonsterData> monsterDataList = new List<MonsterData>();
    private MonsterData selectedMonsterData;

    // 커스텀 스탯 오버라이드 (MonsterID -> 수정된 스탯)
    private Dictionary<int, MonsterStatOverride> statOverrides = new Dictionary<int, MonsterStatOverride>();

    // DataTable 원본 백업 (복구용)
    private Dictionary<int, MonsterStatOverride> originalDataTableValues = new Dictionary<int, MonsterStatOverride>();

    [Serializable]
    public struct MonsterStatOverride
    {
        public float hp;
        public float speed;
        public int exp;
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

        LoadMonsterTable();
    }

    private void LoadMonsterTable()
    {
        monsterDataList.Clear();

        if (monsterDropdown != null)
        {
            monsterDropdown.ClearOptions();
        }

        var allMonsters = DataTableManager.MonsterTable.GetAll();
        var options = new List<TMP_Dropdown.OptionData>();

        foreach (var monster in allMonsters)
        {
            monsterDataList.Add(monster);
            // ID: 이름 형식으로 표시
            string displayName = $"{monster.MON_ID}: {monster.MON_NAME}";
            options.Add(new TMP_Dropdown.OptionData(displayName));
        }

        if (monsterDropdown != null)
        {
            monsterDropdown.AddOptions(options);
        }

        // 첫 번째 몬스터 선택
        if (monsterDataList.Count > 0)
        {
            OnMonsterSelected(0);
        }

        Debug.Log($"[MonsterStatEditor] MonsterTable 로드 완료: {monsterDataList.Count}개 몬스터");
    }

    private void SetupUI()
    {
        if (monsterDropdown != null)
        {
            monsterDropdown.onValueChanged.AddListener(OnMonsterSelected);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshSceneMonsters);
        }

        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplyToSceneMonsters);
        }

        if (applyToAllButton != null)
        {
            applyToAllButton.onClick.AddListener(ApplyToAllSceneMonsters);
        }

        if (applyToDataTableButton != null)
        {
            applyToDataTableButton.onClick.AddListener(ApplyToDataTable);
        }

        if (resetDataTableButton != null)
        {
            resetDataTableButton.onClick.AddListener(ResetDataTableToOriginal);
        }

        if (saveToCsvButton != null)
        {
            saveToCsvButton.onClick.AddListener(SaveToCsv);
        }
    }

    private void OnMonsterSelected(int index)
    {
        if (index >= 0 && index < monsterDataList.Count)
        {
            selectedMonsterData = monsterDataList[index];
            UpdateInfoText();
            UpdateInputFields();
            Debug.Log($"[MonsterStatEditor] 선택: {selectedMonsterData.MON_NAME} (ID: {selectedMonsterData.MON_ID})");
        }
    }

    private void UpdateInfoText()
    {
        if (selectedMonsterInfoText == null) return;

        if (selectedMonsterData == null)
        {
            selectedMonsterInfoText.text = "몬스터를 선택하세요";
            return;
        }

        // MonsterTable 기본값 표시
        string info = $"이름: {selectedMonsterData.MON_NAME}\n";
        info += $"ID: {selectedMonsterData.MON_ID}\n";
        info += $"HP: {selectedMonsterData.MON_HP:F0}\n";
        info += $"이동속도: {selectedMonsterData.MON_SPEED:F2}\n";
        info += $"경험치: {selectedMonsterData.MON_STAGE_EXP}";

        // 오버라이드가 있으면 표시
        if (statOverrides.TryGetValue(selectedMonsterData.MON_ID, out var overrideStats))
        {
            info += $"\n\n[수정된 값]";
            info += $"\nHP: {overrideStats.hp:F0}";
            info += $"\n속도: {overrideStats.speed:F2}";
            info += $"\n경험치: {overrideStats.exp}";
        }

        // 씬에 스폰된 해당 몬스터 수
        int sceneCount = CountSceneMonstersById(selectedMonsterData.MON_ID);
        if (sceneCount > 0)
        {
            info += $"\n\n[씬에 {sceneCount}마리 존재]";
        }

        selectedMonsterInfoText.text = info;
    }

    private void UpdateInputFields()
    {
        if (selectedMonsterData == null) return;

        // 오버라이드가 있으면 오버라이드 값, 없으면 테이블 기본값
        if (statOverrides.TryGetValue(selectedMonsterData.MON_ID, out var overrideStats))
        {
            if (hpInput != null)
                hpInput.text = overrideStats.hp.ToString("F0");
            if (speedInput != null)
                speedInput.text = overrideStats.speed.ToString("F2");
            if (expInput != null)
                expInput.text = overrideStats.exp.ToString();
        }
        else
        {
            if (hpInput != null)
                hpInput.text = selectedMonsterData.MON_HP.ToString("F0");
            if (speedInput != null)
                speedInput.text = selectedMonsterData.MON_SPEED.ToString();
            if (expInput != null)
                expInput.text = selectedMonsterData.MON_STAGE_EXP.ToString();
        }
    }

    /// <summary>
    /// 선택된 몬스터 타입에 해당하는 씬의 몬스터들에 스탯 적용
    /// </summary>
    private void ApplyToSceneMonsters()
    {
        if (selectedMonsterData == null)
        {
            Debug.LogWarning("[MonsterStatEditor] 몬스터를 선택해주세요!");
            return;
        }

        // 입력값 파싱
        float newHp = selectedMonsterData.MON_HP;
        float newSpeed = selectedMonsterData.MON_SPEED;
        int newExp = selectedMonsterData.MON_STAGE_EXP;

        if (hpInput != null && float.TryParse(hpInput.text, out float parsedHp))
            newHp = parsedHp;
        if (speedInput != null && float.TryParse(speedInput.text, out float parsedSpeed))
            newSpeed = parsedSpeed;
        if (expInput != null && int.TryParse(expInput.text, out int parsedExp))
            newExp = parsedExp;

        // 오버라이드 저장
        statOverrides[selectedMonsterData.MON_ID] = new MonsterStatOverride
        {
            hp = newHp,
            speed = newSpeed,
            exp = newExp
        };

        // 씬에 있는 해당 ID의 몬스터들에 적용
        int appliedCount = ApplyStatsToSceneMonstersByID(selectedMonsterData.MON_ID, newHp, newSpeed, newExp);

        // 이벤트 발생
        OnMonsterStatChanged?.Invoke(selectedMonsterData.MON_ID, newHp, newSpeed, newExp);

        UpdateInfoText();
        Debug.Log($"[MonsterStatEditor] {selectedMonsterData.MON_NAME}에 스탯 적용 완료 (씬: {appliedCount}마리)");
    }

    /// <summary>
    /// 씬의 모든 몬스터에 현재 저장된 오버라이드 스탯 적용
    /// </summary>
    private void ApplyToAllSceneMonsters()
    {
        var allEnemies = FindObjectsOfType<Enemy>();
        int appliedCount = 0;

        foreach (var enemy in allEnemies)
        {
            int monsterId = GetMonsterIdFromEnemy(enemy);

            if (statOverrides.TryGetValue(monsterId, out var overrideStats))
            {
                ApplyStatsToEnemy(enemy, overrideStats.hp, overrideStats.speed, overrideStats.exp);
                appliedCount++;
            }
        }

        Debug.Log($"[MonsterStatEditor] 모든 몬스터에 오버라이드 적용 완료 ({appliedCount}마리)");
    }

    /// <summary>
    /// 씬에 새로 스폰된 몬스터들에 오버라이드 적용 (새로고침)
    /// </summary>
    public void RefreshSceneMonsters()
    {
        var allEnemies = FindObjectsOfType<Enemy>();
        int appliedCount = 0;

        foreach (var enemy in allEnemies)
        {
            int monsterId = GetMonsterIdFromEnemy(enemy);

            if (statOverrides.TryGetValue(monsterId, out var overrideStats))
            {
                ApplyStatsToEnemy(enemy, overrideStats.hp, overrideStats.speed, overrideStats.exp);
                appliedCount++;
            }
        }

        UpdateInfoText();
        Debug.Log($"[MonsterStatEditor] 씬 몬스터 갱신 완료 ({appliedCount}마리에 오버라이드 적용)");
    }

    private int ApplyStatsToSceneMonstersByID(int monsterId, float hp, float speed, int exp)
    {
        var allEnemies = FindObjectsOfType<Enemy>();
        int count = 0;

        foreach (var enemy in allEnemies)
        {
            if (GetMonsterIdFromEnemy(enemy) == monsterId)
            {
                ApplyStatsToEnemy(enemy, hp, speed, exp);
                count++;
            }
        }

        return count;
    }

    private void ApplyStatsToEnemy(Enemy enemy, float hp, float speed, int exp)
    {
        if (enemy == null) return;

        var bindingFlags = System.Reflection.BindingFlags.NonPublic |
                           System.Reflection.BindingFlags.Public |
                           System.Reflection.BindingFlags.Instance;

        // HP 적용
        var maxHpField = typeof(Enemy).GetField("maxHp", bindingFlags);
        if (maxHpField != null)
        {
            maxHpField.SetValue(enemy, hp);
        }

        var currentHpField = typeof(Enemy).GetField("currentHp", bindingFlags);
        if (currentHpField != null)
        {
            currentHpField.SetValue(enemy, hp);
        }

        // 이동속도 적용
        var speedField = typeof(Enemy).GetField("speed", bindingFlags);
        if (speedField != null)
        {
            speedField.SetValue(enemy, speed);
        }

        // 경험치 적용
        var expField = typeof(Enemy).GetField("baseStageExp", bindingFlags);
        if (expField != null)
        {
            expField.SetValue(enemy, exp);
        }
    }

    private int GetMonsterIdFromEnemy(Enemy enemy)
    {
        if (enemy == null) return -1;

        var bindingFlags = System.Reflection.BindingFlags.NonPublic |
                           System.Reflection.BindingFlags.Public |
                           System.Reflection.BindingFlags.Instance;

        // monsterData 필드에서 MON_ID 가져오기
        var dataField = typeof(Enemy).GetField("monsterData", bindingFlags);
        if (dataField != null)
        {
            var data = dataField.GetValue(enemy) as MonsterData;
            if (data != null)
            {
                return data.MON_ID;
            }
        }

        return -1;
    }

    private int CountSceneMonstersById(int monsterId)
    {
        var allEnemies = FindObjectsOfType<Enemy>();
        int count = 0;

        foreach (var enemy in allEnemies)
        {
            if (GetMonsterIdFromEnemy(enemy) == monsterId)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 오버라이드 초기화 (테이블 기본값으로 복원)
    /// </summary>
    public void ClearOverride()
    {
        if (selectedMonsterData != null)
        {
            statOverrides.Remove(selectedMonsterData.MON_ID);
            UpdateInputFields();
            UpdateInfoText();
            Debug.Log($"[MonsterStatEditor] {selectedMonsterData.MON_NAME} 오버라이드 초기화됨");
        }
    }

    /// <summary>
    /// 모든 오버라이드 초기화
    /// </summary>
    public void ClearAllOverrides()
    {
        statOverrides.Clear();
        UpdateInputFields();
        UpdateInfoText();
        Debug.Log("[MonsterStatEditor] 모든 오버라이드 초기화됨");
    }

    /// <summary>
    /// 외부에서 특정 몬스터 ID의 오버라이드 스탯 가져오기
    /// MonsterSpawnController에서 스폰 시 사용 가능
    /// </summary>
    public bool TryGetOverrideStats(int monsterId, out float hp, out float speed, out int exp)
    {
        if (statOverrides.TryGetValue(monsterId, out var overrideStats))
        {
            hp = overrideStats.hp;
            speed = overrideStats.speed;
            exp = overrideStats.exp;
            return true;
        }

        hp = 0;
        speed = 0;
        exp = 0;
        return false;
    }

    /// <summary>
    /// 오버라이드가 있는지 확인
    /// </summary>
    public bool HasOverride(int monsterId)
    {
        return statOverrides.ContainsKey(monsterId);
    }

    /// <summary>
    /// 현재 입력된 스탯을 DataTable의 MonsterData에 직접 적용
    /// (런타임 메모리상의 데이터만 변경, 파일은 변경되지 않음)
    /// </summary>
    public void ApplyToDataTable()
    {
        if (selectedMonsterData == null)
        {
            Debug.LogWarning("[MonsterStatEditor] 몬스터를 선택해주세요!");
            return;
        }

        // 원본 백업 (아직 백업되지 않은 경우에만)
        if (!originalDataTableValues.ContainsKey(selectedMonsterData.MON_ID))
        {
            originalDataTableValues[selectedMonsterData.MON_ID] = new MonsterStatOverride
            {
                hp = selectedMonsterData.MON_HP,
                speed = selectedMonsterData.MON_SPEED,
                exp = selectedMonsterData.MON_STAGE_EXP
            };
        }

        // 입력값 파싱
        if (hpInput != null && int.TryParse(hpInput.text, out int newHp))
            selectedMonsterData.MON_HP = newHp;
        if (speedInput != null && int.TryParse(speedInput.text, out int newSpeed))
            selectedMonsterData.MON_SPEED = newSpeed;
        if (expInput != null && int.TryParse(expInput.text, out int newExp))
            selectedMonsterData.MON_STAGE_EXP = newExp;

        // 이벤트 발생
        OnDataTableModified?.Invoke(selectedMonsterData.MON_ID);

        UpdateInfoText();
        Debug.Log($"[MonsterStatEditor] DataTable 수정 완료: {selectedMonsterData.MON_NAME} (HP:{selectedMonsterData.MON_HP}, Speed:{selectedMonsterData.MON_SPEED}, Exp:{selectedMonsterData.MON_STAGE_EXP})");
    }

    /// <summary>
    /// 선택된 몬스터의 DataTable 값을 원본으로 복구
    /// </summary>
    public void ResetDataTableToOriginal()
    {
        if (selectedMonsterData == null)
        {
            Debug.LogWarning("[MonsterStatEditor] 몬스터를 선택해주세요!");
            return;
        }

        if (originalDataTableValues.TryGetValue(selectedMonsterData.MON_ID, out var original))
        {
            selectedMonsterData.MON_HP = (int)original.hp;
            selectedMonsterData.MON_SPEED = (int)original.speed;
            selectedMonsterData.MON_STAGE_EXP = original.exp;

            originalDataTableValues.Remove(selectedMonsterData.MON_ID);

            UpdateInputFields();
            UpdateInfoText();
            Debug.Log($"[MonsterStatEditor] DataTable 원본 복구: {selectedMonsterData.MON_NAME}");
        }
        else
        {
            Debug.LogWarning($"[MonsterStatEditor] {selectedMonsterData.MON_NAME}의 원본 데이터가 없습니다.");
        }
    }

    /// <summary>
    /// 모든 DataTable 변경사항을 원본으로 복구
    /// </summary>
    public void ResetAllDataTableToOriginal()
    {
        foreach (var kvp in originalDataTableValues)
        {
            var monsterData = DataTableManager.MonsterTable.Get(kvp.Key);
            if (monsterData != null)
            {
                monsterData.MON_HP = (int)kvp.Value.hp;
                monsterData.MON_SPEED = (int)kvp.Value.speed;
                monsterData.MON_STAGE_EXP = kvp.Value.exp;
            }
        }

        originalDataTableValues.Clear();
        UpdateInputFields();
        UpdateInfoText();
        Debug.Log("[MonsterStatEditor] 모든 DataTable 원본 복구 완료");
    }

    /// <summary>
    /// DataTable이 수정되었는지 확인
    /// </summary>
    public bool IsDataTableModified(int monsterId)
    {
        return originalDataTableValues.ContainsKey(monsterId);
    }

    /// <summary>
    /// 현재 MonsterTable 데이터를 CSV 파일로 저장
    /// </summary>
    public void SaveToCsv()
    {
        try
        {
            string fullPath = Path.Combine(Application.dataPath, "..", csvFilePath);
            fullPath = Path.GetFullPath(fullPath);

            var sb = new StringBuilder();

            // CSV 헤더
            sb.AppendLine("MON_ID,MON_NAME,MON_TYPE,MON_ATK,MON_HP,MON_DEF,MON_RANGE,MON_ATK_SPD,MON_SPEED,MON_DROP_GOLD,MON_ACCOUNT_EXP,MON_STAGE_EXP,DROP_ITEM1_ID,DROP_ITEM1_COUNT,DROP_ITEM1_RATE,DROP_ITEM2_ID,DROP_ITEM2_COUNT,DROP_ITEM2_RATE,MON_MODEL");

            // 모든 몬스터 데이터 쓰기
            var allMonsters = DataTableManager.MonsterTable.GetAll();
            foreach (var monster in allMonsters)
            {
                string line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}",
                    monster.MON_ID,
                    EscapeCsvField(monster.MON_NAME),
                    monster.MON_TYPE,
                    monster.MON_ATK,
                    monster.MON_HP,
                    monster.MON_DEF,
                    monster.MON_RANGE,
                    monster.MON_ATK_SPD,
                    monster.MON_SPEED,
                    monster.MON_DROP_GOLD,
                    monster.MON_ACCOUNT_EXP,
                    monster.MON_STAGE_EXP,
                    monster.DROP_ITEM1_ID,
                    monster.DROP_ITEM1_COUNT,
                    monster.DROP_ITEM1_RATE,
                    monster.DROP_ITEM2_ID,
                    monster.DROP_ITEM2_COUNT,
                    monster.DROP_ITEM2_RATE,
                    EscapeCsvField(monster.MON_MODEL));

                sb.AppendLine(line);
            }

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[MonsterStatEditor] CSV 저장 완료: {fullPath}");

#if UNITY_EDITOR
            // 에디터에서 에셋 새로고침
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MonsterStatEditor] CSV 저장 실패: {ex.Message}");
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
}
