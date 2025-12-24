using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 광석 던전 시스템 관리자
/// </summary>
public class OreDungeonManager : MonoBehaviour
{
    [SerializeField] private int CurDungeonIdForTesting = 15101;
    [SerializeField] private OreDungeonAssetManager assetManager;

    public bool IsPreloaded { get; private set; }
    public int CurDungeonID { get; private set; }
    public OreDungeonData DungeonData { get; private set; }
    public int RemainTouchCount { get; private set; }
    public int RemainOreCount { get; private set; }
    public int GetOreAmount { get; private set; }

    public int UnitCount => UnitCountToUse;
    public int ExistingUnitCount { get; private set; } // FillUnitsRandomly 호출 전 유닛 수

    public event Action OnInitialized;            // 초기화 및 데이터 로드 완료 후 실행할 메서드
    public event Action<int> OnTouchCountChanged; // 남은 터치 수 변경
    public event Action<int> OnOreCountChanged;   // 남은 광석 수 변경
    public event Action<bool> OnStageEnded;

    private const int UnitCountToUse = 5;
    public HashSet<int> draftUnitList = new HashSet<int> { 11101, 11312 }; // TODO: 나중에 실제 편성 덱과 연결


    private void Awake()
    {
        // 참조 누락 확인
        if (!ValidateReferences())
            return;
    }

    // AssetManager가 Awake에서 호출하는 초기화 메서드
    // Preload 여부에 따라 사용할 던전 ID를 결정
    public void Initialize(bool isPreloaded)
    {
        IsPreloaded = isPreloaded;
        CurDungeonID = isPreloaded ? PlayData.OreDungeonID : CurDungeonIdForTesting;

        var oreDungeonTable = assetManager.OreDungeonTable;
        DungeonData = oreDungeonTable?.Get(CurDungeonID);

        if (DungeonData != null)
        {
            // 초기 카운트 설정
            RemainTouchCount = CalculateTotalTouchCount();
            RemainOreCount = DungeonData.Number_Of_Ores + DungeonData.Number_Of_Ores2;
        }
        // 초기화 완료 이벤트 발행
        OnInitialized?.Invoke();
    }

    public void OnOreTouched()
    {
        RemainTouchCount--;
        OnTouchCountChanged?.Invoke(RemainTouchCount);

        Debug.Log($"Touch! RemainTouchCount: {RemainTouchCount}");

        // 터치 횟수가 0 이하일 때만 체크
        if (RemainTouchCount <= 0)
        {
            CheckStageEnd();
        }
    }

    public void OnOreDestroyed()
    {
        RemainOreCount--;
        OnOreCountChanged?.Invoke(RemainOreCount);


        // 광석이 모두 파괴되었을 때만 체크
        if (RemainOreCount <= 0)
        {
            CheckStageEnd();
        }
    }

    private void CheckStageEnd()
    {
        // 터치도 남고 광석도 남음 → 계속 진행
        if (RemainTouchCount > 0 && RemainOreCount > 0)
            return;

        // 광석을 모두 파괴했고 터치가 남음 → 성공
        if (RemainOreCount <= 0 && RemainTouchCount >= 0)
        {
            OnStageEnded?.Invoke(true);
        }
        // 터치를 모두 소진했는데 광석이 남음 → 실패
        else if (RemainTouchCount <= 0 && RemainOreCount > 0)
        {
            OnStageEnded?.Invoke(false);
        }
    }

    // 참조 누락 확인
    private bool ValidateReferences()
    {
        if (assetManager == null)
        {
            Debug.LogError($"{nameof(OreDungeonAssetManager)} 참조가 누락되었습니다.");
            return false;
        }

        return true;
    }

    // 총 필요 터치 횟수 계산
    private int CalculateTotalTouchCount()
    {
        if (DungeonData == null)
            return 0;

        // 부족한 유닛을 랜덤으로 채우기
        if (draftUnitList.Count < UnitCountToUse)
            FillUnitsRandomly();

        // 편성된 유닛들의 총 공격력 계산
        float totalAttackPower = 0f;
        foreach (int unitId in draftUnitList)
        {
            var unitData = DataTableManager.UnitTable.Get(unitId);
            if (unitData != null)
            {
                totalAttackPower += unitData.ATTACK;
            }
        }

        if (totalAttackPower <= 0f)
            return 0;

        int touchCount = Mathf.FloorToInt(totalAttackPower / DungeonData.Touch_Attrition_Battle);
        return touchCount;
    }

    // 부족한 유닛을 랜덤으로 채우기
    private void FillUnitsRandomly()
    {
        // FillUnitsRandomly 호출 전 유닛 수 저장
        ExistingUnitCount = draftUnitList.Count;

        if (DataTableManager.UnitTable == null)
            return;

        var allUnitIds = new List<int>(DataTableManager.UnitTable.RawTable.Keys);
        if (allUnitIds.Count == 0)
            return;

        // 부족한 개수만큼 랜덤으로 추가
        int missingCount = UnitCountToUse - draftUnitList.Count;
        for (int i = 0; i < missingCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, allUnitIds.Count);
            int randomUnitId = allUnitIds[randomIndex];

            // 중복 방지를 위해 이미 있는 유닛이면 다시 선택
            int attempts = 0;
            while (draftUnitList.Contains(randomUnitId) && attempts < 100)
            {
                randomIndex = UnityEngine.Random.Range(0, allUnitIds.Count);
                randomUnitId = allUnitIds[randomIndex];
                attempts++;
            }

            draftUnitList.Add(randomUnitId);
        }
    }

    public void AddOreCount(int addAmount = 1)
    {
        GetOreAmount += addAmount;
    }

    /// <summary>
    /// 로비 씬으로 돌아가기
    /// </summary>
    public async void ReturnToLobby()
    {
        Debug.Log($"[OreDungeonManager] 로비 복귀 시작 - IsPreloaded: {IsPreloaded}, GetOreAmount: {GetOreAmount}");

        // 강화석 저장 (성공 시)
        if (IsPreloaded && GetOreAmount > 0)
        {
            await SaveRewardsAsync();
        }
        else
        {
            Debug.LogWarning($"[OreDungeonManager] 강화석 저장 건너뜀 - IsPreloaded: {IsPreloaded}, GetOreAmount: {GetOreAmount}");
        }

        LoadingRequest request = new("Lobby");

        request.AddTask("로비 이동 준비", async (ct) =>
        {
            await UniTask.Delay(300, cancellationToken: ct);
        }, weight: 1.0f);

        request.onLoadingComplete = () =>
        {
            Debug.Log("로비로 이동 완료");
        };

        await LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }

    /// <summary>
    /// 보상 저장 및 사용자 데이터 동기화
    /// </summary>
    private async UniTask SaveRewardsAsync()
    {
        if (DatabaseManager.Instance == null || !DatabaseManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[OreDungeonManager] DatabaseManager가 초기화되지 않아 보상을 저장할 수 없습니다.");
            return;
        }

        try
        {
            // 강화석 저장
            await DatabaseManager.Instance.AddEnhanceStoneAsync(GetOreAmount);
            Debug.Log($"[OreDungeonManager] 강화석 {GetOreAmount}개 저장 완료");

            // 사용자 데이터 다시 로드하여 PlayData 동기화
            await DatabaseManager.Instance.LoadUserDataAsync();
            PlayData.SyncFromDatabase();

            Debug.Log("[OreDungeonManager] 사용자 데이터 동기화 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[OreDungeonManager] 보상 저장 실패: {e.Message}");
        }
    }
}