using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public class UnitGridPreview : MonoBehaviour
{
    [Header("Preview Settings")]
    [SerializeField] private Transform previewTransform;
    [SerializeField] private float cellUISize = 20f;

    private GridPreviewHelper previewHelper;
    private UnitInfoUI unitInfoUI;
    private UnitGridData currentGridData;
    private AsyncOperationHandle<UnitGridData> gridDataHandle;

    private void Awake()
    {
        unitInfoUI = GetComponent<UnitInfoUI>();

        if (unitInfoUI == null)
        {
            Debug.LogError("[UnitGridPreview] UnitInfoUI를 찾을 수 없습니다!");
        }
    }

    private void Start()
    {
        if (unitInfoUI != null)
        {
            // UnitInfoUI가 준비될 때까지 대기 후 그리드 로드
            LoadGridPreviewAsync().Forget();
        }
    }

    private void OnEnable()
    {
        if (unitInfoUI != null)
        {
            // 창이 활성화될 때마다 그리드 업데이트
            LoadGridPreviewAsync().Forget();
        }
    }

    private async UniTaskVoid LoadGridPreviewAsync()
    {
        if (unitInfoUI == null)
            return;

        // UnitInfoUI가 초기화될 때까지 대기
        await unitInfoUI.WaitUntilReady();

        // 현재 유닛의 그리드 데이터 로드
        await UpdateGridPreview();
    }

    /// <summary>
    /// 현재 유닛의 그리드 데이터를 로드하고 미리보기 업데이트
    /// </summary>
    public async UniTask UpdateGridPreview()
    {
        if (unitInfoUI == null)
        {
            Debug.LogWarning("[UnitGridPreview] UnitInfoUI가 null입니다.");
            return;
        }

        // 현재 보고 있는 유닛 가져오기
        var previewUnit = unitInfoUI.GetPreviewUnit();
        if (previewUnit == null)
        {
            Debug.LogWarning("[UnitGridPreview] PreviewUnit이 null입니다.");
            return;
        }

        var unitData = previewUnit.GetUnitData();
        if (unitData == null)
        {
            Debug.LogWarning("[UnitGridPreview] UnitData가 null입니다.");
            return;
        }

        // 그리드 데이터 키
        string gridDataKey = unitData.GRID_DATA;
        if (string.IsNullOrEmpty(gridDataKey))
        {
            Debug.LogWarning($"[UnitGridPreview] GRID_DATA가 비어있습니다. UnitID: {unitData.UNIT_ID}");
            return;
        }

        // 이전 그리드 데이터 해제
        if (gridDataHandle.IsValid())
        {
            Addressables.Release(gridDataHandle);
        }

        // 새로운 그리드 데이터 로드
        gridDataHandle = Addressables.LoadAssetAsync<UnitGridData>(gridDataKey);
        await gridDataHandle.ToUniTask();

        if (gridDataHandle.Status == AsyncOperationStatus.Succeeded)
        {
            currentGridData = gridDataHandle.Result;
            UpdatePreviewVisuals();
        }
        else
        {
            Debug.LogError($"[UnitGridPreview] 그리드 데이터 로드 실패: {gridDataKey}");
        }
    }

    /// <summary>
    /// 그리드 미리보기 시각적 업데이트
    /// </summary>
    private void UpdatePreviewVisuals()
    {
        if (currentGridData == null)
        {
            Debug.LogWarning("[UnitGridPreview] currentGridData가 null입니다.");
            return;
        }

        if (previewTransform == null)
        {
            Debug.LogWarning("[UnitGridPreview] previewTransform이 null입니다.");
            return;
        }

        // GridPreviewHelper 초기화
        if (previewHelper == null)
        {
            previewHelper = new GridPreviewHelper(previewTransform, cellUISize);
        }

        // 프리뷰 생성 또는 업데이트
        if (previewHelper.HasCells)
        {
            previewHelper.UpdatePreview(currentGridData);
        }
        else
        {
            previewHelper.CreatePreview(currentGridData);
        }

        previewHelper.Show();
    }

    /// <summary>
    /// 그리드 프리뷰 표시
    /// </summary>
    public void Show()
    {
        if (previewHelper != null)
        {
            previewHelper.Show();
        }
    }

    /// <summary>
    /// 그리드 프리뷰 숨김
    /// </summary>
    public void Hide()
    {
        if (previewHelper != null)
        {
            previewHelper.Hide();
        }
    }

    /// <summary>
    /// 외부에서 UnitInfoUI 설정 (Inspector에서 설정하지 않은 경우)
    /// </summary>
    public void SetUnitInfoUI(UnitInfoUI unitInfoUI)
    {
        this.unitInfoUI = unitInfoUI;
    }

    /// <summary>
    /// 수동으로 그리드 데이터 설정
    /// </summary>
    public void SetGridData(UnitGridData gridData)
    {
        currentGridData = gridData;
        UpdatePreviewVisuals();
    }

    private void OnDestroy()
    {
        // 프리뷰 정리
        if (previewHelper != null)
        {
            previewHelper.Clear();
        }

        // Addressable 리소스 해제
        if (gridDataHandle.IsValid())
        {
            Addressables.Release(gridDataHandle);
        }
    }
}
