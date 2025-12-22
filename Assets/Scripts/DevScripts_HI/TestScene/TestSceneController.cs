using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 테스트 씬의 전체 초기화 및 참조 설정을 담당
/// 기존 클래스들(Enemy, Unit, MonsterSpawner)이 필요로 하는 씬 참조를 설정
/// </summary>
public class TestSceneController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform wallTransform;
    [SerializeField] private ExperienceCollector expCollector;
    [SerializeField] private ObjectPoolManager poolManager;
    [SerializeField] private PassiveSkillManager passiveSkillManager;
    [SerializeField] private FloatingTextSpawner floatingTextSpawner;
    
    [Header("Test Controllers")]
    [SerializeField] private TestMonsterSpawnController monsterSpawnController;
    [SerializeField] private TestWaveSettingsController waveSettingsController;
    [SerializeField] private TestPassiveSkillController passiveSkillController;
    [SerializeField] private TestPlayerSkillController playerSkillController;
    [SerializeField] private TestUnitStatEditor unitStatEditor;
    
    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private GameObject unitStatPanel;
    
    public static TestSceneController Instance { get; private set; }
    
    public ObjectPoolManager PoolManager => poolManager;
    public PassiveSkillManager PassiveSkillManager => passiveSkillManager;
    public Transform WallTransform => wallTransform;
    public ExperienceCollector ExpCollector => expCollector;
    public FloatingTextSpawner FloatingTextSpawner => floatingTextSpawner;
    
    private async void Awake()
    {
        Instance = this;

        // 할당되지 않은 참조들 자동 탐색
        AutoFindReferences();

        // DataTable 초기화 대기
        await DataTableManager.InitAsync();

        // 씬 참조 설정 (Enemy, Unit에서 사용)
        SetupSceneReferences();

        // 각 컨트롤러 초기화
        InitializeControllers();
    }

    private void AutoFindReferences()
    {
        if (wallTransform == null)
            wallTransform = GameObject.FindWithTag("Wall")?.transform;

        if (expCollector == null)
            expCollector = FindFirstObjectByType<ExperienceCollector>();

        if (poolManager == null)
            poolManager = FindFirstObjectByType<ObjectPoolManager>();

        if (passiveSkillManager == null)
            passiveSkillManager = FindFirstObjectByType<PassiveSkillManager>();

        if (floatingTextSpawner == null)
            floatingTextSpawner = FindFirstObjectByType<FloatingTextSpawner>();

        if (monsterSpawnController == null)
            monsterSpawnController = FindFirstObjectByType<TestMonsterSpawnController>();
    }
    
    private void SetupSceneReferences()
    {
        // Enemy 클래스의 정적 참조 설정
        if (wallTransform != null && expCollector != null)
        {
            Enemy.SetSceneReferences(wallTransform, expCollector);
        }
        
        // Unit 클래스의 정적 참조 설정
        if (poolManager != null && passiveSkillManager != null)
        {
            Unit.SetSceneReferences(poolManager, passiveSkillManager);
        }
    }
    
    private void InitializeControllers()
    {
        // 몬스터 스폰 컨트롤러 초기화
        if (monsterSpawnController != null)
        {
            monsterSpawnController.Initialize(poolManager);
        }
        
        // 웨이브 설정 컨트롤러 초기화
        if (waveSettingsController != null)
        {
            waveSettingsController.Initialize();
        }
        
        // 패시브 스킬 컨트롤러 초기화
        if (passiveSkillController != null)
        {
            passiveSkillController.Initialize(passiveSkillManager);
        }
        
        // 플레이어 스킬 컨트롤러 초기화
        if (playerSkillController != null)
        {
            playerSkillController.Initialize();
        }
        
        // 유닛 스탯 에디터 초기화
        if (unitStatEditor != null)
        {
            unitStatEditor.Initialize();
        }
    }
    
    private void OnDestroy()
    {
        // 정적 참조 정리
        Enemy.ClearSceneReferences();
        Unit.ClearSceneReferences();
        
        Instance = null;
    }
    
    // UI 패널 토글
    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
    
    public void ToggleSkillPanel()
    {
        if (skillPanel != null)
            skillPanel.SetActive(!skillPanel.activeSelf);
    }
    
    public void ToggleUnitStatPanel()
    {
        if (unitStatPanel != null)
            unitStatPanel.SetActive(!unitStatPanel.activeSelf);
    }
}
