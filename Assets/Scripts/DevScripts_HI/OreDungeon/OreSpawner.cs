using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 광석을 랜덤 위치에 랜덤 순서로 스폰
/// </summary>
public class OreSpawner : MonoBehaviour
{
    [SerializeField] private OreDungeonManager gameManager;
    [SerializeField] private OreDungeonAssetManager assetManager;
    [SerializeField] private ObjectPoolManager poolManager;
    [SerializeField] private Ore orePrf;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Transform spawnTrans;
    [SerializeField] private Transform tailEffectTarget;

    [Header("스폰 설정")]
    [SerializeField] private int maxSpawnOreAmount = 10; // 한 번에 최대 스폰 개수
    [SerializeField] private float oreSize = 100f; // 광석 크기 (픽셀)
    [SerializeField] private float padding = 20f; // 광석 간 간격 (픽셀)
    [SerializeField] private RectTransform[] excludePanels; // 겹치지 않을 패널들

    private List<Vector2> availablePositions = new List<Vector2>();
    private List<Ore> allOres = new List<Ore>(); // 전체 광석 리스트
    private List<Ore> currentBatchOres = new List<Ore>(); // 현재 배치의 광석 리스트
    private int currentSpawnIndex = 0; // 현재 스폰 위치 인덱스

    private void Awake()
    {
        // 참조 누락 확인
        if (!ValidateReferences())
            return;
    }

    private void Start()
    {
        gameManager.OnInitialized += Initialize;
    }

    private void OnDestroy()
    {
        gameManager.OnInitialized -= Initialize;
    }

    private void Initialize()
    {
        OreDungeonData data = gameManager.DungeonData;

        // 스폰 위치 준비
        InitializeSpawnPositions();

        // 광석 스폰
        SpawnOres(data);
    }

    private void InitializeSpawnPositions()
    {
        availablePositions.Clear();

        Rect safeArea = Screen.safeArea;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // Screen 좌표를 Canvas 로컬 좌표로 변환
        Vector2 safeAreaMin, safeAreaMax;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            safeArea.min,
            canvas.worldCamera,
            out safeAreaMin
        );
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            safeArea.max,
            canvas.worldCamera,
            out safeAreaMax
        );

        // 그리드 생성
        List<Vector2> gridPositions = GenerateGridPositions(safeAreaMin, safeAreaMax);
        ShuffleList(gridPositions);

        availablePositions = gridPositions;
    }

    private void SpawnOres(OreDungeonData data)
    {
        allOres.Clear();
        currentSpawnIndex = 0;
        int posIndex = 0;

        // 타입1 생성 (전체 광석을 모두 생성)
        for (int i = 0; i < data.Number_Of_Ores && posIndex < availablePositions.Count; i++)
        {
            Ore ore = SpawnOre(availablePositions[posIndex], data.OresID);
            ore.gameObject.SetActive(false);
            allOres.Add(ore);
            posIndex++;
        }

        // 타입2 생성
        for (int i = 0; i < data.Number_Of_Ores2 && posIndex < availablePositions.Count; i++)
        {
            Ore ore = SpawnOre(availablePositions[posIndex], data.OresID2);
            ore.gameObject.SetActive(false);
            allOres.Add(ore);
            posIndex++;
        }

        // 전체 광석 리스트 섞기
        ShuffleList(allOres);

        // 첫 번째 배치 스폰
        SpawnNextBatch();
    }

    private void SpawnNextBatch()
    {
        currentBatchOres.Clear();

        // 다음 배치에 스폰할 광석 수 계산
        int remainingOres = allOres.Count - currentSpawnIndex;
        int oresToSpawn = Mathf.Min(remainingOres, maxSpawnOreAmount);

        if (oresToSpawn <= 0)
        {
            Debug.Log("모든 광석 스폰 완료");
            return;
        }

        // 현재 배치 광석 활성화
        for (int i = 0; i < oresToSpawn; i++)
        {
            Ore ore = allOres[currentSpawnIndex];
            ore.gameObject.SetActive(true);
            ore.OnOreDeactivated += OnOreDeactivated; // 이벤트 구독
            currentBatchOres.Add(ore);
            currentSpawnIndex++;
        }

        Debug.Log($"배치 스폰: {oresToSpawn}개 광석 활성화 (총 {currentSpawnIndex}/{allOres.Count})");
    }

    private void OnOreDeactivated(Ore ore)
    {
        // 이벤트 구독 해제
        ore.OnOreDeactivated -= OnOreDeactivated;

        // 현재 배치에서 제거
        currentBatchOres.Remove(ore);

        // 현재 배치의 모든 광석이 비활성화되었는지 확인
        if (currentBatchOres.Count == 0)
        {
            Debug.Log("현재 배치 모두 비활성화됨. 다음 배치 스폰 시도");

            // 남은 광석이 있으면 다음 배치 스폰
            if (currentSpawnIndex < allOres.Count)
            {
                SpawnNextBatch();
            }
        }
    }

    private List<Vector2> GenerateGridPositions(Vector2 areaMin, Vector2 areaMax)
    {
        List<Vector2> positions = new List<Vector2>();

        float cellSize = oreSize + padding;

        // 그리드 크기 계산
        int gridWidth = Mathf.FloorToInt((areaMax.x - areaMin.x) / cellSize);
        int gridHeight = Mathf.FloorToInt((areaMax.y - areaMin.y) / cellSize);

        // 중앙 정렬을 위한 오프셋
        float offsetX = areaMin.x + ((areaMax.x - areaMin.x) - (gridWidth * cellSize)) / 2f + cellSize / 2f;
        float offsetY = areaMin.y + ((areaMax.y - areaMin.y) - (gridHeight * cellSize)) / 2f + cellSize / 2f;

        // 그리드 위치 생성
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 position = new Vector2(
                    offsetX + x * cellSize,
                    offsetY + y * cellSize
                );

                // 패널과 겹치는지 확인
                if (!IsOverlappingWithPanels(position))
                {
                    positions.Add(position);
                }
            }
        }

        return positions;
    }

    private bool IsOverlappingWithPanels(Vector2 position)
    {
        if (excludePanels == null || excludePanels.Length == 0)
            return false;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        foreach (var panel in excludePanels)
        {
            if (panel == null)
                continue;

            // Panel의 월드 코너를 Canvas 로컬 좌표로 변환
            Vector3[] panelCorners = new Vector3[4];
            panel.GetWorldCorners(panelCorners);

            // 월드 코너를 Canvas 로컬 좌표로 변환
            Vector2 panelMin = canvasRect.InverseTransformPoint(panelCorners[0]);
            Vector2 panelMax = canvasRect.InverseTransformPoint(panelCorners[2]);

            // 광석의 크기를 고려한 영역 계산
            float halfOreSize = oreSize / 2f;
            Vector2 oreMin = position - Vector2.one * halfOreSize;
            Vector2 oreMax = position + Vector2.one * halfOreSize;

            // AABB 충돌 검사
            if (oreMin.x < panelMax.x && oreMax.x > panelMin.x &&
                oreMin.y < panelMax.y && oreMax.y > panelMin.y)
            {
                return true; // 겹침
            }
        }

        return false; // 겹치지 않음
    }

    private Ore SpawnOre(Vector2 position, int oresID)
    {
        Ore ore = Instantiate(orePrf, spawnTrans);

        // RectTransform 설정
        RectTransform rectTransform = ore.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;

        // Camera 가져오기 (Canvas의 worldCamera가 null이면 Main Camera 사용)
        Camera camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

        // 광석 타입 설정 (OreTable, Manager, Canvas, Camera, PoolManager, TailEffectTarget 전달)
        ore.SetOreType(oresID, assetManager.OreTable, gameManager, canvas, camera, poolManager, tailEffectTarget);

        return ore;
    }

    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }

    // 참조 누락 확인
    private bool ValidateReferences()
    {
        if (gameManager == null)
        {
            Debug.LogError($"{nameof(OreDungeonManager)} 참조가 누락되었습니다.");
            return false;
        }

        if (assetManager == null)
        {
            Debug.LogError($"{nameof(OreDungeonAssetManager)} 참조가 누락되었습니다.");
            return false;
        }

        if (poolManager == null)
        {
            Debug.LogError($"{nameof(ObjectPoolManager)} 참조가 누락되었습니다.");
            return false;
        }

        if (orePrf == null)
        {
            Debug.LogError($"{nameof(Ore)} 프리팹 참조가 누락되었습니다.");
            return false;
        }

        if (canvas == null)
        {
            Debug.LogError($"{nameof(Canvas)} 참조가 누락되었습니다.");
            return false;
        }

        return true;
    }
}