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

    [MenuItem("Tools/Tutorial/Create All Sequences (기획서 v2.0)")]
    [MenuItem("Tools/Tutorial/Create Sample Sequences")]
    public static void CreateAllSequences()
    {
        EnsureFolder();

        // 1. 로비 튜토리얼 (103101 + 게임시작)
        CreateLobbyTutorial();

        // 2. 1스테이지 인게임 (103102~117)
        CreateStage1Tutorial();

        // 3. 1스테이지 클리어 후 뽑기 (103118~120)
        CreateStage1ClearTutorial();

        // 4. 3스테이지 튜토리얼 (103121~125)
        CreateStage3Tutorial();

        // 5. 강화 튜토리얼 (103126~133)
        CreateEnhanceTutorial();

        // 6. 던전 튜토리얼 (103134~141)
        CreateDungeonTutorial();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[TutorialSequenceCreator] 기획서 v2.0 기준 튜토리얼 시퀀스 생성 완료! (6개)");
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

    #region 1. 로비 튜토리얼

    /// <summary>
    /// 로비 튜토리얼 (103101 + 게임시작)
    /// 로비 첫 입장 시 → 스테이지 선택 → 게임 시작
    /// </summary>
    private static void CreateLobbyTutorial()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "lobby_tutorial";
        sequence.description = "로비 튜토리얼 - 첫 입장 시 게임 시작까지";
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

        SaveAsset(sequence, "New_Seq_Lobby");
    }

    #endregion

    #region 2. 1스테이지 인게임

    /// <summary>
    /// 1스테이지 인게임 튜토리얼 (103102~117)
    /// 배치, 합성, 판매, 리롤, 컴플리트 버프
    /// </summary>
    private static void CreateStage1Tutorial()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "stage1_tutorial";
        sequence.description = "1스테이지 인게임 - 배치~컴플리트 버프";
        sequence.triggerType = TutorialTriggerType.OnStageStart;
        sequence.triggerStageId = 701;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // ========== 1 레벨: 기본 배치 ==========
            // 103102: "비어 있는 타일 위에 모험가를 배치해 몬스터를 공격할 수 있어요."
            Step(103102, TutorialActionType.Touch, DialogAnchor.Top, highlight: "TileGrid", isCheckpoint: true),

            // 103103: "배치할 수 없는 곳이면 타일이 빨갛게, 배치할 수 있으면 초록색으로 보일 거예요."
            Step(103103, TutorialActionType.Touch, DialogAnchor.Top),

            // 103104: "엘렌을 타일 위로 드래그 해 배치해 보세요."
            // Ellen 1성 UnitId = 11113
            Step(103104, TutorialActionType.DragToPosition, DialogAnchor.Top,
                highlightUnitId: 11113, showHand: true,
                allowedUnitIds: new[] { 11113 },
                allowedTiles: new[] { new Vector2Int(1, 1) }),

            // 103105: "타일 위의 모험가는 언제든 원하는 위치로 이동시킬 수 있어요."
            Step(103105, TutorialActionType.Touch, DialogAnchor.Top),

            // 103106: "배치된 모험가는 자동으로 몬스터를 공격해요..."
            Step(103106, TutorialActionType.Touch, DialogAnchor.Top, highlight: "ExpBar"),

            // 103107: "몬스터가 방벽에 다다르면 방벽의 내구도가 깎여요..."
            Step(103107, TutorialActionType.Touch, DialogAnchor.Top, highlight: "HealthBar"),

            // 103108: "상단에서는 남은 몬스터 수와 스테이지 진행 시간을 확인할 수 있어요."
            Step(103108, TutorialActionType.Touch, DialogAnchor.Top, highlight: "StageInfoPanel", isCheckpoint: true),

            // ========== 2 레벨: 합성 ==========
            // 103109: "같은 성급의 같은 모험가 두 개를 합성하면 높은 성급의 모험가를 얻을 수 있어요..."
            // Ellen 1성 UnitId = 11113
            Step(103109, TutorialActionType.DragToPosition, DialogAnchor.Top,
                highlightUnitId: 11113, showHand: true,
                allowedUnitIds: new[] { 11113 },
                allowedTiles: new[] { new Vector2Int(1, 1) },
                conditionKey: "LEVEL_2", delayBefore: 0.5f, isCheckpoint: true),

            // ========== 4 레벨: 판매/리롤 ==========
            // 103110: "원하는 모험가가 없나요?..."
            Step(103110, TutorialActionType.Touch, DialogAnchor.Top,
                conditionKey: "LEVEL_4", delayBefore: 0.5f),

            // 103111: "모험가는 전투 중에 언제든 판매할 수 있어요..."
            Step(103111, TutorialActionType.Touch, DialogAnchor.Top),

            // 103112: "이제 필요하지 않은 모험가를 하나 판매해 봐요."
            // dragSource: null = 어떤 유닛이든 드래그 허용
            Step(103112, TutorialActionType.DragToPosition, DialogAnchor.Top,
                dragTarget: "SellArea", highlight: "SellArea", showHand: true),

            // 103113: "부족한 25 크레딧은 제가 드릴게요. 카드를 다시 뽑아봐요."
            Step(103113, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "RerollButton", highlight: "RerollButton", showHand: true,
                rewardType: TutorialRewardType.Credit, rewardAmount: 25, isCheckpoint: true),

            // 103114: "어떤 선택지도 고르지 않고 완료 버튼을 누르는 방법으로도 크레딧을 얻을 수 있어요."
            Step(103114, TutorialActionType.Touch, DialogAnchor.Top),

            // 103115: "계속해서 몬스터를 해치워봐요!..."
            Step(103115, TutorialActionType.Touch, DialogAnchor.Top, isCheckpoint: true),

            // ========== 컴플리트 버프 ==========
            // 103116: "타일을 다 채우면 컴플리트 버프를 받을 수 있어요."
            Step(103116, TutorialActionType.Touch, DialogAnchor.Center),

            // (조건 대기) 컴플리트 버프 발동까지 대기
            Step(0, TutorialActionType.WaitCondition, DialogAnchor.Center,
                conditionKey: "COMPLETE_BUFF"),

            // 103117: "획득한 버프 정보는 별 아이콘을 꾹 눌러 확인할 수 있어요."
            Step(103117, TutorialActionType.Touch, DialogAnchor.Top, highlight: "BuffIcon", isCheckpoint: true),
        };

        SaveAsset(sequence, "New_Seq_Stage1");
    }

    #endregion

    #region 3. 1스테이지 클리어 후 뽑기

    /// <summary>
    /// 1스테이지 클리어 후 뽑기 튜토리얼 (103118~120)
    /// 로비 이동 → 뽑기 탭 → 1회 뽑기
    /// </summary>
    private static void CreateStage1ClearTutorial()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "stage1_clear_tutorial";
        sequence.description = "1스테이지 클리어 후 - 뽑기";
        sequence.triggerType = TutorialTriggerType.OnStageClear;
        sequence.triggerStageId = 701;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // 103118: "이제 슬슬 새로운 모험가를 모집해볼까요?"
            Step(103118, TutorialActionType.TouchTarget, DialogAnchor.Center,
                targetButton: "LobbyButton", highlight: "LobbyButton", showHand: true, isCheckpoint: true),

            // 103119: "뽑기 탭에서 새로운 모험가를 모집할 수 있어요."
            Step(103119, TutorialActionType.TouchTarget, DialogAnchor.Bottom,
                targetButton: "GachaTabButton", highlight: "GachaTabButton", showHand: true, delayBefore: 0.5f),

            // 103120: "이번 계약서는 제가 드릴게요. 1회 뽑기를 돌려봐요."
            Step(103120, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "GachaSingleButton", highlight: "GachaSingleButton", showHand: true,
                rewardType: TutorialRewardType.Item, rewardItemId: 5102, rewardAmount: 1, isCheckpoint: true),
        };

        SaveAsset(sequence, "New_Seq_Stage1_Clear");
    }

    #endregion

    #region 4. 3스테이지 튜토리얼

    /// <summary>
    /// 3스테이지 튜토리얼 (103121~125)
    /// 스테이지 버프 + 패시브 스킬 + 플레이어 스킬
    /// </summary>
    private static void CreateStage3Tutorial()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "stage3_tutorial";
        sequence.description = "3스테이지 튜토리얼 - 버프 + 패시브 + 플레이어 스킬";
        sequence.triggerType = TutorialTriggerType.OnStageStart;
        sequence.triggerStageId = 703;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // ========== 스테이지 버프 ==========
            // 103121: "또 오셨네요, 길드장 님! 이번 전투부터는 타일이 조금 바뀌어요."
            Step(103121, TutorialActionType.Touch, DialogAnchor.Top, isCheckpoint: true),

            // 103122: "모험가를 배치해 색이 다른 타일을 채우면 버프를 얻을 수 있어요. 한 번 해볼까요?"
            Step(103122, TutorialActionType.DragToPosition, DialogAnchor.Top,
                highlight: "ColoredTile", showHand: true),

            // 103123: "잘하셨어요! 남은 칸도 채워 봐요."
            Step(103123, TutorialActionType.WaitCondition, DialogAnchor.Top,
                conditionKey: "STAGE_BUFF", isCheckpoint: true),

            // ========== 3레벨: 패시브 스킬 ==========
            // 103124: "하나를 골라 선택해보세요. 전투에 도움이 될 거예요."
            Step(103124, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "PassiveSkillCard", highlight: "PassiveSkillPanel", showHand: true,
                conditionKey: "LEVEL_3", delayBefore: 0.5f, isCheckpoint: true),

            // ========== 플레이어 스킬 ==========
            // 103125: "강력한 스킬 선택지가 나왔어요! 스킬은 신중히 쓰는 게 좋을 거예요."
            Step(103125, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "PlayerSkillCard", highlight: "PlayerSkillPanel", showHand: true,
                conditionKey: "PLAYER_SKILL_SELECTION", isCheckpoint: true),
        };

        SaveAsset(sequence, "New_Seq_Stage3");
    }

    #endregion

    #region 5. 강화 튜토리얼

    /// <summary>
    /// 강화 튜토리얼 (103126~133)
    /// 3스테이지 클리어 후 유닛 강화
    /// </summary>
    private static void CreateEnhanceTutorial()
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "enhance_tutorial";
        sequence.description = "강화 튜토리얼 - 3스테이지 클리어 후";
        sequence.triggerType = TutorialTriggerType.OnStageClear;
        sequence.triggerStageId = 703;
        sequence.canSkip = false;
        sequence.isRequired = true;
        sequence.priority = 0;

        sequence.steps = new List<TutorialStep>
        {
            // 103126: "이제 모험가들의 힘을 강화할 수 있어요."
            Step(103126, TutorialActionType.TouchTarget, DialogAnchor.Center,
                targetButton: "LobbyButton", highlight: "LobbyButton", showHand: true, isCheckpoint: true),

            // 103127: "모험가 탭에서 편성과 강화를 할 수 있어요."
            Step(103127, TutorialActionType.TouchTarget, DialogAnchor.Bottom,
                targetButton: "UnitTabButton", highlight: "UnitTabButton", showHand: true, delayBefore: 0.5f),

            // 103128: "지금 길드장 님이 편성할 수 있는 유닛은 두 개 뿐이지만..."
            Step(103128, TutorialActionType.Touch, DialogAnchor.Top),

            // 103129: "전투에서는 길드장 님이 편성한 유닛에 랜덤으로..."
            Step(103129, TutorialActionType.Touch, DialogAnchor.Top),

            // 103130: "그럼 이제 밀리아를 눌러 강화해 봐요."
            Step(103130, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "UnitSlot_Millia", highlight: "UnitSlot_Millia", showHand: true),

            // (UI) 일반강화 버튼 터치
            Step(0, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "NormalEnhanceButton", highlight: "NormalEnhanceButton", showHand: true),

            // 103131: "강화석과 골드를 소모해 유닛을 강화할 수 있어요..."
            Step(103131, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "EnhanceConfirmButton", highlight: "EnhanceConfirmButton", showHand: true,
                rewardType: TutorialRewardType.EnhanceStone, rewardAmount: 3),

            // 103132: "모험가 조각으로 영웅 강화를 하면..."
            Step(103132, TutorialActionType.Touch, DialogAnchor.Top),

            // 103133: "앞으로도 강화를 잘 활용하여 몬스터들을 막아주세요!"
            Step(103133, TutorialActionType.Touch, DialogAnchor.Top, isCheckpoint: true),
        };

        SaveAsset(sequence, "New_Seq_Enhance");
    }

    #endregion

    #region 6. 던전 튜토리얼

    /// <summary>
    /// 던전 튜토리얼 (103134~141)
    /// 던전 탭 첫 입장 + 던전 스테이지 진행
    /// </summary>
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
            // ========== 던전 탭 ==========
            // 103134: "던전에서는 필요한 재료를 구할 수 있어요."
            Step(103134, TutorialActionType.Touch, DialogAnchor.Top, isCheckpoint: true),

            // 103135: "던전은 하루에 3번까지는 무료로 입장할 수 있지만..."
            Step(103135, TutorialActionType.Touch, DialogAnchor.Top),

            // 103136: "입장을 눌러 강화석 던전에 들어가 봐요."
            Step(103136, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "DungeonEnterButton", highlight: "DungeonEnterButton", showHand: true),

            // ========== 던전 스테이지 ==========
            // 103137: "던전은 스테이지와 달리 5명의 모험가와 함께하게 돼요."
            Step(103137, TutorialActionType.Touch, DialogAnchor.Bottom, delayBefore: 0.5f, isCheckpoint: true),

            // 103138: "광석을 터치해서 부수면 강화석을 획득할 수 있어요."
            Step(103138, TutorialActionType.Touch, DialogAnchor.Top),

            // 103139: "모험가들의 공격력으로 터치 수가 정해져요..."
            Step(103139, TutorialActionType.Touch, DialogAnchor.Top, highlight: "TouchCountUI"),

            // 103140: "그 옆에는 남은 광석 수가 떠요..."
            Step(103140, TutorialActionType.Touch, DialogAnchor.Top, highlight: "OreCountUI"),

            // 103141: "그럼 지금부터 광석을 터치해 봐요!"
            Step(103141, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "Ore", highlight: "Ore", showHand: true, isCheckpoint: true),
        };

        SaveAsset(sequence, "New_Seq_Dungeon");
    }

    #endregion

    #region Helper Methods

    private static TutorialStep Step(
        int stringId,
        TutorialActionType actionType,
        DialogAnchor anchor,
        string targetButton = null,
        string highlight = null,
        int highlightUnitId = 0,
        Vector2Int[] highlightTiles = null,
        bool showHand = false,
        string dragSource = null,
        string dragTarget = null,
        Vector2Int[] allowedTiles = null,
        int[] allowedUnitIds = null,
        float autoDelay = 0f,
        string conditionKey = null,
        bool isCheckpoint = false,
        float delayBefore = 0f,
        TutorialRewardType rewardType = TutorialRewardType.None,
        int rewardItemId = 0,
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
            highlightUnitId = highlightUnitId,
            highlightTilePositions = highlightTiles,
            showHandGuide = showHand,
            dragSourceName = dragSource,
            dragTargetName = dragTarget,
            allowedTiles = allowedTiles,
            allowedUnitIds = allowedUnitIds,
            autoAdvanceDelay = autoDelay,
            conditionKey = conditionKey,
            isCheckpoint = isCheckpoint,
            delayBeforeStep = delayBefore,
            pauseGame = pauseGame,
            reward = new TutorialReward
            {
                rewardType = rewardType,
                itemId = rewardItemId,
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
