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
    public const string FIRST_BUFF = "FIRST_BUFF";           // 703 스테이지: 첫 버프 획득
    public const string MID_BOSS_CLEAR = "MID_BOSS_CLEAR";   // 703 스테이지: 중간 보스 클리어
    public const string GACHA_TUTORIAL = "GACHA_TUTORIAL";       // 로비: 뽑기 튜토리얼 시작
    public const string ENHANCE_TUTORIAL = "ENHANCE_TUTORIAL"; // 로비: 강화 튜토리얼 시작
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
/// 튜토리얼 시퀀스 ID (기획서 v2.0)
/// </summary>
public static class TutorialSequenceIds
{
    // 1. 로비 튜토리얼
    public const string LobbyTutorial = "lobby_tutorial";

    // 2. 1스테이지 인게임
    public const string Stage1Tutorial = "stage1_tutorial";

    // 3. 1스테이지 클리어 후 로비로
    public const string Stage1Clear = "stage1_clear_tutorial";

    // 3-2. 로비에서 뽑기 튜토리얼
    public const string GachaTutorial = "gacha_tutorial";

    // 4. 3스테이지 클리어 후 로비로
    public const string Stage3Clear = "stage3_clear_tutorial";

    // 4-2. 로비에서 강화 튜토리얼
    public const string EnhanceTutorial = "enhance_tutorial";

    // 5. 3스테이지 인게임 튜토리얼 (분리됨)
    public const string Stage3Intro = "stage3_intro";           // 스테이지 시작
    public const string Stage3Buff = "stage3_buff";             // 첫 버프 획득
    public const string Stage3Passive = "stage3_passive";       // 레벨 3 패시브 선택
    public const string Stage3MidBoss = "stage3_midboss";       // 중간 보스 클리어

    // 6. 던전 튜토리얼
    public const string DungeonTutorial = "dungeon_tutorial";
}