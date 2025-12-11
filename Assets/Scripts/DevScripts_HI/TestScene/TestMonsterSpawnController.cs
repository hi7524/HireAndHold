using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using TMPro;
using Cysharp.Threading.Tasks;

/// <summary>
/// 몬스터/보스 스폰을 위한 드롭다운 UI 및 스폰 로직
/// </summary>
public class TestMonsterSpawnController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown monsterDropdown;
    [SerializeField] private Toggle bossToggle;
    [SerializeField] private Button spawnButton;
    [SerializeField] private Button killAllButton;
    [SerializeField] private TMP_InputField spawnCountInput;

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float horizontalRange = 2f;

    private ObjectPoolManager poolManager;
    private List<MonsterData> monsterList = new List<MonsterData>();
    private int selectedMonsterId = -1;

    // 웨이브 설정과 연동
    private float hpMultiplier = 1f;
    private float expMultiplier = 1f;

    // 캐시된 프리팹
    private GameObject cachedMonsterPrefab;
    private List<GameObject> spawnedMonsters = new List<GameObject>();

    // MonsterStatEditor 연동
    private TestMonsterStatEditor monsterStatEditor;

    // FloatingText 연동
    private FloatingTextSpawner floatingTextSpawner;

    public void Initialize(ObjectPoolManager pool)
    {
        poolManager = pool;
        LoadMonsterPrefab();
        SetupDropdown();
        SetupButtons();

        // MonsterStatEditor 연결
        monsterStatEditor = TestMonsterStatEditor.Instance;
        if (monsterStatEditor != null)
        {
            monsterStatEditor.OnMonsterStatChanged += OnMonsterStatOverrideChanged;
        }

        // FloatingTextSpawner 연결
        floatingTextSpawner = FindFirstObjectByType<FloatingTextSpawner>();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (monsterStatEditor != null)
        {
            monsterStatEditor.OnMonsterStatChanged -= OnMonsterStatOverrideChanged;
        }
    }

    /// <summary>
    /// MonsterStatEditor에서 스탯이 변경되었을 때 호출
    /// 이미 스폰된 해당 몬스터들에 스탯 적용
    /// </summary>
    private void OnMonsterStatOverrideChanged(int monsterId, float hp, float speed, int exp)
    {
        // 스폰된 몬스터 중 해당 ID의 몬스터에 스탯 적용
        foreach (var monsterObj in spawnedMonsters)
        {
            if (monsterObj == null) continue;

            var enemy = monsterObj.GetComponent<Enemy>();
            if (enemy == null) continue;

            // 몬스터 ID 확인
            int enemyMonsterId = GetMonsterIdFromEnemy(enemy);
            if (enemyMonsterId == monsterId)
            {
                ApplyStatsToEnemy(enemy, hp, speed, exp);
            }
        }
    }

    private int GetMonsterIdFromEnemy(Enemy enemy)
    {
        if (enemy == null) return -1;

        var bindingFlags = System.Reflection.BindingFlags.NonPublic |
                           System.Reflection.BindingFlags.Public |
                           System.Reflection.BindingFlags.Instance;

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

    private void ApplyStatsToEnemy(Enemy enemy, float hp, float speed, int exp)
    {
        if (enemy == null) return;

        var bindingFlags = System.Reflection.BindingFlags.NonPublic |
                           System.Reflection.BindingFlags.Public |
                           System.Reflection.BindingFlags.Instance;

        var maxHpField = typeof(Enemy).GetField("maxHp", bindingFlags);
        if (maxHpField != null) maxHpField.SetValue(enemy, hp);

        var currentHpField = typeof(Enemy).GetField("currentHp", bindingFlags);
        if (currentHpField != null) currentHpField.SetValue(enemy, hp);

        var speedField = typeof(Enemy).GetField("speed", bindingFlags);
        if (speedField != null) speedField.SetValue(enemy, speed);

        var expField = typeof(Enemy).GetField("baseStageExp", bindingFlags);
        if (expField != null) expField.SetValue(enemy, exp);
    }

    private void LoadMonsterPrefab()
    {
        // Monster 프리팹 Addressable에서 로드
        var handle = Addressables.LoadAssetAsync<GameObject>("Monster");
        cachedMonsterPrefab = handle.WaitForCompletion();

        if (cachedMonsterPrefab != null)
        {
            Debug.Log("[TestMonsterSpawnController] Monster 프리팹 로드 완료");
        }
        else
        {
            Debug.LogError("[TestMonsterSpawnController] Monster 프리팹 로드 실패!");
        }
    }
    
    private async void SetupDropdown()
    {
        // DataTable 로드 대기
        while (!DataTableManager.IsInitialized)
        {
            await UniTask.Yield();
        }
        
        // 몬스터 데이터 로드
        monsterList.Clear();
        monsterDropdown.ClearOptions();
        
        var allMonsters = DataTableManager.MonsterTable.GetAll();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        
        foreach (var monster in allMonsters)
        {
            monsterList.Add(monster);
            // ID와 이름을 함께 표시
            string displayName = $"{monster.MON_ID}: {monster.MON_NAME}";
            options.Add(new TMP_Dropdown.OptionData(displayName));
        }
        
        monsterDropdown.AddOptions(options);
        
        // 드롭다운 선택 이벤트
        monsterDropdown.onValueChanged.AddListener(OnMonsterSelected);
        
        // 첫 번째 몬스터 선택
        if (monsterList.Count > 0)
        {
            selectedMonsterId = monsterList[0].MON_ID;
        }
        
        Debug.Log($"[TestMonsterSpawnController] {monsterList.Count}개 몬스터 로드 완료");
    }
    
    private void SetupButtons()
    {
        if (spawnButton != null)
        {
            spawnButton.onClick.AddListener(OnSpawnButtonClicked);
        }
        
        if (killAllButton != null)
        {
            killAllButton.onClick.AddListener(OnKillAllButtonClicked);
        }
        
        // 기본 스폰 개수 설정
        if (spawnCountInput != null)
        {
            spawnCountInput.text = "1";
        }
    }
    
    private void OnMonsterSelected(int index)
    {
        if (index >= 0 && index < monsterList.Count)
        {
            selectedMonsterId = monsterList[index].MON_ID;
            Debug.Log($"[TestMonsterSpawnController] 선택된 몬스터: {monsterList[index].MON_NAME} (ID: {selectedMonsterId})");
        }
    }
    
    private void OnSpawnButtonClicked()
    {
        if (selectedMonsterId < 0)
        {
            Debug.LogWarning("[TestMonsterSpawnController] 몬스터를 선택해주세요!");
            return;
        }
        
        int spawnCount = 1;
        if (spawnCountInput != null && int.TryParse(spawnCountInput.text, out int parsed))
        {
            spawnCount = Mathf.Max(1, parsed);
        }
        
        bool isBoss = bossToggle != null && bossToggle.isOn;
        
        // 웨이브 설정에서 배율 가져오기
        var waveSettings = FindObjectOfType<TestWaveSettingsController>();
        if (waveSettings != null)
        {
            hpMultiplier = waveSettings.HpMultiplier;
            expMultiplier = waveSettings.ExpMultiplier;
        }
        
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnMonster(selectedMonsterId, isBoss);
        }
        
        Debug.Log($"[TestMonsterSpawnController] {spawnCount}마리 스폰 완료 (Boss: {isBoss}, HP: x{hpMultiplier}, EXP: x{expMultiplier})");
    }
    
    private void SpawnMonster(int monsterId, bool isBoss)
    {
        MonsterData data = DataTableManager.MonsterTable.Get(monsterId);
        if (data == null)
        {
            Debug.LogError("[TestMonsterSpawnController] 몬스터 ID " + monsterId + " 없음!");
            return;
        }

        if (cachedMonsterPrefab == null)
        {
            Debug.LogError("[TestMonsterSpawnController] Monster 프리팹이 로드되지 않음!");
            return;
        }

        // 프리팹 직접 인스턴스화
        GameObject monsterObj = Instantiate(cachedMonsterPrefab);
        spawnedMonsters.Add(monsterObj);

        // 스폰 위치 설정
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : new Vector3(0, 6, 0);
        spawnPos.x += Random.Range(-horizontalRange, horizontalRange);
        monsterObj.transform.position = spawnPos;

        // Enemy 초기화
        Enemy enemy = monsterObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            // poolKey는 테스트용이므로 null 또는 빈 문자열
            enemy.InitializeWithData(poolManager, "", data, isBoss, hpMultiplier, expMultiplier);

            // FloatingTextSpawner 설정
            if (floatingTextSpawner != null)
            {
                enemy.SetFloatingTextSpawner(floatingTextSpawner);
            }

            // 비주얼 로드
            if (!string.IsNullOrEmpty(data.MON_MODEL))
            {
                enemy.LoadVisual(data.MON_MODEL);
            }

            // MonsterStatEditor의 오버라이드가 있으면 적용
            if (monsterStatEditor == null)
            {
                monsterStatEditor = TestMonsterStatEditor.Instance;
            }

            if (monsterStatEditor != null &&
                monsterStatEditor.TryGetOverrideStats(monsterId, out float overrideHp, out float overrideSpeed, out int overrideExp))
            {
                ApplyStatsToEnemy(enemy, overrideHp, overrideSpeed, overrideExp);
                Debug.Log($"[TestMonsterSpawnController] 오버라이드 스탯 적용: HP={overrideHp}, Speed={overrideSpeed}, Exp={overrideExp}");
            }
        }
    }
    
    private void OnKillAllButtonClicked()
    {
        // 스폰된 몬스터 모두 제거
        foreach (var monsterObj in spawnedMonsters)
        {
            if (monsterObj != null)
            {
                var enemy = monsterObj.GetComponent<Enemy>();
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.TakeDamage(999999f);
                }
                // Pool로 반환하지 않고 직접 파괴
                Destroy(monsterObj, 0.5f);
            }
        }
        spawnedMonsters.Clear();

        // 씬에 있는 다른 몬스터들도 제거
        var monsters = GameObject.FindGameObjectsWithTag("Monster");
        foreach (var monster in monsters)
        {
            var enemy = monster.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(999999f);
            }
        }

        Debug.Log("[TestMonsterSpawnController] 모든 몬스터 제거됨");
    }
    
    // 외부에서 배율 설정
    public void SetMultipliers(float hp, float exp)
    {
        hpMultiplier = hp;
        expMultiplier = exp;
    }
    
    // 드롭다운에서 이름으로 검색
    public void SearchMonsterByName(string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return;
            
        searchText = searchText.ToLower();
        
        for (int i = 0; i < monsterList.Count; i++)
        {
            if (monsterList[i].MON_NAME.ToLower().Contains(searchText) ||
                monsterList[i].MON_ID.ToString().Contains(searchText))
            {
                monsterDropdown.value = i;
                break;
            }
        }
    }
}
