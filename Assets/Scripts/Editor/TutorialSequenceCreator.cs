using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Tutorial;

/// <summary>
/// 튜토리얼 시퀀스 에셋 생성 에디터 윈도우
/// </summary>
public class TutorialSequenceCreator : EditorWindow
{
    [MenuItem("Tools/Tutorial/Create Sample Sequences")]
    public static void CreateSampleSequences()
    {
        // 저장 폴더 확인/생성
        string folderPath = "Assets/ScriptableObjects/Tutorial";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Tutorial");
        }

        // 샘플 시퀀스 생성
        CreateForcedTutorialSequence(folderPath);
        CreateStage3TutorialSequence(folderPath);
        CreateStage4TutorialSequence(folderPath);
        CreateEnhanceTutorialSequence(folderPath);
        CreateStage6TutorialSequence(folderPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[TutorialSequenceCreator] 샘플 튜토리얼 시퀀스 5개 생성 완료!");
    }

    /// <summary>
    /// 강제 튜토리얼 (스테이지 1-2)
    /// </summary>
    private static void CreateForcedTutorialSequence(string folderPath)
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "forced_tutorial";
        sequence.description = "강제 튜토리얼 - 스테이지 1-2 (첫 접속 시)";
        sequence.triggerType = TutorialTriggerType.OnLobbyEnter;
        sequence.triggerStageId = 0;
        sequence.canSkip = false;
        sequence.isRequired = true;

        var steps = new List<TutorialStep>
        {
            // 103101: 로비 첫 진입
            CreateStep(103101, TutorialActionType.Touch, DialogAnchor.Bottom),

            // 103102-103104: 스테이지 1 시작 - 배치 설명
            CreateStep(103102, TutorialActionType.Touch, DialogAnchor.Top),
            CreateStep(103103, TutorialActionType.Touch, DialogAnchor.Top),
            CreateStep(103104, TutorialActionType.DragToPosition, DialogAnchor.Top,
                dragSource: "UnitSlot_Ellen", allowedTiles: new Vector2Int[] { new Vector2Int(1, 1) }),

            // 103105-103106: 배치 후
            CreateStep(103105, TutorialActionType.Touch, DialogAnchor.Top),
            CreateStep(103106, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "StartBattleButton", highlightTarget: "StartBattleButton"),

            // 103107-103111: 전투 중 설명
            CreateStep(103107, TutorialActionType.WaitAuto, DialogAnchor.Top, autoDelay: 3f),
            CreateStep(103108, TutorialActionType.Touch, DialogAnchor.Top),
            CreateStep(103109, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "SpeedButton", highlightTarget: "SpeedButton"),
            CreateStep(103110, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "PauseButton", highlightTarget: "PauseButton"),
            CreateStep(103111, TutorialActionType.Touch, DialogAnchor.Top),

            // 103112-103113: 레벨 2 - 합성
            CreateStep(103112, TutorialActionType.DragToPosition, DialogAnchor.Top,
                dragSource: "UnitSlot_Ellen"),
            CreateStep(103113, TutorialActionType.Touch, DialogAnchor.Top, isCheckpoint: true),
        };

        sequence.steps = steps;
        AssetDatabase.CreateAsset(sequence, $"{folderPath}/Sequence_ForcedTutorial.asset");
    }

    /// <summary>
    /// 스테이지 3 튜토리얼
    /// </summary>
    private static void CreateStage3TutorialSequence(string folderPath)
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "stage3_tutorial";
        sequence.description = "스테이지 3 튜토리얼 - 타일 버프";
        sequence.triggerType = TutorialTriggerType.OnStageStart;
        sequence.triggerStageId = 3;
        sequence.canSkip = false;
        sequence.isRequired = true;

        var steps = new List<TutorialStep>
        {
            // 103132-103134: 타일 버프 설명
            CreateStep(103132, TutorialActionType.Touch, DialogAnchor.Top),
            CreateStep(103133, TutorialActionType.DragToPosition, DialogAnchor.Top),
            CreateStep(103134, TutorialActionType.Touch, DialogAnchor.Top, isCheckpoint: true),
        };

        sequence.steps = steps;
        AssetDatabase.CreateAsset(sequence, $"{folderPath}/Sequence_Stage3Tutorial.asset");
    }

    /// <summary>
    /// 스테이지 4 튜토리얼
    /// </summary>
    private static void CreateStage4TutorialSequence(string folderPath)
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "stage4_tutorial";
        sequence.description = "스테이지 4 튜토리얼 - 특별 선택지";
        sequence.triggerType = TutorialTriggerType.OnStageStart;
        sequence.triggerStageId = 4;
        sequence.canSkip = false;
        sequence.isRequired = true;

        var steps = new List<TutorialStep>
        {
            // 103135-103136: 특별 선택지
            CreateStep(103135, TutorialActionType.Touch, DialogAnchor.Top),
            CreateStep(103136, TutorialActionType.TouchTarget, DialogAnchor.Center,
                targetButton: "SelectionPanel", highlightTarget: "SelectionPanel", isCheckpoint: true),
        };

        sequence.steps = steps;
        AssetDatabase.CreateAsset(sequence, $"{folderPath}/Sequence_Stage4Tutorial.asset");
    }

    /// <summary>
    /// 유닛 강화 튜토리얼
    /// </summary>
    private static void CreateEnhanceTutorialSequence(string folderPath)
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "enhance_tutorial";
        sequence.description = "유닛 강화 튜토리얼 - 스테이지 5 클리어 후";
        sequence.triggerType = TutorialTriggerType.OnStageClear;
        sequence.triggerStageId = 5;
        sequence.canSkip = false;
        sequence.isRequired = true;

        var steps = new List<TutorialStep>
        {
            // 103137-103145: 강화 설명
            CreateStep(103137, TutorialActionType.Touch, DialogAnchor.Bottom),
            CreateStep(103138, TutorialActionType.TouchTarget, DialogAnchor.Bottom,
                targetButton: "UnitTabButton", highlightTarget: "UnitTabButton"),
            CreateStep(103139, TutorialActionType.Touch, DialogAnchor.Top),
            CreateStep(103140, TutorialActionType.Touch, DialogAnchor.Top),
            CreateStep(103141, TutorialActionType.Touch, DialogAnchor.Top),
            CreateStep(103142, TutorialActionType.TouchTarget, DialogAnchor.Top,
                targetButton: "MilliaSlot", highlightTarget: "MilliaSlot"),
            CreateStep(103143, TutorialActionType.TouchTarget, DialogAnchor.Center,
                targetButton: "EnhanceButton", highlightTarget: "EnhanceButton",
                rewardType: TutorialRewardType.EnhanceStone, rewardAmount: 3),
            CreateStep(103144, TutorialActionType.Touch, DialogAnchor.Top),
            CreateStep(103145, TutorialActionType.Touch, DialogAnchor.Top, isCheckpoint: true),
        };

        sequence.steps = steps;
        AssetDatabase.CreateAsset(sequence, $"{folderPath}/Sequence_EnhanceTutorial.asset");
    }

    /// <summary>
    /// 스테이지 6 튜토리얼
    /// </summary>
    private static void CreateStage6TutorialSequence(string folderPath)
    {
        var sequence = ScriptableObject.CreateInstance<TutorialSequence>();
        sequence.sequenceId = "stage6_tutorial";
        sequence.description = "스테이지 6 튜토리얼 - 플레이어 스킬";
        sequence.triggerType = TutorialTriggerType.OnStageStart;
        sequence.triggerStageId = 6;
        sequence.canSkip = false;
        sequence.isRequired = true;

        var steps = new List<TutorialStep>
        {
            // 103146-103147: 플레이어 스킬
            CreateStep(103146, TutorialActionType.Touch, DialogAnchor.Top),
            CreateStep(103147, TutorialActionType.TouchTarget, DialogAnchor.Center,
                targetButton: "SkillSelectionPanel", highlightTarget: "SkillSelectionPanel", isCheckpoint: true),
        };

        sequence.steps = steps;
        AssetDatabase.CreateAsset(sequence, $"{folderPath}/Sequence_Stage6Tutorial.asset");
    }

    /// <summary>
    /// TutorialStep 생성 헬퍼
    /// </summary>
    private static TutorialStep CreateStep(
        int stringId,
        TutorialActionType actionType,
        DialogAnchor anchor,
        string targetButton = null,
        string highlightTarget = null,
        string dragSource = null,
        string dragTarget = null,
        Vector2Int[] allowedTiles = null,
        float autoDelay = 0f,
        bool isCheckpoint = false,
        TutorialRewardType rewardType = TutorialRewardType.None,
        int rewardAmount = 0)
    {
        var step = new TutorialStep
        {
            stringId = stringId,
            dialogAnchor = anchor,
            actionType = actionType,
            targetButtonName = targetButton,
            highlightTarget = highlightTarget,
            dragSourceName = dragSource,
            dragTargetName = dragTarget,
            allowedTiles = allowedTiles,
            autoAdvanceDelay = autoDelay,
            isCheckpoint = isCheckpoint,
            showCharacter = true,
            showHandGuide = !string.IsNullOrEmpty(highlightTarget),
            reward = new TutorialReward
            {
                rewardType = rewardType,
                amount = rewardAmount
            }
        };

        return step;
    }
}
