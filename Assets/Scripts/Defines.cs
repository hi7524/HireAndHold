using UnityEngine;

public static class DataTableIds
{
    public static readonly string String = "StringTable";
    public static readonly string Stage = "StageTable";
    public static readonly string Monster = "MonsterTable";
    public static readonly string Wave = "WaveTable";
    public static readonly string UnitCatalog = "UnitCatalogTable";
    public static readonly string Skill = "SkillTable";
    public static readonly string Unit = "UnitTable";
    public static readonly string NormalEnforce = "NormalEnforceTable";
    public static readonly string HeroEnforce = "HeroEnforceTable";
    public static readonly string HeroEnforceEffect = "HeroEnforceEffectTable";
    public static readonly string Effect = "EffectTable";
    public static readonly string Item = "ItemTable";
    public static readonly string Selling = "SellingTable";
    public static readonly string StageLevel = "StageLevelTable";
    public static readonly string UnitGacha = "UnitGachaTable";
    public static readonly string Ore = "OreTable";
    public static readonly string OreDungeon = "OreDungeonTable";
    public static readonly string DungeonSetting = "DungeonSettingTable";
    public static readonly string Achievement = "AchievementsTable";
    public static readonly string DailyReward = "DailyRewardTable";
}

public static class Tags
{
    // public static readonly string GameController = "GameController";
    public static readonly string Monster = "Monster";
    public static readonly string PoolManager = "PoolManager";
}

public static class AudioMixerParams
{
    public static readonly string Master = "Master";
    public static readonly string Bgm = "BGM";
    public static readonly string Sfx = "SFX";
}

public static class AnimParams
{
    public static readonly int IsActive = Animator.StringToHash("IsActive");
    public static readonly int Slash = Animator.StringToHash("Slash");
    public static readonly int SimpleBowShot = Animator.StringToHash("SimpleBowShot");
    public static readonly int Cast = Animator.StringToHash("Cast");
}

public static class GameConstants
{
    public static readonly float previewCellSizeObject = 0.5f;
    public static readonly float previewCellSizeUi = 55f;
}

public static class PoolKey
{
    public static readonly string UnitSkill = "UnitSkill";
}

public enum ProjectileType
{
    Default = 0,
    Bow = 1,
}