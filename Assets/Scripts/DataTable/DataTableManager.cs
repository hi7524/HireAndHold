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
        if (isInitializing)
        {
            while (!IsInitialized)
            {
                await UniTask.Yield();
            }
            return;
        }
        isInitializing = true;

        try
        {
            await LoadAllTablesAsync();
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

            // 업적
            LoadTableAsync<DataTable_Achievement>(DataTableIds.Achievement),
            // 일일 보상
            LoadTableAsync<DataTable_DailyReward>(DataTableIds.DailyReward),
        };

        await UniTask.WhenAll(loadTasks);
    }

    private static async UniTask LoadTableAsync<T>(string id) where T : DataTable, new()
    {
        // 이미 로드된 테이블은 스킵
        if (tables.ContainsKey(id))
        {
            return;
        }

        var table = new T();
        await table.LoadAsync(id);
        tables.Add(id, table);
    }

    public static DataTable_String StringTable => Get<DataTable_String>(DataTableIds.String);
    public static DataTable_Stage StageTable => Get<DataTable_Stage>(DataTableIds.Stage);
    public static DataTable_Monster MonsterTable => Get<DataTable_Monster>(DataTableIds.Monster);
    public static DataTable_Wave WaveTable => Get<DataTable_Wave>(DataTableIds.Wave);
    public static DataTable_UnitCatalog UnitCatalogTable => Get<DataTable_UnitCatalog>(DataTableIds.UnitCatalog);
    public static DataTable_Unit UnitTable => Get<DataTable_Unit>(DataTableIds.Unit);
    public static DataTable_Skill SkillTable => Get<DataTable_Skill>(DataTableIds.Skill);
    public static DataTable_NormalEnforce NormalEnforceTable => Get<DataTable_NormalEnforce>(DataTableIds.NormalEnforce);
    public static DataTable_Effect EffectTable => Get<DataTable_Effect>(DataTableIds.Effect);
    public static DataTable_Item ItemTable => Get<DataTable_Item>(DataTableIds.Item);
    public static DataTable_Selling SellingTable => Get<DataTable_Selling>(DataTableIds.Selling);
    public static DataTable_StageLevel StageLevelTable => Get<DataTable_StageLevel>(DataTableIds.StageLevel);
    public static DataTable_HeroEnforce heroEnforceTable => Get<DataTable_HeroEnforce>(DataTableIds.HeroEnforce);
    public static DataTable_HeroEnforceEffect heroEnforceEffectTable => Get<DataTable_HeroEnforceEffect>(DataTableIds.HeroEnforceEffect);
    public static DataTable_UnitGacha UnitGachaTable => Get<DataTable_UnitGacha>(DataTableIds.UnitGacha);
    public static DataTable_Ore OreTable => Get<DataTable_Ore>(DataTableIds.Ore);
    public static DataTable_OreDungeon OreDungeonTable => Get<DataTable_OreDungeon>(DataTableIds.OreDungeon);
    public static DataTable_DungeonSetting DungeonSettingTable => Get<DataTable_DungeonSetting>(DataTableIds.DungeonSetting);
    public static DataTable_Achievement AchievementTable => Get<DataTable_Achievement>(DataTableIds.Achievement);
    public static DataTable_DailyReward DailyRewardTable => Get<DataTable_DailyReward>(DataTableIds.DailyReward);

    public static T Get<T>(string id) where T : DataTable
    {
        if (!tables.ContainsKey(id))
        {
            Debug.LogError("존재하지 않는 키");
            return null;
        }
        return tables[id] as T;
    }

    public static string GetString(int stringId)
    {
        var stringTable = StringTable;
        if (stringTable == null)
            return null;

        var result = stringTable.Get(stringId);

        if (result == "존재하지 않는 키")
            return null;

        return result;
    }


    public static string GetString(string stringId)
    {
        if (string.IsNullOrEmpty(stringId))
            return null;

        if (int.TryParse(stringId, out int id))
        {
            return GetString(id);
        }

        return null;
    }
}
