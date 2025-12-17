using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 광석 스폰
/// </summary>
public class OreSpawner : MonoBehaviour
{
    [SerializeField] private OreDungeonManager gameManager;
    [SerializeField] private ObjectPoolManager poolManager;
    [SerializeField] private Ore orePrf;
    [SerializeField] private Canvas canvas;

    [Header("스폰 설정")]
    [SerializeField] private float oreSize = 100f; // 광석 크기 (픽셀)
    [SerializeField] private float padding = 20f; // 광석 간 최소 간격 (픽셀)
    [SerializeField] private RectTransform[] excludePanels; // 겹치지 않을 패널들

    private int oreAmount;
    private List<Vector2> spawnedPositions = new List<Vector2>();

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
        oreAmount = gameManager.DungeonData.Number_Of_Ores;
        Debug.Log($"총 광석 개수 {oreAmount}");

        SpawnAllOres();
    }

    private void SpawnAllOres()
    {
        spawnedPositions.Clear();

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

        // 그리드 위치를 섞기
        ShuffleList(gridPositions);

        // 광석 스폰
        int spawnCount = Mathf.Min(oreAmount, gridPositions.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnOre(gridPositions[i]);
        }

        Debug.Log($"{spawnCount}개의 광석 스폰 완료 (요청: {oreAmount}, 가능: {gridPositions.Count})");
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

    private void SpawnOre(Vector2 position)
    {
        Ore ore = Instantiate(orePrf, canvas.transform);

        // RectTransform 설정
        RectTransform rectTransform = ore.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;

        spawnedPositions.Add(position);
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
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