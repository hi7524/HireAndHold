using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 진행 조건 타입
    /// </summary>
    public enum TutorialActionType
    {
        Touch,              // 아무 곳이나 터치하면 다음
        TouchTarget,        // 특정 버튼 터치해야 다음
        DragToPosition,     // 특정 위치로 드래그해야 다음
        WaitAuto,           // N초 후 자동으로 다음
        WaitCondition,      // 특정 조건 만족 시 다음
    }

    /// <summary>
    /// 대화창 앵커 위치
    /// </summary>
    public enum DialogAnchor
    {
        Top,
        Center,
        Bottom,
        Custom,             // dialogPosition 직접 사용
    }

    /// <summary>
    /// 보상 타입
    /// </summary>
    public enum TutorialRewardType
    {
        None,
        Credit,
        Gold,
        Item,
        EnhanceStone,
        SummonTicket,
    }

    /// <summary>
    /// 튜토리얼 보상 데이터
    /// </summary>
    [System.Serializable]
    public class TutorialReward
    {
        public TutorialRewardType rewardType = TutorialRewardType.None;
        public int itemId;          // Item 타입일 때 아이템 ID
        public int amount;          // 보상 수량

        public bool HasReward => rewardType != TutorialRewardType.None && amount > 0;
    }

    /// <summary>
    /// 튜토리얼 개별 스텝 데이터
    /// </summary>
    [System.Serializable]
    public class TutorialStep
    {
        [Header("기본 정보")]
        [Tooltip("StringTable에서 텍스트를 가져올 ID (103101~103147)")]
        public int stringId;

        [Tooltip("이 스텝에서 재생할 보이스 키 (AddressablePreloader에서 로드)")]
        public string voiceKey;

        [Tooltip("스텝 완료 시 지급할 보상")]
        public TutorialReward reward;

        [Header("대화창 설정")]
        [Tooltip("대화창 앵커 위치")]
        public DialogAnchor dialogAnchor = DialogAnchor.Bottom;

        [Tooltip("Custom 앵커일 때 대화창 위치 (캔버스 기준)")]
        public Vector2 dialogPosition;

        [Tooltip("캐릭터 이미지 표시 여부")]
        public bool showCharacter = true;

        [Header("하이라이트 설정")]
        [Tooltip("하이라이트할 UI 오브젝트 이름 (비어있으면 하이라이트 없음)")]
        public string highlightTarget;

        [Tooltip("하이라이트 위치 오프셋")]
        public Vector2 highlightOffset;

        [Tooltip("하이라이트 크기 (0이면 타겟 크기 사용)")]
        public Vector2 highlightSize;

        [Header("두 번째 하이라이트 설정")]
        [Tooltip("두 번째 하이라이트할 UI 오브젝트 이름 (비어있으면 하이라이트 없음)")]
        public string highlightTarget2;

        [Tooltip("두 번째 하이라이트 위치 오프셋")]
        public Vector2 highlightOffset2;

        [Tooltip("두 번째 하이라이트 크기 (0이면 타겟 크기 사용)")]
        public Vector2 highlightSize2;

        [Header("손가락 가이드")]
        [Tooltip("손가락 가이드 표시 여부")]
        public bool showHandGuide;

        [Tooltip("손가락 가이드 위치 오프셋")]
        public Vector2 handGuideOffset;

        [Header("진행 조건")]
        [Tooltip("다음 스텝으로 넘어가는 조건")]
        public TutorialActionType actionType = TutorialActionType.Touch;

        [Tooltip("TouchTarget: 터치해야 하는 버튼 이름")]
        public string targetButtonName;

        [Tooltip("DragToPosition: 드래그 시작 오브젝트 이름")]
        public string dragSourceName;

        [Tooltip("DragToPosition: 드래그 끝 오브젝트 이름")]
        public string dragTargetName;

        [Tooltip("DragToPosition: 허용되는 타일 좌표들")]
        public Vector2Int[] allowedTiles;

        [Tooltip("WaitAuto: 자동 진행까지 대기 시간 (초)")]
        public float autoAdvanceDelay = 2f;

        [Tooltip("WaitCondition: 조건 키 (COMPLETE_BUFF, STAGE_CLEAR 등)")]
        public string conditionKey;

        [Header("특수 설정")]
        [Tooltip("이 스텝에서 게임 일시정지 여부")]
        public bool pauseGame = true;

        [Tooltip("이 스텝이 체크포인트인지 (중간 저장 지점)")]
        public bool isCheckpoint;

        [Tooltip("특정 유닛만 드래그 허용 (비어있으면 제한 없음)")]
        public string[] allowedUnitNames;

        [Tooltip("스텝 시작 전 대기 시간 (초) - UI 활성화 대기용")]
        public float delayBeforeStep = 0f;
    }
}
