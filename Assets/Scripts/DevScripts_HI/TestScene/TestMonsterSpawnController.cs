using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using TMPro;
using Cysharp.Threading.Tasks;

/// <summary>
/// 몬스터/보스 스폰을 위한 드롭다운 UI 및 스폰 로직
/// 테스트 씬에서 MonsterSpawner.Instance를 대체하여 스킬 시스템과 연동
/// </summary>
public class TestMonsterSpawnController : MonoBehaviour, IMonsterProvider
{
    public static TestMonsterSpawnController Instance { get; private set; }

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
    private List<Enemy> activeEnemies = new List<Enemy>();

    // MonsterStatEditor 연동
    private TestMonsterStatEditor monsterStatEditor;

    // FloatingText 연동
    private FloatingTextSpawner floatingTextSpawner;

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(ObjectPoolManager pool)
    {
        poolManager = pool;

        // MonsterSpawner.Instance가 없을 때 이 컨트롤러를 몬스터 제공자로 등록
        MonsterProviderRegistry.Register(this);

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
        // 몬스터 제공자 등록 해제
        MonsterProviderRegistry.Unregister(this);

        // 이벤트 구독 해제
        if (monsterStatEditor != null)
        {
            monsterStatEditor.OnMonsterStatChanged -= OnMonsterStatOverrideChanged;
        }

        Instance = null;
    }

    /// <summary>
    /// IMonsterProvider 구현: 활성 몬스터 리스트 반환
    /// </summary>
    public IReadOnlyList<Enemy> GetActiveMonsters()
    {
        // null이거나 비활성화된 몬스터 제거
        activeEnemies.RemoveAll(e => e == null || !e.gameObject.activeSelf || e.IsDead);
        return activeEnemies;
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

            // 활성 몬스터 리스트에 추가 (스킬 시스템 연동용)
            activeEnemies.Add(enemy);

            // 몬스터 사망 시 리스트에서 제거
            enemy.OnDeath += OnEnemyDeath;

            // FloatingTextSpawner 설정
            if (floatingTextSpawner != null)
            {
                enemy.SetFloatingTextSpawner(floatingTextSpawner);
            }

            // 상태이상 이펙트 높이 조정 (테스트 씬용)
            var statusEffectManager = monsterObj.GetComponent<StatusEffectManager>();
            if (statusEffectManager == null)
            {
                statusEffectManager = monsterObj.AddComponent<StatusEffectManager>();
            }
            // effectOffset 조정 (리플렉션 사용)
            var effectOffsetField = typeof(StatusEffectManager).GetField("effectOffset",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (effectOffsetField != null)
            {
                effectOffsetField.SetValue(statusEffectManager, new Vector3(0f, 0.2f, 0f));
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
            }
        }
    }

    private void OnEnemyDeath(Enemy enemy)
    {
        if (enemy != null)
        {
            enemy.OnDeath -= OnEnemyDeath;
            activeEnemies.Remove(enemy);
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
        activeEnemies.Clear();

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
