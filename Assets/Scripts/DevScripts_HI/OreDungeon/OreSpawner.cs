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

    [Header("스폰 설정")]
    [SerializeField] private int maxSpawnOreAmount = 10; // 한 번에 최대 스폰 개수
    [SerializeField] private float oreSize = 100f; // 광석 크기 (픽셀)
    [SerializeField] private float padding = 20f; // 광석 간 간격 (픽셀)
    [SerializeField] private RectTransform[] excludePanels; // 겹치지 않을 패널들

    private List<Vector2> availablePositions = new List<Vector2>();

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
        List<Ore> oreList = new List<Ore>();
        int posIndex = 0;

        // 타입1 생성
        for (int i = 0; i < data.Number_Of_Ores && posIndex < availablePositions.Count && posIndex < maxSpawnOreAmount; i++)
        {
            Ore ore = SpawnOre(availablePositions[posIndex], data.OresID);
            ore.gameObject.SetActive(false);
            oreList.Add(ore);
            posIndex++;
        }

        // 타입2 생성
        for (int i = 0; i < data.Number_Of_Ores2 && posIndex < availablePositions.Count && posIndex < maxSpawnOreAmount; i++)
        {
            Ore ore = SpawnOre(availablePositions[posIndex], data.OresID2);
            ore.gameObject.SetActive(false);
            oreList.Add(ore);
            posIndex++;
        }

        ShuffleList(oreList);
        foreach (var ore in oreList)
        {
            ore.gameObject.SetActive(true);
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

        // 광석 타입 설정 (OreTable, Manager, Canvas, Camera, PoolManager 전달)
        ore.SetOreType(oresID, assetManager.OreTable, gameManager, canvas, camera, poolManager);

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