using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GridState
{
    Empty = 0,
    Occupied = 1,
    Unavailable = -1,
}

public class GridManager : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField] private StageManager stageManager;
    [SerializeField] private BattleUnitManager unitManager;
    [SerializeField] private GridVisualizer gridVisualizer;
    [SerializeField] private BuffManager buffManager;
    [SerializeField] private AttackTriggerZone attackTriggerZone;
    [SerializeField] private LevelUpRewardController levelUpRewardController;

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float cellSpace = 0.1f;
    [SerializeField] private GameObject gridUnitPrefab;

    [Header("Merge Effect")]
    [SerializeField] private GameObject[] mergeEffectPrefabs; // 0: 2성, 1: 3성

    public int[,] GridArray { get; private set; }
    public GridLayoutData LayoutData { get; private set; }
    public bool IsInitialized { get; private set; } = false;
    public float CellSize => cellSize;
    public float CellSpace => cellSpace;
    public int GridUnitCount => gridUnitCount;
    public bool IsLastUnitOnGrid => gridUnitCount <= 1;

    private const string DEFAULT_GRID_LAYOUT_KEY = "StageGridLayout_00";
    private int gridUnitCount = 0;
    private Dictionary<int, Queue<GameObject>> mergeEffectPools = new Dictionary<int, Queue<GameObject>>();
    private Transform effectParent;


    private void Awake()
    {
        InitializeMergeEffectPools();
    }

    private void Start()
    {
        // 스테이지에 맞는 GridLayoutData 설정 (StageManager의 Start 이후에 실행되도록)
        SetGridData();

        InitializeGridArrays();

        // GridVisualizer에게 그리드 생성 요청
        if (gridVisualizer != null && LayoutData != null)
        {
            gridVisualizer.VisualizeGridData(LayoutData);
        }

        // Start에서 초기화하여 모든 Awake가 완료된 후 실행되도록 보장
        EnsureInitialized();
    }

    // 외부에서 초기화 상태 보장을 위해 호출 가능
    public void EnsureInitialized()
    {
        if (IsInitialized)
            return;

        RegisterCells();
        UpdateBuffColors();

        // GridCell Collider2D가 Physics2D에 등록되도록 동기화 (씬 전환 시 감지 문제 해결)
        Physics2D.SyncTransforms();

        IsInitialized = true;
    }

    public void SetGridData()
    {
        string gridLayoutKey;

        if (stageManager == null)
        {
            gridLayoutKey = DEFAULT_GRID_LAYOUT_KEY;
            Debug.LogWarning($"StageManager가 할당되지 않아 기본 레이아웃을 사용합니다: {gridLayoutKey}");
        }
        else
        {
            int currentStageId = stageManager.CurrentStageId;
            StageData stageData = DataTableManager.StageTable.Get(currentStageId);

            if (stageData == null)
            {
                gridLayoutKey = DEFAULT_GRID_LAYOUT_KEY;
                Debug.LogWarning($"Stage ID {currentStageId}에 해당하는 데이터를 찾을 수 없어 기본 레이아웃을 사용합니다: {gridLayoutKey}");
            }
            else
            {
                gridLayoutKey = stageData.STAGE_GRID;
                if (string.IsNullOrEmpty(gridLayoutKey))
                {
                    gridLayoutKey = DEFAULT_GRID_LAYOUT_KEY;
                    Debug.LogWarning($"Stage ID {currentStageId}의 STAGE_GRID 값이 비어있어 기본 레이아웃을 사용합니다: {gridLayoutKey}");
                }
            }
        }

        // AddressablePreloader에서 GridLayoutData 가져오기 시도
        if (AddressablePreloader.Instance != null)
        {
            GridLayoutData newLayoutData = AddressablePreloader.Instance.GetCachedGridLayout(gridLayoutKey);
            if (newLayoutData != null)
            {
                LayoutData = newLayoutData;
                Debug.Log($"GridLayoutData 설정 완료: {gridLayoutKey}");
                return;
            }
        }

        // Preloader가 없거나 데이터를 찾을 수 없을 경우 기본 레이아웃 생성
        Debug.LogWarning($"AddressablePreloader를 통해 GridLayoutData를 가져올 수 없어 기본 레이아웃을 생성합니다.");
        LayoutData = CreateDefaultGridLayout();
    }

    // 기본 그리드 레이아웃 생성 (7x4 그리드)
    private GridLayoutData CreateDefaultGridLayout()
    {
        GridLayoutData defaultLayout = ScriptableObject.CreateInstance<GridLayoutData>();
        defaultLayout.width = 7;
        defaultLayout.height = 4;

        // 모든 셀을 유효한 셀로 설정 (bool 배열)
        int totalCells = defaultLayout.width * defaultLayout.height;
        defaultLayout.validCells = new bool[totalCells];
        for (int i = 0; i < totalCells; i++)
        {
            defaultLayout.validCells[i] = true;
        }

        // 버프 비활성화
        defaultLayout.enableCrossBuffs = false;
        defaultLayout.enableRegionBuffs = false;
        defaultLayout.crossBuffs = new List<CrossBuffData>();
        defaultLayout.regionBuffs = new List<RegionBuffData>();

        Debug.Log("기본 3x3 그리드 레이아웃 생성 완료");
        return defaultLayout;
    }

    // 그리드 배열 초기화 (셀 등록 전)
    private void InitializeGridArrays()
    {
        GridArray = new int[LayoutData.width, LayoutData.height];
    }

    // 그리드 셀 상태 설정 (Empty, Occupied, Unavailable)
    public void SetGridState(Vector2Int pos, GridState state)
    {
        GridArray[pos.x, pos.y] = (int)state;

        GridCell cell = gridVisualizer.GetGridCellAt(pos);
        if (cell != null)
        {
            cell.SetAcceptable(state == GridState.Empty);
        }

        if (buffManager != null)
            buffManager.CheckAllBuffConditions();
    }

    // GridVisualizer의 자식 오브젝트들을 순회하며 GridCell 컴포넌트 수집 및 등록
    private void RegisterCells()
    {
        if (gridVisualizer == null)
        {
            Debug.LogWarning("GridVisualizer가 할당되지 않았습니다.");
            return;
        }

        foreach (Transform child in gridVisualizer.transform)
        {
            var cell = child.GetComponent<GridCell>();
            if (cell != null)
            {
                RegisterCell(cell);
            }
        }
    }

    // 개별 셀을 그리드 배열에 등록
    private void RegisterCell(GridCell cell)
    {
        cell.SetGridManager(this);
    }

    // 유닛 배치 가능 여부 확인 및 시각적 피드백 제공
    public bool CanPlaceUnit(Vector2Int curPos, List<Vector2Int> grid)
    {
        gridVisualizer.ClearAllGridsColor();
        gridVisualizer.ChangeOccupiedCellColor();

        HashSet<Vector2Int> allPositions = GetAbsolutePositions(curPos, grid);
        bool canPlace = ValidateAllPositions(allPositions);

        gridVisualizer.HighlightCells(allPositions, canPlace);

        return canPlace;
    }

    // 현재 위치와 상대 위치를 합쳐 절대 위치 목록 생성
    private HashSet<Vector2Int> GetAbsolutePositions(Vector2Int curPos, List<Vector2Int> grid)
    {
        HashSet<Vector2Int> allPositions = new HashSet<Vector2Int> { curPos };

        foreach (var relativePos in grid)
        {
            allPositions.Add(curPos + relativePos);
        }

        return allPositions;
    }

    // 모든 위치가 배치 가능한지 검증
    private bool ValidateAllPositions(HashSet<Vector2Int> positions)
    {
        bool canPlace = true;

        foreach (var pos in positions)
        {
            if (!IsValidPosition(pos))
            {
                canPlace = false;
            }
        }

        return canPlace;
    }

    // 위치가 유효한지 확인 (범위 내, 유효한 셀, 비어있음)
    private bool IsValidPosition(Vector2Int pos)
    {
        if (!IsWithinBounds(pos))
            return false;

        if (!LayoutData.IsValidCell(pos))
            return false;

        int cellState = GridArray[pos.x, pos.y];
        return cellState == (int)GridState.Empty;
    }

    // 위치가 그리드 범위 내에 있는지 확인
    private bool IsWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < LayoutData.width &&
               pos.y >= 0 && pos.y < LayoutData.height;
    }

    // 특정 그리드 위치의 GridCell 반환
    public GridCell GetGridCellAt(Vector2Int pos)
    {
        return gridVisualizer.GetGridCellAt(pos);
    }

    // 월드 좌표를 가장 가까운 그리드 위치로 변환
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        // GridCell들의 위치를 기준으로 가장 가까운 셀 찾기
        float minDistance = float.MaxValue;
        Vector2Int closestGridPos = Vector2Int.zero;

        for (int x = 0; x < LayoutData.width; x++)
        {
            for (int y = 0; y < LayoutData.height; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                GridCell cell = gridVisualizer.GetGridCellAt(gridPos);
                if (cell != null)
                {
                    float distance = Vector3.Distance(worldPos, cell.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestGridPos = gridPos;
                    }
                }
            }
        }

        return closestGridPos;
    }

    // 버프 영역의 색상 업데이트
    public void UpdateBuffColors()
    {
        gridVisualizer.UpdateBuffColors();
    }

    // 버프 활성화 이펙트 재생
    public void PlayBuffActivationEffect(string buffName)
    {
        List<Vector2Int> affectedCells = GetBuffAffectedCells(buffName);
        gridVisualizer.PlayBuffActivationEffect(affectedCells);
    }

    // 버프가 영향을 주는 셀 목록 가져오기
    private List<Vector2Int> GetBuffAffectedCells(string buffName)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        // 크로스 버프 체크
        if (LayoutData.enableCrossBuffs)
        {
            // 해당 buffName과 정확히 일치하는 crossBuff만 찾아서 처리
            foreach (var crossBuff in LayoutData.crossBuffs)
            {
                bool isHorizontalMatch = crossBuff.horizontalBuffName == buffName;
                bool isVerticalMatch = crossBuff.verticalBuffName == buffName;

                if (isHorizontalMatch)
                {
                    // 가로줄 셀 추가
                    for (int x = 0; x < LayoutData.width; x++)
                    {
                        Vector2Int pos = new Vector2Int(x, crossBuff.centerPos.y);
                        if (LayoutData.IsValidCell(pos))
                        {
                            cells.Add(pos);
                        }
                    }
                    return cells; // 찾았으므로 바로 반환
                }

                if (isVerticalMatch)
                {
                    // 세로줄 셀 추가
                    for (int y = 0; y < LayoutData.height; y++)
                    {
                        Vector2Int pos = new Vector2Int(crossBuff.centerPos.x, y);
                        if (LayoutData.IsValidCell(pos))
                        {
                            cells.Add(pos);
                        }
                    }
                    return cells; // 찾았으므로 바로 반환
                }
            }
        }

        // 리전 버프 체크
        if (LayoutData.enableRegionBuffs)
        {
            foreach (var regionBuff in LayoutData.regionBuffs)
            {
                if (regionBuff.buffName == buffName)
                {
                    cells.AddRange(regionBuff.regionCells);
                }
            }
        }

        // 전체 그리드 버프 체크
        if (buffName == "FullGrid")
        {
            for (int x = 0; x < LayoutData.width; x++)
            {
                for (int y = 0; y < LayoutData.height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (LayoutData.IsValidCell(pos))
                    {
                        cells.Add(pos);
                    }
                }
            }
        }

        return cells;
    }

    // 머지 이펙트 풀 초기화
    private void InitializeMergeEffectPools()
    {
        if (mergeEffectPrefabs == null || mergeEffectPrefabs.Length == 0)
            return;

        // 이펙트를 담을 부모 오브젝트 생성
        GameObject parentObj = new GameObject("MergeEffects");
        effectParent = parentObj.transform;
        effectParent.SetParent(transform);

        // 각 성급별로 풀 생성 (2성, 3성)
        for (int i = 0; i < mergeEffectPrefabs.Length; i++)
        {
            if (mergeEffectPrefabs[i] == null)
                continue;

            Queue<GameObject> pool = new Queue<GameObject>();

            // 각 성급별로 2개씩 미리 생성
            for (int j = 0; j < 2; j++)
            {
                GameObject effect = Instantiate(mergeEffectPrefabs[i], effectParent);
                effect.SetActive(false);
                pool.Enqueue(effect);
            }

            mergeEffectPools[i] = pool;
        }
    }

    // 머지 이펙트 재생
    public void PlayMergeEffect(Vector3 position, int newStarLevel)
    {
        if (mergeEffectPrefabs == null || mergeEffectPrefabs.Length == 0)
            return;

        int index = newStarLevel - 2; // 2성=0, 3성=1
        if (index >= 0 && index < mergeEffectPrefabs.Length && mergeEffectPools.ContainsKey(index))
        {
            GameObject effect = GetOrCreateEffect(index);
            if (effect != null)
            {
                effect.transform.position = position;
                effect.SetActive(true);
                StartCoroutine(ReturnEffectToPool(effect, index, 2f));
            }
        }
    }

    // 풀에서 이펙트 가져오기 또는 새로 생성
    private GameObject GetOrCreateEffect(int index)
    {
        if (mergeEffectPools[index].Count > 0)
        {
            // 풀에서 가져오기
            GameObject effect = mergeEffectPools[index].Dequeue();
            return effect;
        }
        else
        {
            // 풀이 비었으면 새로 생성
            GameObject effect = Instantiate(mergeEffectPrefabs[index], effectParent);
            effect.SetActive(false);
            return effect;
        }
    }

    // 이펙트를 풀로 반환
    private IEnumerator ReturnEffectToPool(GameObject effect, int index, float delay)
    {
        yield return new WaitForSeconds(delay);

        effect.SetActive(false);
        mergeEffectPools[index].Enqueue(effect);
    }

    // 모든 하이라이트된 셀의 색상 초기화
    public void ClearAllGridsColor()
    {
        gridVisualizer.ClearAllGridsColor();
    }

    // 유닛이 차지하는 모든 셀에 지정된 색상 적용
    public void SetUnitCellsColor(Vector2Int curPos, List<Vector2Int> grid, Color color)
    {
        HashSet<Vector2Int> allPositions = GetAbsolutePositions(curPos, grid);
        gridVisualizer.SetUnitCellsColor(allPositions, color);
    }

    // 점유된 셀과 색상 정보 등록
    public void SetOccupiedCellAndColor(Vector2Int pos, Color color)
    {
        gridVisualizer.SetOccupiedCellAndColor(pos, color);
    }

    // 유닛이 차지하던 셀의 색상 정보 제거 및 색상 초기화
    public void RemoveColoredCells(Vector2Int curPos, List<Vector2Int> grid)
    {
        gridVisualizer.RemoveCellColor(curPos);

        foreach (var relativePos in grid)
        {
            Vector2Int absolutePos = curPos + relativePos;
            gridVisualizer.RemoveCellColor(absolutePos);
        }
    }

    // 점유된 모든 셀에 등록된 색상 다시 적용
    public void ChangeOccupiedCellColor()
    {
        gridVisualizer.ChangeOccupiedCellColor();
    }

    // 현재 색상 상태를 임시 저장 (실패 시 복원용)
    public void CopyColoredCellToTemp()
    {
        gridVisualizer.CopyColoredCellToTemp();
    }

    // 배치 실패 시 임시 저장된 색상 상태로 복원
    public void OnFailed()
    {
        gridVisualizer.OnFailed();
    }

    // 그리드 유닛 생성 및 초기화
    public GridUnit SpawnGridUnit(Vector3 position, int unitId, UnitGridData gridData)
    {
        if (gridUnitPrefab == null)
        {
            Debug.LogWarning("GridUnitPrefab이 할당되지 않았습니다.");
            return null;
        }

        GameObject unitObj = Instantiate(gridUnitPrefab, position, Quaternion.identity);

        Unit unit = unitObj.GetComponent<Unit>();
        GridUnit gridUnit = unitObj.GetComponent<GridUnit>();

        if (gridUnit == null)
        {
            Debug.LogWarning("GridUnit 컴포넌트를 찾을 수 없습니다.");
            Destroy(unitObj);
            return null;
        }

        gridUnit.SetGridData(gridData);

        // BattleUnitManager가 있을 경우에만 설정
        if (unitManager != null)
        {
            gridUnit.SetBattleUnitManager(unitManager);
        }

        if (unit != null)
        {
            unit.SetUnitID(unitId);

            // BattleUnitManager가 있을 경우에만 등록
            if (unitManager != null)
            {
                unitManager.RegisterUnit(unit);
            }

            // AttackTriggerZone 연결
            if (attackTriggerZone != null)
            {
                unit.SetAttackZone(attackTriggerZone);
            }
        }

        return gridUnit;
    }

    // 레벨업 보상 유닛이 생성되었음을 알림
    public void NotifyLevelUpRewardUnitSpawned(GridUnit unit)
    {
        if (levelUpRewardController != null)
            levelUpRewardController.OnLevelUpRewardUnitSpawned(unit);
    }

    // 그리드에 유닛 추가됨을 알림
    public void IncrementUnitCount()
    {
        gridUnitCount++;
    }

    // 그리드에서 유닛 제거됨을 알림
    public void DecrementUnitCount()
    {
        gridUnitCount--;
        if (gridUnitCount < 0)
            gridUnitCount = 0;
    }

    // 그리드가 꽉 찼는지 확인 (모든 유효한 셀이 점유되었는지)
    public bool IsGridFull()
    {
        if (LayoutData == null || GridArray == null)
            return false;

        for (int x = 0; x < LayoutData.width; x++)
        {
            for (int y = 0; y < LayoutData.height; y++)
            {
                // 유효한 셀인지 확인
                if (!LayoutData.IsValidCell(x, y))
                    continue;

                // 빈 셀이 하나라도 있으면 false
                if (GridArray[x, y] == (int)GridState.Empty)
                    return false;
            }
        }

        return true;
    }
}