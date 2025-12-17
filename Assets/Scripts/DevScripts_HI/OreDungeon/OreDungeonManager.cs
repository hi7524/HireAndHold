using System;
using UnityEngine;

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

    public event Action OnInitialized; // 초기화 및 데이터 로드 완료 후 실행할 메서드


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

        Debug.Log($"OreDungeonManager 초기화 완료 - ID: {CurDungeonID}, Preloaded: {IsPreloaded}, DungeonData: {(DungeonData != null ? "로드 성공" : "로드 실패")}");
        
        // 초기화 완료 이벤트 발행
        OnInitialized?.Invoke();
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
}