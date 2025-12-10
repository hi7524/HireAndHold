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
    [SerializeField] private GridLayoutData layoutData;
    [SerializeField] private GridVisualizer gridVisualizer;
    [SerializeField] private BuffManager buffManager;
    [SerializeField] private BattleUnitManager unitManager;
    [Space]
    [SerializeField] private Color validColor;
    [SerializeField] private Color invalidColor;
    [Space]
    [SerializeField] private Color crossBuffColor = new Color(0.9f, 0.95f, 1f, 1f);
    [SerializeField] private Color regionBuffColor = new Color(0.95f, 1f, 0.9f, 1f);
    [SerializeField] private GameObject gridUnitPrefab;
    [Space]
    [SerializeField] private LevelUpRewardController levelUpRewardController;
    [Space]
    [SerializeField] private AttackTriggerZone attackTriggerZone;
    [Space]
    [Header("Merge Effect")]
    [SerializeField] private GameObject[] mergeEffectPrefabs; // 0: 2성, 1: 3성


    public int[,] gridArray { get; private set; }
    public GridLayoutData LayoutData => layoutData;
    public bool IsInitialized { get; private set; } = false;

    private GridCell[,] gridCells;
    private int gridUnitCount = 0; // 그리드에 배치된 유닛 개수
    private HashSet<GridCell> highlightedCells = new HashSet<GridCell>();
    private Dictionary<Vector2Int, Color> coloredCell = new Dictionary<Vector2Int, Color>();
    private Dictionary<Vector2Int, Color> tempColoredCell;
    private Dictionary<Vector2Int, Color> buffColoredCells = new Dictionary<Vector2Int, Color>(); // 버프 활성화된 셀의 색상

    // 머지 이펙트 풀
    private Dictionary<int, Queue<GameObject>> mergeEffectPools = new Dictionary<int, Queue<GameObject>>();
    private Transform effectParent;


    private void Awake()
    {
        InitializeGridArrays();
        InitializeMergeEffectPools();
    }

    private void Start()
    {
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

    // 그리드 배열 초기화 (셀 등록 전)
    private void InitializeGridArrays()
    {
        gridArray = new int[layoutData.width, layoutData.height];
        gridCells = new GridCell[layoutData.width, layoutData.height];
    }

    // 그리드 셀 상태 설정 (Empty, Occupied, Unavailable)
    public void SetGridState(Vector2Int pos, GridState state)
    {
        gridArray[pos.x, pos.y] = (int)state;
        gridCells[pos.x, pos.y].SetAcceptable(state == GridState.Empty);

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
        Vector2Int pos = cell.GridPosition;

        if (IsWithinBounds(pos))
        {
            gridCells[pos.x, pos.y] = cell;
        }

        cell.SetGridManager(this);
    }

    // 유닛 배치 가능 여부 확인 및 시각적 피드백 제공
    public bool CanPlaceUnit(Vector2Int curPos, List<Vector2Int> grid)
    {
        ClearAllGridsColor();
        ChangeOccupiedCellColor();

        HashSet<Vector2Int> allPositions = GetAbsolutePositions(curPos, grid);
        bool canPlace = ValidateAllPositions(allPositions);

        HighlightCells(allPositions, canPlace);

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

        if (!layoutData.IsValidCell(pos))
            return false;

        int cellState = gridArray[pos.x, pos.y];
        return cellState == (int)GridState.Empty;
    }

    // 위치가 그리드 범위 내에 있는지 확인
    private bool IsWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < layoutData.width &&
               pos.y >= 0 && pos.y < layoutData.height;
    }

    // 배치 가능 여부에 따라 셀 하이라이트 색상 적용
    private void HighlightCells(HashSet<Vector2Int> positions, bool canPlace)
    {
        Color highlightColor = canPlace ? validColor : invalidColor;

        foreach (var pos in positions)
        {
            if (IsWithinBounds(pos))
            {
                GridCell cell = gridCells[pos.x, pos.y];
                if (cell != null)
                {
                    cell.SetColor(highlightColor);
                    highlightedCells.Add(cell);
                }
            }
        }
    }

    // 버프 영역의 색상 업데이트
    public void UpdateBuffColors()
    {
        // 이전 버프 색상 초기화
        buffColoredCells.Clear();

        if (layoutData == null)
        {
            Debug.LogError("layoutData가 null입니다!");
            return;
        }

        // 크로스 버프 색상 적용
        if (layoutData.enableCrossBuffs)
        {
            foreach (var crossBuff in layoutData.crossBuffs)
            {
                ApplyColorToHorizontalLine(crossBuff.centerPos, crossBuffColor);
                ApplyColorToVerticalLine(crossBuff.centerPos, crossBuffColor);

                // 중심점
                Color.RGBToHSV(crossBuffColor, out float h, out float s, out float v);
                Color centerColor = Color.HSVToRGB(h, Mathf.Min(s * 2f, 1f), Mathf.Min(v * 2f, 1f));
                centerColor.a = 1f;
                buffColoredCells[crossBuff.centerPos] = centerColor;
            }
        }

        // 리전 버프 색상 적용
        if (layoutData.enableRegionBuffs)
        {
            foreach (var regionBuff in layoutData.regionBuffs)
            {
                ApplyColorToRegion(regionBuff.regionCells, regionBuffColor);
            }
        }

        // 실제 셀에 색상 반영
        ApplyBuffColorsToGrid();
    }

    // 가로줄에 색상 적용
    private void ApplyColorToHorizontalLine(Vector2Int centerPos, Color color)
    {
        for (int x = 0; x < layoutData.width; x++)
        {
            Vector2Int pos = new Vector2Int(x, centerPos.y);
            if (layoutData.IsValidCell(pos))
            {
                buffColoredCells[pos] = color;
            }
        }
    }

    // 세로줄에 색상 적용
    private void ApplyColorToVerticalLine(Vector2Int centerPos, Color color)
    {
        for (int y = 0; y < layoutData.height; y++)
        {
            Vector2Int pos = new Vector2Int(centerPos.x, y);
            if (layoutData.IsValidCell(pos))
            {
                buffColoredCells[pos] = color;
            }
        }
    }

    // 리전에 색상 적용
    private void ApplyColorToRegion(List<Vector2Int> region, Color color)
    {
        foreach (var pos in region)
        {
            if (layoutData.IsValidCell(pos))
            {
                buffColoredCells[pos] = color;
            }
        }
    }

    // 버프 색상을 실제 그리드에 반영
    private void ApplyBuffColorsToGrid()
    {
        int appliedCount = 0;
        foreach (var buffCell in buffColoredCells)
        {
            Vector2Int pos = buffCell.Key;
            Color color = buffCell.Value;

            // 유닛이 이미 배치된 셀은 유닛 색상 유지
            if (!coloredCell.ContainsKey(pos))
            {
                if (IsWithinBounds(pos))
                {
                    gridCells[pos.x, pos.y].SetColor(color);
                    appliedCount++;
                }
            }
        }
    }

    // 버프 활성화 이펙트 재생
    public void PlayBuffActivationEffect(string buffName)
    {
        List<Vector2Int> affectedCells = GetBuffAffectedCells(buffName);

        if (affectedCells.Count == 0)
            return;

        float delayIncrement = 0.05f;

        for (int i = 0; i < affectedCells.Count; i++)
        {
            Vector2Int pos = affectedCells[i];
            if (IsWithinBounds(pos) && gridCells[pos.x, pos.y] != null)
            {
                float delay = i * delayIncrement;
                gridCells[pos.x, pos.y].PlayBuffActivationAnimation(delay);
            }
        }
    }

    // 버프가 영향을 주는 셀 목록 가져오기
    private List<Vector2Int> GetBuffAffectedCells(string buffName)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        // 크로스 버프 체크
        if (layoutData.enableCrossBuffs)
        {
            foreach (var crossBuff in layoutData.crossBuffs)
            {
                if (crossBuff.horizontalBuffName == buffName)
                {
                    // 가로줄 셀 추가
                    for (int x = 0; x < layoutData.width; x++)
                    {
                        Vector2Int pos = new Vector2Int(x, crossBuff.centerPos.y);
                        if (layoutData.IsValidCell(pos))
                        {
                            cells.Add(pos);
                        }
                    }
                }
                else if (crossBuff.verticalBuffName == buffName)
                {
                    // 세로줄 셀 추가
                    for (int y = 0; y < layoutData.height; y++)
                    {
                        Vector2Int pos = new Vector2Int(crossBuff.centerPos.x, y);
                        if (layoutData.IsValidCell(pos))
                        {
                            cells.Add(pos);
                        }
                    }
                }
            }
        }

        // 리전 버프 체크
        if (layoutData.enableRegionBuffs)
        {
            foreach (var regionBuff in layoutData.regionBuffs)
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
            for (int x = 0; x < layoutData.width; x++)
            {
                for (int y = 0; y < layoutData.height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (layoutData.IsValidCell(pos))
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
        foreach (var cell in highlightedCells)
        {
            if (cell != null && !coloredCell.ContainsKey(cell.GridPosition))
            {
                // 버프가 활성화된 셀이면 버프 색상으로, 아니면 흰색으로
                if (buffColoredCells.ContainsKey(cell.GridPosition))
                {
                    cell.SetColor(buffColoredCells[cell.GridPosition]);
                }
                else
                {
                    cell.SetColor(Color.white);
                }
            }
        }
        highlightedCells.Clear();
    }

    // 유닛이 차지하는 모든 셀에 지정된 색상 적용
    public void SetUnitCellsColor(Vector2Int curPos, List<Vector2Int> grid, Color color)
    {
        ClearAllGridsColor();

        HashSet<Vector2Int> allPositions = GetAbsolutePositions(curPos, grid);

        foreach (var pos in allPositions)
        {
            if (IsWithinBounds(pos))
            {
                GridCell cell = gridCells[pos.x, pos.y];
                if (cell != null)
                {
                    cell.SetColor(color);
                }
            }
        }
    }

    // 점유된 셀과 색상 정보 등록
    public void SetOccupiedCellAndColor(Vector2Int pos, Color color)
    {
        coloredCell[pos] = color;
    }

    // 유닛이 차지하던 셀의 색상 정보 제거 및 색상 초기화
    public void RemoveColoredCells(Vector2Int curPos, List<Vector2Int> grid)
    {
        RemoveCellColor(curPos);

        foreach (var relativePos in grid)
        {
            Vector2Int absolutePos = curPos + relativePos;
            RemoveCellColor(absolutePos);
        }
    }

    // 특정 셀의 색상 정보 제거 및 기본 색상으로 리셋
    private void RemoveCellColor(Vector2Int pos)
    {
        if (coloredCell.Remove(pos))
        {
            // 버프가 활성화된 셀이면 버프 색상으로, 아니면 흰색으로
            if (buffColoredCells.ContainsKey(pos))
            {
                gridCells[pos.x, pos.y].SetColor(buffColoredCells[pos]);
            }
            else
            {
                gridCells[pos.x, pos.y].SetColor(Color.white);
            }
        }
    }

    // 점유된 모든 셀에 등록된 색상 다시 적용
    public void ChangeOccupiedCellColor()
    {
        foreach (var cell in coloredCell)
        {
            var pos = cell.Key;
            gridCells[pos.x, pos.y].SetColor(cell.Value);
        }
    }

    // 현재 색상 상태를 임시 저장 (실패 시 복원용)
    public void CopyColoredCellToTemp()
    {
        tempColoredCell = new Dictionary<Vector2Int, Color>(coloredCell);
    }

    // 배치 실패 시 임시 저장된 색상 상태로 복원
    public void OnFailed()
    {
        if (tempColoredCell == null)
            return;

        RestoreColoredCells();
        RestoreCellColors();
    }

    // coloredCell 딕셔너리를 임시 저장 값으로 복원
    private void RestoreColoredCells()
    {
        foreach (var cell in tempColoredCell)
        {
            coloredCell[cell.Key] = cell.Value;
        }
    }

    // 그리드 셀의 시각적 색상을 임시 저장 값으로 복원
    private void RestoreCellColors()
    {
        foreach (var cell in tempColoredCell)
        {
            var pos = cell.Key;
            gridCells[pos.x, pos.y].SetColor(cell.Value);
        }
    }

    // 그리드 유닛 생성 및 초기화
    public GridUnit SpawnGridUnit(Vector3 position, int unitId, UnitGridData gridData)
    {
        if (gridUnitPrefab == null)
            return null;

        GameObject unitObj = Instantiate(gridUnitPrefab, position, Quaternion.identity);

        Unit unit = unitObj.GetComponent<Unit>();
        GridUnit gridUnit = unitObj.GetComponent<GridUnit>();

        if (gridUnit == null)
        {
            Destroy(unitObj);
            return null;
        }

        gridUnit.SetGridData(gridData);
        gridUnit.SetBattleUnitManager(unitManager);

        if (unit != null)
        {
            unit.SetUnitID(unitId);

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

    // 현재 그리드에 있는 유닛 개수 반환
    public int GetGridUnitCount()
    {
        return gridUnitCount;
    }

    // 그리드에 마지막 남은 유닛인지 확인
    public bool IsLastUnitOnGrid()
    {
        return gridUnitCount <= 1;
    }
}