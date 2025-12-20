using UnityEngine;
using UnityEngine.UI;

public class FlexibleAttendanceGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int columns = 7; // 가로 개수 (고정)
    [SerializeField] private int rows = 5;    // 세로 개수 (고정)

    [Header("Cell Settings")]
    [SerializeField] private Vector2 cellSize = new Vector2(60, 60); // 체크박스 크기

    [Header("Padding (여백)")]
    [SerializeField] private float paddingLeft = 30;
    [SerializeField] private float paddingRight = 30;
    [SerializeField] private float paddingTop = 30;
    [SerializeField] private float paddingBottom = 30;

    [Header("Minimum Spacing (최소 간격)")]
    [SerializeField] private float minSpacingX = 10;
    [SerializeField] private float minSpacingY = 10;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        ArrangeGrid();
    }

    // 해상도 변경 시 자동으로 재배치
    void OnRectTransformDimensionsChange()
    {
        if (rectTransform != null && gameObject.activeInHierarchy)
        {
            ArrangeGrid();
        }
    }

    // Inspector에서 값 변경 시 실시간 미리보기
    void OnValidate()
    {
        if (Application.isPlaying && rectTransform != null)
        {
            ArrangeGrid();
        }
    }

    /// <summary>
    /// 그리드 배치 메인 함수
    /// </summary>
    public void ArrangeGrid()
    {
        int childCount = transform.childCount;
        if (childCount == 0) return;

        // 1. 사용 가능한 공간 계산
        float availableWidth = rectTransform.rect.width - paddingLeft - paddingRight;
        float availableHeight = rectTransform.rect.height - paddingTop - paddingBottom;

        // 2. 유연한 간격 계산 (남은 공간을 간격으로 분배)
        float totalCellWidth = cellSize.x * columns;
        float remainingWidth = availableWidth - totalCellWidth;
        float spacingX = Mathf.Max(minSpacingX, remainingWidth / (columns - 1));

        float totalCellHeight = cellSize.y * rows;
        float remainingHeight = availableHeight - totalCellHeight;
        float spacingY = Mathf.Max(minSpacingY, remainingHeight / (rows - 1));

        // 3. 실제 그리드 크기 계산
        float gridWidth = (cellSize.x * columns) + (spacingX * (columns - 1));
        float gridHeight = (cellSize.y * rows) + (spacingY * (rows - 1));

        // 4. 시작 위치 계산 (중앙 정렬)
        float startX = -gridWidth / 2 + cellSize.x / 2;
        float startY = gridHeight / 2 - cellSize.y / 2;

        // 5. 각 자식 오브젝트 배치
        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null) continue;

            // 현재 행과 열 계산
            int col = i % columns;
            int row = i / columns;

            // 위치 계산
            float posX = startX + (col * (cellSize.x + spacingX));
            float posY = startY - (row * (cellSize.y + spacingY));

            // 자식 오브젝트 앵커를 중앙으로 설정
            child.anchorMin = new Vector2(0.5f, 0.5f);
            child.anchorMax = new Vector2(0.5f, 0.5f);
            child.pivot = new Vector2(0.5f, 0.5f);

            // 위치 및 크기 적용
            child.anchoredPosition = new Vector2(posX, posY);
            child.sizeDelta = cellSize;
        }
    }

    // 수동으로 재배치가 필요할 때 호출
    [ContextMenu("Force Rearrange Grid")]
    public void ForceRearrange()
    {
        ArrangeGrid();
    }
}
