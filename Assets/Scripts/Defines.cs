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
    public static readonly string Tutorial = "TutorialTable";
    public static readonly string Quest = "QuestTable";
    public static readonly string Package = "PackageTable";
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

/// <summary>
/// 튜토리얼 조건 키
/// </summary>
public static class TutorialConditions
{
    public const string STAGE_BUFF = "STAGE_BUFF";
    public const string COMPLETE_BUFF = "COMPLETE_BUFF";
    public const string FIRST_EXP = "FIRST_EXP";
    public const string DUNGEON_TAB_FIRST_ENTER = "DUNGEON_TAB_FIRST_ENTER";
    public const string DUNGEON_STAGE_FIRST_ENTER = "DUNGEON_STAGE_FIRST_ENTER";
}

/// <summary>
/// 튜토리얼 버튼 이름
/// </summary>
public static class TutorialButtons
{
    public const string LobbyButton = "LobbyButton";
    public const string StartButton = "StartButton";
    public const string StageButton = "StageButton";
    public const string HomeButton = "HomeButton";
    public const string DungeonEnterButton = "DungeonEnterButton";
}

/// <summary>
/// 튜토리얼 시퀀스 ID
/// </summary>
public static class TutorialSequenceIds
{
    public const string Stage1Clear = "forced_03_stage1clear";
    public const string LobbyTutorial = "forced_05_lobbytutorial";
    public const string Gacha = "forced_05_gacha";
    public const string GachaPart2 = "forced_05_gacha_part2";
    public const string EnhanceTutorial = "enhance_tutorial";
    public const string EnhancePart2 = "enhance_part2";
    public const string DungeonTutorial = "dungeon_tutorial";
    public const string DungeonPart2 = "dungeon_part2";
}