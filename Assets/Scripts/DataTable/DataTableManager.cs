using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class DataTableManager
{
    private static readonly Dictionary<string, DataTable> tables = new Dictionary<string, DataTable>();
    public static bool IsInitialized { get; private set; } = false;
    private static bool isInitializing = false;
   
    public static async UniTask InitAsync()
    {
        if (IsInitialized)
            return;

        // 누군가 이미 초기화 중이면 끝날 때까지 기다렸다가 리턴
        if (isInitializing)
        {
            while (!IsInitialized)
            {
                await UniTask.Yield();
            }
            return;
        }

        // 여기까지 온 애만 실제 초기화 수행
        isInitializing = true;

        try
        {
            await LoadAllTablesAsync();
            Debug.Log("DataTableManager initialized");
            IsInitialized = true;
        }
        finally
        {
            isInitializing = false;
        }
    }

    private static async UniTask LoadAllTablesAsync()
    {
        // 모든 테이블을 병렬로 로드
        var loadTasks = new List<UniTask>
        {
            // LoadTableAsync<DataTable_String>(DataTableIds.String),
            LoadTableAsync<DataTable_Stage>(DataTableIds.Stage),
            LoadTableAsync<DataTable_Monster>(DataTableIds.Monster),
            LoadTableAsync<DataTable_Wave>(DataTableIds.Wave),
            LoadTableAsync<DataTable_UnitCatalog>(DataTableIds.UnitCatalog),
            LoadTableAsync<DataTable_Unit>(DataTableIds.Unit),
            LoadTableAsync<DataTable_Skill>(DataTableIds.Skill),
            // 다른 테이블들 추가
            // LoadTableAsync<DataTable_Item>(DataTableIds.Item),
            // LoadTableAsync<DataTable_Character>(DataTableIds.Character),
        };

        await UniTask.WhenAll(loadTasks);
    }

    private static async UniTask LoadTableAsync<T>(string id) where T : DataTable, new()
    {
        // 이미 로드된 테이블은 스킵
        if (tables.ContainsKey(id))
        {
            Debug.Log($"[DataTableManager] {id} 이미 로드됨 - 스킵");
            return;
        }

        var table = new T();
        await table.LoadAsync(id);
        tables.Add(id, table);
    }

    // public static DataTable_String StringTable => Get<DataTable_String>(DataTableIds.String);
    public static DataTable_Stage StageTable => Get<DataTable_Stage>(DataTableIds.Stage);
    public static DataTable_Monster MonsterTable => Get<DataTable_Monster>(DataTableIds.Monster);
    public static DataTable_Wave WaveTable => Get<DataTable_Wave>(DataTableIds.Wave);
    public static DataTable_UnitCatalog UnitCatalogTable => Get<DataTable_UnitCatalog>(DataTableIds.UnitCatalog);
    public static DataTable_Unit UnitTable => Get<DataTable_Unit>(DataTableIds.Unit);
    public static DataTable_Skill SkillTable => Get<DataTable_Skill>(DataTableIds.Skill);

    public static T Get<T>(string id) where T : DataTable
    {
        if (!tables.ContainsKey(id))
        {
            Debug.LogError("존재하지 않는 키");
            return null;
        }
        return tables[id] as T;
    }
}
