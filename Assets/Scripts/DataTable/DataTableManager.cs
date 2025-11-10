using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class DataTableManager
{
    private static readonly Dictionary<string, DataTable> tables = new Dictionary<string, DataTable>();

    public static async UniTask Init()
    {
        await LoadAllTablesAsync();
        Debug.Log("DataTableManager initialized");
    }

    private static async UniTask LoadAllTablesAsync()
    {
        // 모든 테이블을 병렬로 로드
        var loadTasks = new List<UniTask>
        {
            LoadTableAsync<DataTable_String>(DataTableIds.String),
            // 다른 테이블들 추가
            // LoadTableAsync<DataTable_Item>(DataTableIds.Item),
            // LoadTableAsync<DataTable_Character>(DataTableIds.Character),
        };

        await UniTask.WhenAll(loadTasks);
    }

    private static async UniTask LoadTableAsync<T>(string id) where T : DataTable, new()
    {
        var table = new T();
        await table.LoadAsync(id);
        tables.Add(id, table);
    }

    public static DataTable_String StringTable => Get<DataTable_String>(DataTableIds.String);
    //

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
