using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DataTableManager : MonoBehaviour
{
    private static DataTableManager _instance;
    public static DataTableManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("DataTableManager");
                _instance = go.AddComponent<DataTableManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private readonly Dictionary<string, DataTable> tables = new Dictionary<string, DataTable>();
    private bool _isInitialized = false;
    private bool _isInitializing = false;

    // 기존 static 호출 유지
    public static bool IsInitialized => _instance != null && _instance._isInitialized;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static async UniTask InitAsync()
    {
        await Instance.InitAsyncInternal();
    }

    private async UniTask InitAsyncInternal()
    {
        if (_isInitialized)
            return;
        if (_isInitializing)
        {
            while (!_isInitialized)
            {
                await UniTask.Yield();
            }
            return;
        }
        _isInitializing = true;

        try
        {
            await LoadAllTablesAsync();
            Debug.Log("DataTableManager initialized");
            _isInitialized = true;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private async UniTask LoadAllTablesAsync()
    {
        var loadTasks = new List<UniTask>
        {
            LoadTableAsync<DataTable_String>(DataTableIds.String),
            LoadTableAsync<DataTable_Stage>(DataTableIds.Stage),
            LoadTableAsync<DataTable_Monster>(DataTableIds.Monster),
            LoadTableAsync<DataTable_Wave>(DataTableIds.Wave),
            LoadTableAsync<DataTable_UnitCatalog>(DataTableIds.UnitCatalog),
            LoadTableAsync<DataTable_Unit>(DataTableIds.Unit),
            LoadTableAsync<DataTable_Skill>(DataTableIds.Skill),
            LoadTableAsync<DataTable_NormalEnforce>(DataTableIds.NormalEnforce),
            LoadTableAsync<DataTable_Effect>(DataTableIds.Effect),
            LoadTableAsync<DataTable_Item>(DataTableIds.Item),
            LoadTableAsync<DataTable_Selling>(DataTableIds.Selling),
            LoadTableAsync<DataTable_StageLevel>(DataTableIds.StageLevel),
            LoadTableAsync<DataTable_HeroEnforce>(DataTableIds.HeroEnforce),
            LoadTableAsync<DataTable_HeroEnforceEffect>(DataTableIds.HeroEnforceEffect),
            LoadTableAsync<DataTable_UnitGacha>(DataTableIds.UnitGacha),

            // 던전
            LoadTableAsync<DataTable_Ore>(DataTableIds.Ore),
            LoadTableAsync<DataTable_OreDungeon>(DataTableIds.OreDungeon),
            LoadTableAsync<DataTable_DungeonSetting>(DataTableIds.DungeonSetting),
        };

        await UniTask.WhenAll(loadTasks);
    }

    private async UniTask LoadTableAsync<T>(string id) where T : DataTable, new()
    {
        if (tables.ContainsKey(id))
        {
            Debug.Log($"[DataTableManager] {id} 이미 로드됨 - 스킵");
            return;
        }

        var table = new T();
        await table.LoadAsync(id);
        tables.Add(id, table);
    }

    // static 프로퍼티 유지 - 기존 호출부 변경 불필요
    public static DataTable_String StringTable => Instance.Get<DataTable_String>(DataTableIds.String);
    public static DataTable_Stage StageTable => Instance.Get<DataTable_Stage>(DataTableIds.Stage);
    public static DataTable_Monster MonsterTable => Instance.Get<DataTable_Monster>(DataTableIds.Monster);
    public static DataTable_Wave WaveTable => Instance.Get<DataTable_Wave>(DataTableIds.Wave);
    public static DataTable_UnitCatalog UnitCatalogTable => Instance.Get<DataTable_UnitCatalog>(DataTableIds.UnitCatalog);
    public static DataTable_Unit UnitTable => Instance.Get<DataTable_Unit>(DataTableIds.Unit);
    public static DataTable_Skill SkillTable => Instance.Get<DataTable_Skill>(DataTableIds.Skill);
    public static DataTable_NormalEnforce NormalEnforceTable => Instance.Get<DataTable_NormalEnforce>(DataTableIds.NormalEnforce);
    public static DataTable_Effect EffectTable => Instance.Get<DataTable_Effect>(DataTableIds.Effect);
    public static DataTable_Item ItemTable => Instance.Get<DataTable_Item>(DataTableIds.Item);
    public static DataTable_Selling SellingTable => Instance.Get<DataTable_Selling>(DataTableIds.Selling);
    public static DataTable_StageLevel StageLevelTable => Instance.Get<DataTable_StageLevel>(DataTableIds.StageLevel);
    public static DataTable_HeroEnforce heroEnforceTable => Instance.Get<DataTable_HeroEnforce>(DataTableIds.HeroEnforce);
    public static DataTable_HeroEnforceEffect heroEnforceEffectTable => Instance.Get<DataTable_HeroEnforceEffect>(DataTableIds.HeroEnforceEffect);
    public static DataTable_UnitGacha UnitGachaTable => Instance.Get<DataTable_UnitGacha>(DataTableIds.UnitGacha);
    public static DataTable_Ore OreTable => Instance.Get<DataTable_Ore>(DataTableIds.Ore);
    public static DataTable_OreDungeon OreDungeonTable => Instance.Get<DataTable_OreDungeon>(DataTableIds.OreDungeon);
    public static DataTable_DungeonSetting DungeonSettingTable => Instance.Get<DataTable_DungeonSetting>(DataTableIds.DungeonSetting);

    public T Get<T>(string id) where T : DataTable
    {
        if (!tables.ContainsKey(id))
        {
            Debug.LogError("존재하지 않는 키");
            return null;
        }
        return tables[id] as T;
    }
}
