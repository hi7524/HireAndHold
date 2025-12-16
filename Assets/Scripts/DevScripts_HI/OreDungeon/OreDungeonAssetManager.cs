using UnityEngine;

/// <summary>
/// 광석 던전 에셋 관리
/// AddressablePreloader 유무에 따라 캐시 참조 또는 직접 로드를 분기 처리
/// 정상 플로우와 에디터 테스트 양쪽을 지원
/// </summary>
public class OreDungeonAssetManager : MonoBehaviour
{
    [SerializeField] private OreDungeonManager manager;

    private void Awake()
    {
        // 참조 누락 확인
        if (!ValidateReferences()) 
            return;

        // AddressablePreloader가 존재하면 캐싱된 데이터 사용, 없으면 직접 로드
        if (AddressablePreloader.Instance != null && AddressablePreloader.Instance.IsLoaded)
            LoadFromCache();
        else
            LoadDirectly();
    }

    private void LoadFromCache()
    {
        Debug.Log("로드된 데이터 사용");
        manager.SetIsPreloaded(true);
    }

    private void LoadDirectly()
    {
        Debug.Log("직접 로드해서 사용");
        manager.SetIsPreloaded(false);
    }

    // 참조 누락 확인
    private bool ValidateReferences()
    {
        if (manager == null)
        {
            Debug.LogError($"{manager} 참조가 누락되었습니다.");
            return false;
        }
        
        return true;
    }
}