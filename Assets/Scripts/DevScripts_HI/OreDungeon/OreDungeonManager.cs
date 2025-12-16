using UnityEngine;

public class OreDungeonManager : MonoBehaviour
{
    [SerializeField] private OreDungeonAssetManager assetManager;

    public bool IsPreloaded { get; private set; }
    public int CurDungeonID { get; private set; }
    public OreDungeonData DungeonData { get; private set; }

    private void Awake()
    {
        // 참조 누락 확인
        if (!ValidateReferences())
            return;
    }

    // Preloaded 여부 설정
    public void SetIsPreloaded(bool value)
    {
        IsPreloaded = value;

        if (IsPreloaded)
        {
        }
    }

    // 참조 누락 확인
    private bool ValidateReferences()
    {
        if (assetManager == null)
        {
            Debug.LogError($"{assetManager} 참조가 누락되었습니다.");
            return false;
        }

        return true;
    }
}