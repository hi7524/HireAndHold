using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public int CurrentStageId { get; private set; }
    public StageData CurrentStageData { get; private set; }

    public event Action<int> OnStageStart;
    public event Action<int> OnStageComplete;
    public event Action<int> OnStageFailed;

    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameManager gameManager;

    private async UniTaskVoid Start()
    {
        waveManager.Initialize(gameManager, this);
         if (!DataTableManager.IsInitialized)
        {
            Debug.Log("[StageManager] DataTable 초기화 대기 중...");
            await DataTableManager.InitAsync();
        }
        CurrentStageId = 701; 
        gameManager.OnGameStart += () => StartStage(CurrentStageId);
    }

    
    public void StartStage(int stageId)
    {
       
        CurrentStageData = DataTableManager.StageTable.Get(stageId);
        
        if (CurrentStageData == null)
        {
            Debug.LogError($"[StageManager] 스테이지 {stageId} 데이터를 찾을 수 없습니다!");
            return;
        }

        CurrentStageId = stageId;
        Debug.Log($"[StageManager] 스테이지 {stageId} 시작: {CurrentStageData.STAGE_NAME}");
        Debug.Log($"  - 제한시간: {CurrentStageData.STAGE_TIME_SEC}초");
        Debug.Log($"  - 총 웨이브: {CurrentStageData.TOTAL_WAVE}개");
        Debug.Log($"  - 맵: {CurrentStageData.STAGE_NAME}");

        OnStageStart?.Invoke(stageId);
        waveManager.InitializeWaves(stageId);
        
    }
    public void CompleteStage()
    {
        Debug.Log($"[StageManager] 스테이지 {CurrentStageId} 클리어!");
        Debug.Log($"  - 보상 경험치: {CurrentStageData.STAGE_C_EXP}");
        Debug.Log($"  - 보상 골드: {CurrentStageData.STAGE_C_GOLD}");
        
        OnStageComplete?.Invoke(CurrentStageId);

    }

    public void FailStage()
    {
        Debug.Log($"[StageManager] 스테이지 {CurrentStageId} 실패");
        OnStageFailed?.Invoke(CurrentStageId);
        gameManager.GameEnd();
    }
    private void OnDestroy()
    {
        gameManager.OnGameStart -= () => StartStage(CurrentStageId);
    }
}