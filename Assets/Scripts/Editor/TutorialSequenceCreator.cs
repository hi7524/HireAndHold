using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Tutorial;

/// <summary>
/// 튜토리얼 시퀀스 에셋 생성 에디터 윈도우
/// 기획서 ver1.6 기준
/// </summary>
public class TutorialSequenceCreator : EditorWindow
{
    private const string FOLDER_PATH = "Assets/ScriptableObjects/Tutorial";

    [MenuItem("Tools/Tutorial/Create All Sequences (기획서 v1.6)")]
    [MenuItem("Tools/Tutorial/Create Sample Sequences")]
    public static void CreateAllSequences()
    {
        EnsureFolder();

        CreateForcedTutorial_Part1_Lobby();
        CreateForcedTutorial_Part2_Stage1();
        CreateForcedTutorial_Part3_Stage1Clear();
        CreateForcedTutorial_Part4_Stage2();
        CreateForcedTutorial_Part5_Gacha();
        CreateStage3Tutorial();
        CreateStage4Tutorial();
        CreateEnhanceTutorial();
        CreateStage6Tutorial();
        CreateDungeonTutorial();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[TutorialSequenceCreator] 기획서 v1.6 기준 튜토리얼 시퀀스 생성 완료!");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(FOLDER_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Tutorial");
        }
    }

    #region 강제 진행 튜토리얼 (1-2 스테이지)

    /// <summary>
    /// 강제 튜토리얼 Part 1: 로비 첫 입장
    /// </summary>
    private static void CreateForcedTutorial_Part1_Lobby()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "forced_01_lobby";
        sequence.description = "강제 튜토리얼 Part1 - 로비 첫 입장 (게임 시작 버튼까지)";
        sequence.triggerType = TutorialTriggerType.OnLobbyEnter;
        sequence.triggerStageId = 0;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // 103101: "안녕하세요, 길드장 님! 몬스터들이 몰려오고 있어요. 방벽을 방어하러 가볼까요?"
            Step(103101, TutorialActionType.Touch, DialogAnchor.Bottom, isCheckpoint: true),

            // 스테이지 버튼 터치 유도 (텍스트 없이 하이라이트만)
            Step(0, TutorialActionType.TouchTarget, DialogAnchor.Bottom,
                targetButton: "StageButton", highlight: "StageButton", showHand: true, delayBefore: 0.3f),

            // 게임 시작 버튼 터치 유도
            Step(0, TutorialActionType.TouchTarget, DialogAnchor.Bottom,
                targetButton: "StartButton", highlight: "StartButton", showHand: true, delayBefore: 0.3f),
        };

        SaveAsset(sequence, "Seq_Forced_01_Lobby");
    }

    /// <summary>
    /// 강제 튜토리얼 Part 2: 1 스테이지 인게임
    /// </summary>
    private static void CreateForcedTutorial_Part2_Stage1()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "forced_02_stage1";
        sequence.description = "강제 튜토리얼 Part2 - 1스테이지 인게임";
        sequence.triggerType = TutorialTriggerType.OnStageStart;
        sequence.triggerStageId = 1;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // === 1 레벨 시작 ===
            // 103102: "비어 있는 타일 위에 모험가를 배치해 몬스터를 공격할 수 있어요."
            Step(103102, TutorialActionType.Touch, DialogAnchor.Top, highlight: "TileGrid"),

            // 103103: "배치할 수 없는 곳이면 타일이 빨갛게, 배치할 수 있으면 초록색으로 보일 거예요."
            Step(103103, TutorialActionType.Touch, DialogAnchor.Top),

            // 103104: "엘렌을 타일 위로 드래그 해 배치해 보세요."
            Step(103104, TutorialActionType.DragToPosition, DialogAnchor.Top,
                dragSource: "UnitCard_Ellen", highlight: "UnitCard_Ellen", showHand: true,
                allowedUnits: new[] { "Ellen" },
                allowedTiles: new[] { new Vector2Int(3, 0) }), // 지정 위치

            // 103105: "좋아요! 타일 위의 모험가는 언제든 원하는 위치로 이동시킬 수 있어요."
            Step(103105, TutorialActionType.Touch, DialogAnchor.Top),

            // 103106: "배치된 모험가는 자동으로 몬스터를 공격해요..."
            Step(103106, TutorialActionType.Touch, DialogAnchor.Top, highlight: "ExpBar"),

            // 103107: "몬스터가 방벽에 다다르면 방벽의 내구도가 깎여요..."
            Step(103107, TutorialActionType.Touch, DialogAnchor.Top, highlight: "HealthBar"),

            // 103108: "전투는 1배속부터 3배속까지 조절할 수 있어요."
            Step(103108, TutorialActionType.Touch, DialogAnchor.Top, highlight: "SpeedButton"),

            // 103109: "일시정지 버튼을 눌러 전투를 잠깐 멈출 수 있어요..."
            Step(103109, TutorialActionType.Touch, DialogAnchor.Top, highlight: "PauseButton"),

            // 103110: "상단에서는 남은 몬스터 수와 스테이지 진행 시간을 확인할 수 있어요."
            Step(103110, TutorialActionType.Touch, DialogAnchor.Top, highlight: "StageInfoPanel"),

            // === 2 레벨 - 합성 ===
            // 103111: "이번에는 엘렌을 합성해 볼까요?"
            Step(103111, TutorialActionType.DragToPosition, DialogAnchor.Top,
                dragSource: "UnitCard_Ellen", highlight: "UnitCard_Ellen", showHand: true,
                allowedUnits: new[] { "Ellen" },
                conditionKey: "LEVEL_2", delayBefore: 0.5f),

            // 103112: "좋아요! 이제 몬스터를 해치워봐요! 아 참, 타일을 다 채우면 좋은 일이 생긴다고 해요..."
            Step(103112, TutorialActionType.Touch, DialogAnchor.Top),

            // === 컴플리트 버프 ===
            // 103113: "타일을 다 채우면 컴플리트 버프를 받을 수 있어요."
            Step(103113, TutorialActionType.WaitCondition, DialogAnchor.Center,
                conditionKey: "COMPLETE_BUFF"),

            // 103114: "획득한 버프 정보는 별 아이콘을 꾹 눌러 확인할 수 있어요."
            Step(103114, TutorialActionType.Touch, DialogAnchor.Top, highlight: "BuffIcon", isCheckpoint: true),
        };

        SaveAsset(sequence, "Seq_Forced_02_Stage1");
    }

    /// <summary>
    /// 강제 튜토리얼 Part 3: 1 스테이지 클리어 후 (퀘스트/업적)
    /// </summary>
    private static void CreateForcedTutorial_Part3_Stage1Clear()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "forced_03_stage1clear";
        sequence.description = "강제 튜토리얼 Part3 - 1스테이지 클리어 후 퀘스트/업적";
        sequence.triggerType = TutorialTriggerType.OnStageClear;
        sequence.triggerStageId = 1;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // 103115: "모든 몬스터를 해치웠어요! 길드에 새로운 기능이 해금된 것 같은데 로비로 나가 확인해 볼까요?"
            Step(103115, TutorialActionType.TouchTarget, DialogAnchor.Center,
                targetButton: "LobbyButton", highlight: "LobbyButton", showHand: true, isCheckpoint: true),

            // 103116: "메뉴에서 퀘스트와 업적을 확인할 수 있어요."
            Step(103116, TutorialActionType.TouchTarget, DialogAnchor.Bottom,
                targetButton: "MenuButton", highlight: "MenuButton", showHand: true, delayBefore: 0.5f),

            // 103117: "퀘스트는, 매일 또는 매주 갱신되는 미션이에요."
            Step(103117, TutorialActionType.TouchTarget, DialogAnchor.Bottom,
                targetButton: "QuestButton", highlight: "QuestButton", showHand: true),

            // 103118: "완료된 퀘스트가 있네요. 터치하면 보상을 받을 수 있어요."
            Step(103118, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "QuestRewardButton", highlight: "QuestRewardButton", showHand: true, isCheckpoint: true),

            // 103119: "업적에서는 길드장 님이 달성한 도전 과제를 확인할 수 있어요."
            Step(103119, TutorialActionType.TouchTarget, DialogAnchor.Bottom,
                targetButton: "AchievementButton", highlight: "AchievementButton", showHand: true, isCheckpoint: true),

            // 103120: "터치해서 보상을 받아 봐요."
            Step(103120, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "AchievementRewardButton", highlight: "AchievementRewardButton", showHand: true),

            // 103121: "또 몬스터가 몰려오고 있나 봐요."
            Step(103121, TutorialActionType.Touch, DialogAnchor.Bottom, isCheckpoint: true),
        };

        SaveAsset(sequence, "Seq_Forced_03_Stage1Clear");
    }

    /// <summary>
    /// 강제 튜토리얼 Part 4: 2 스테이지
    /// </summary>
    private static void CreateForcedTutorial_Part4_Stage2()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "forced_04_stage2";
        sequence.description = "강제 튜토리얼 Part4 - 2스테이지 (판매/리롤)";
        sequence.triggerType = TutorialTriggerType.OnStageStart;
        sequence.triggerStageId = 2;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // === 2 레벨 - 판매/리롤 설명 ===
            // 103122: "원하는 모험가가 없나요? 배치했던 모험가를 판매하고 얻은 크레딧으로 선택지를 다시 뽑을 수 있어요."
            Step(103122, TutorialActionType.Touch, DialogAnchor.Top, conditionKey: "LEVEL_2", delayBefore: 0.5f),

            // 103123: "모험가는 전투 중에 언제든 판매할 수 있어요. 팔고 싶은 모험가를 드래그하면 판매 아이콘이 뜰 거예요."
            Step(103123, TutorialActionType.Touch, DialogAnchor.Top),

            // 103124: "이제 필요하지 않은 모험가를 하나 판매해 봐요."
            Step(103124, TutorialActionType.DragToPosition, DialogAnchor.Top,
                dragSource: "PlacedUnit", dragTarget: "SellArea", highlight: "SellArea", showHand: true),

            // 103125: "부족한 25 크레딧은 제가 드릴게요. 선택지를 다시 뽑아봐요."
            Step(103125, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "RerollButton", highlight: "RerollButton", showHand: true,
                rewardType: TutorialRewardType.Credit, rewardAmount: 25),

            // 103126: "어떤 선택지도 고르지 않고 스킵 버튼을 누르는 방법으로도 크레딧을 얻을 수 있어요."
            Step(103126, TutorialActionType.Touch, DialogAnchor.Top),

            // === 컴플리트 버프 ===
            // 103127: "이번에도 타일을 다 채우는 데 성공했네요!"
            Step(103127, TutorialActionType.WaitCondition, DialogAnchor.Center,
                conditionKey: "COMPLETE_BUFF", isCheckpoint: true),
        };

        SaveAsset(sequence, "Seq_Forced_04_Stage2");
    }

    /// <summary>
    /// 강제 튜토리얼 Part 5: 2 스테이지 클리어 후 뽑기
    /// </summary>
    private static void CreateForcedTutorial_Part5_Gacha()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "forced_05_gacha";
        sequence.description = "강제 튜토리얼 Part5 - 2스테이지 클리어 후 뽑기";
        sequence.triggerType = TutorialTriggerType.OnStageClear;
        sequence.triggerStageId = 2;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // 103128: "두 번째 승리네요! 이제 슬슬 새로운 모험가를 모집해볼까요?"
            Step(103128, TutorialActionType.TouchTarget, DialogAnchor.Center,
                targetButton: "LobbyButton", highlight: "LobbyButton", showHand: true, isCheckpoint: true),

            // 103129: "뽑기 탭에서 새로운 모험가를 모집할 수 있어요."
            Step(103129, TutorialActionType.TouchTarget, DialogAnchor.Bottom,
                targetButton: "GachaTabButton", highlight: "GachaTabButton", showHand: true, delayBefore: 0.5f),

            // 103130: "이번 계약서는 제가 드릴게요. 1회 뽑기를 돌려봐요."
            Step(103130, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "GachaSingleButton", highlight: "GachaSingleButton", showHand: true,
                rewardType: TutorialRewardType.SummonTicket, rewardAmount: 1, isCheckpoint: true),
        };

        SaveAsset(sequence, "Seq_Forced_05_Gacha");
    }

    #endregion

    #region 3 스테이지 튜토리얼

    private static void CreateStage3Tutorial()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "stage3_tutorial";
        sequence.description = "3 스테이지 튜토리얼 - 스테이지 버프";
        sequence.triggerType = TutorialTriggerType.OnStageStart;
        sequence.triggerStageId = 3;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // 103131: "또 오셨네요, 길드장 님! 이번 전투부터는 타일이 조금 바뀌어요."
            Step(103131, TutorialActionType.Touch, DialogAnchor.Top),

            // 103132: "모험가를 배치해 색이 다른 타일을 채우면 버프를 얻을 수 있어요. 한 번 해볼까요?"
            Step(103132, TutorialActionType.DragToPosition, DialogAnchor.Top,
                highlight: "ColoredTile", showHand: true),

            // 103133: "잘하셨어요! 남은 칸도 채워 봐요."
            Step(103133, TutorialActionType.WaitCondition, DialogAnchor.Top,
                conditionKey: "STAGE_BUFF", isCheckpoint: true),
        };

        SaveAsset(sequence, "Seq_Stage3");
    }

    #endregion

    #region 4 스테이지 튜토리얼

    private static void CreateStage4Tutorial()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "stage4_tutorial";
        sequence.description = "4 스테이지 튜토리얼 - 패시브 스킬";
        sequence.triggerType = TutorialTriggerType.OnStageStart;
        sequence.triggerStageId = 4;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // 103134: "길드장 님, 이번에는 특별한 선택지들이 나온다고 해요! 기대되지 않나요?"
            Step(103134, TutorialActionType.Touch, DialogAnchor.Top),

            // 103135: "하나를 골라 선택해보세요. 전투에 도움이 될 거예요."
            Step(103135, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "PassiveSkillCard", highlight: "PassiveSkillPanel", showHand: true,
                conditionKey: "LEVEL_3", delayBefore: 0.5f, isCheckpoint: true),
        };

        SaveAsset(sequence, "Seq_Stage4");
    }

    #endregion

    #region 유닛 강화 튜토리얼 (5 스테이지 클리어 후)

    private static void CreateEnhanceTutorial()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "enhance_tutorial";
        sequence.description = "유닛 강화 튜토리얼 - 5 스테이지 클리어 후";
        sequence.triggerType = TutorialTriggerType.OnStageClear;
        sequence.triggerStageId = 5;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // 103136: "이제 모험가들의 힘을 강화할 수 있어요."
            Step(103136, TutorialActionType.TouchTarget, DialogAnchor.Center,
                targetButton: "LobbyButton", highlight: "LobbyButton", showHand: true),

            // 103137: "모험가 강화는 모험가 탭에서 할 수 있어요."
            Step(103137, TutorialActionType.TouchTarget, DialogAnchor.Bottom,
                targetButton: "UnitTabButton", highlight: "UnitTabButton", showHand: true, delayBefore: 0.5f),

            // 103138: "여기서 모험가 편성과 강화를 할 수 있어요."
            Step(103138, TutorialActionType.Touch, DialogAnchor.Top),

            // 103139: "지금 길드장 님이 편성할 수 있는 유닛은 두 개 뿐이지만, 3개의 칸을 추가로 구매할 수 있어요."
            Step(103139, TutorialActionType.Touch, DialogAnchor.Top),

            // 103140: "전투에서는 길드장 님이 편성한 유닛에 랜덤으로 편성된 유닛까지 총 10개의 유닛에서 랜덤으로 선택지가 제공돼요."
            Step(103140, TutorialActionType.Touch, DialogAnchor.Top),

            // 103141: "그럼 이제 밀리아를 눌러 강화해 봐요."
            Step(103141, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "UnitSlot_Millia", highlight: "UnitSlot_Millia", showHand: true),

            // 103142: "강화석과 골드를 소모해 유닛을 강화할 수 있어요. 이번에 필요한 재화는 제가 드릴게요. 강화를 눌러 보세요."
            Step(103142, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "EnhanceButton", highlight: "EnhanceButton", showHand: true,
                rewardType: TutorialRewardType.EnhanceStone, rewardAmount: 3),
            // 골드 500도 지급해야 함 - 별도 처리 필요

            // 103143: "모험가 조각으로 영웅 강화를 하면 단계에 따라 특별한 효과를 얻을 수 있어요."
            Step(103143, TutorialActionType.Touch, DialogAnchor.Top),

            // 103144: "앞으로도 강화를 잘 활용하여 몬스터들을 막아주세요!"
            Step(103144, TutorialActionType.Touch, DialogAnchor.Top, isCheckpoint: true),
        };

        SaveAsset(sequence, "Seq_Enhance");
    }

    #endregion

    #region 6 스테이지 튜토리얼

    private static void CreateStage6Tutorial()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "stage6_tutorial";
        sequence.description = "6 스테이지 튜토리얼 - 플레이어 스킬";
        sequence.triggerType = TutorialTriggerType.OnStageStart;
        sequence.triggerStageId = 6;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // 103145: "길드장 님, 저 앞에서 더욱 강력한 힘을 가진 선택지가 느껴져요! 어서 몬스터를 무찌르고 가 봐요!"
            Step(103145, TutorialActionType.Touch, DialogAnchor.Top),

            // 103146: "강력한 스킬 선택지가 나왔어요! 스킬은 신중히 쓰는 게 좋을 거예요."
            Step(103146, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "PlayerSkillCard", highlight: "PlayerSkillPanel", showHand: true,
                conditionKey: "PLAYER_SKILL_SELECTION", isCheckpoint: true),
        };

        SaveAsset(sequence, "Seq_Stage6");
    }

    #endregion

    #region 던전 튜토리얼

    private static void CreateDungeonTutorial()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "dungeon_tutorial";
        sequence.description = "던전 튜토리얼 - 던전 탭 첫 입장 시";
        sequence.triggerType = TutorialTriggerType.OnCondition;
        sequence.triggerConditionKey = "DUNGEON_TAB_FIRST_ENTER";
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // 103147: "필요한 재료를 던전에서 구할 수 있어요."
            Step(103147, TutorialActionType.Touch, DialogAnchor.Top),

            // 103148: "던전은 하루에 3번까지는 무료로 입장할 수 있지만 그 이후에는 열쇠가 필요해요."
            Step(103148, TutorialActionType.Touch, DialogAnchor.Top),

            // 103149: "입장을 눌러 강화석 던전에 들어가 봐요."
            Step(103149, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "DungeonEnterButton", highlight: "DungeonEnterButton", showHand: true),

            // 103150: "던전은 스테이지와 달리 5명의 모험가와 함께하게 돼요."
            Step(103150, TutorialActionType.Touch, DialogAnchor.Bottom, delayBefore: 0.5f),

            // 103151: "광석을 터치해서 부수면 강화석을 획득할 수 있어요."
            Step(103151, TutorialActionType.Touch, DialogAnchor.Top),

            // 103152: "모험가들의 공격력으로 터치 수가 정해져요. 상단에서 남은 터치 수를 확인할 수 있어요."
            Step(103152, TutorialActionType.Touch, DialogAnchor.Top, highlight: "TouchCountUI"),

            // 103153: "그 옆에는 남은 광석 수가 떠요. 광석을 다 캐면 던전 클리어예요!"
            Step(103153, TutorialActionType.Touch, DialogAnchor.Top, highlight: "OreCountUI"),

            // 103154: "그럼 지금부터 광석을 터치해 봐요!"
            Step(103154, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "Ore", highlight: "Ore", showHand: true, isCheckpoint: true),
        };

        SaveAsset(sequence, "Seq_Dungeon");
    }

    #endregion

    #region Helper Methods

    private static TutorialStep Step(
        int stringId,
        TutorialActionType actionType,
        DialogAnchor anchor,
        string targetButton = null,
        string highlight = null,
        bool showHand = false,
        string dragSource = null,
        string dragTarget = null,
        Vector2Int[] allowedTiles = null,
        string[] allowedUnits = null,
        float autoDelay = 0f,
        string conditionKey = null,
        bool isCheckpoint = false,
        float delayBefore = 0f,
        TutorialRewardType rewardType = TutorialRewardType.None,
        int rewardAmount = 0,
        bool pauseGame = true)
    {
        return new TutorialStep
        {
            stringId = stringId,
            voiceKey = stringId > 0 ? $"Tut_{(stringId - 103100):D2}" : "",
            dialogAnchor = anchor,
            showCharacter = stringId > 0,
            actionType = actionType,
            targetButtonName = targetButton,
            highlightTarget = highlight,
            showHandGuide = showHand,
            dragSourceName = dragSource,
            dragTargetName = dragTarget,
            allowedTiles = allowedTiles,
            allowedUnitNames = allowedUnits,
            autoAdvanceDelay = autoDelay,
            conditionKey = conditionKey,
            isCheckpoint = isCheckpoint,
            delayBeforeStep = delayBefore,
            pauseGame = pauseGame,
            reward = new TutorialReward
            {
                rewardType = rewardType,
                amount = rewardAmount
            }
        };
    }

    private static void SaveAsset(TutorialSequence sequence, string fileName)
    {
        string path = $"{FOLDER_PATH}/{fileName}.asset";

        // 기존 에셋이 있으면 삭제
        var existing = AssetDatabase.LoadAssetAtPath<TutorialSequence>(path);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        AssetDatabase.CreateAsset(sequence, path);
        Debug.Log($"[Tutorial] Created: {path}");
    }

    #endregion
}
