using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UnitGridData))]
public class UnitGridDataEditor : Editor
{
    private const float CellSize = 30f;
    private const float GridPadding = 10f;
    private Color centerColor = new Color(1f, 0.5f, 0.5f);     // 중심 표시 색상
    private Color occupiedColor = new Color(0.3f, 0.7f, 1f);   // 차지된 셀 색상
    private Color emptyColor = new Color(0.2f, 0.2f, 0.2f);    // 빈 셀 색상
    private Color gridLineColor = new Color(0.4f, 0.4f, 0.4f); // 그리드 선 색상

    public override void OnInspectorGUI()
    {
        UnitGridData data = (UnitGridData)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Unit Shape Editor", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("클릭하여 셀을 토글. 중심(빨간색)을 기준으로 상대 좌표가 저장");
        EditorGUILayout.Space(5);

        // 그리드 그리기
        DrawGrid(data);

        EditorGUILayout.Space(10);

        // 현재 저장된 상대 좌표 표시
        EditorGUILayout.LabelField("Occupied Cells (Relative Coordinates):", EditorStyles.boldLabel);
        if (data.shape.occupiedCells.Count == 0)
        {
            EditorGUILayout.LabelField("  None", EditorStyles.miniLabel);
        }
        else
        {
            foreach (var cell in data.shape.occupiedCells)
            {
                EditorGUILayout.LabelField($"  ({cell.x}, {cell.y})", EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.Space(10);

        // Clear 버튼
        if (GUILayout.Button("Clear All Cells"))
        {
            Undo.RecordObject(data, "Clear All Cells");
            data.shape.occupiedCells.Clear();
            EditorUtility.SetDirty(data);
        }
    }

    private void DrawGrid(UnitGridData data)
    {
        int gridSize = UnitGridData.GridSize;
        int centerIndex = gridSize / 2;

        float totalSize = gridSize * CellSize;
        Rect gridRect = GUILayoutUtility.GetRect(totalSize + GridPadding * 2, totalSize + GridPadding * 2);

        // 배경
        EditorGUI.DrawRect(gridRect, new Color(0.15f, 0.15f, 0.15f));

        float startX = gridRect.x + GridPadding;
        float startY = gridRect.y + GridPadding;

        // 셀 그리기
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                Rect cellRect = new Rect(
                    startX + x * CellSize,
                    startY + y * CellSize,
                    CellSize - 1,
                    CellSize - 1
                );

                // 중심인지 확인
                bool isCenter = (x == centerIndex && y == centerIndex);

                // 차지된 셀인지 확인
                Vector2Int relativePos = new Vector2Int(x - centerIndex, y - centerIndex);
                bool isOccupied = data.shape.occupiedCells.Contains(relativePos);

                // 색상 결정
                Color cellColor;
                if (isCenter)
                {
                    cellColor = centerColor;
                }
                else if (isOccupied)
                {
                    cellColor = occupiedColor;
                }
                else
                {
                    cellColor = emptyColor;
                }

                EditorGUI.DrawRect(cellRect, cellColor);

                // 마우스 클릭 처리 (중심은 클릭 불가)
                if (!isCenter && Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    Undo.RecordObject(data, "Toggle Cell");
                    data.ToggleCell(x, y);
                    EditorUtility.SetDirty(data);
                    Event.current.Use();
                }

                // 중심에 표시
                if (isCenter)
                {
                    GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                    labelStyle.alignment = TextAnchor.MiddleCenter;
                    labelStyle.fontStyle = FontStyle.Bold;
                    labelStyle.normal.textColor = Color.white;
                    GUI.Label(cellRect, "C", labelStyle);
                }
            }
        }

        // 그리드 선 그리기
        Handles.BeginGUI();
        Handles.color = gridLineColor;

        // 세로 선
        for (int x = 0; x <= gridSize; x++)
        {
            float xPos = startX + x * CellSize;
            Handles.DrawLine(new Vector3(xPos, startY), new Vector3(xPos, startY + totalSize));
        }

        // 가로 선
        for (int y = 0; y <= gridSize; y++)
        {
            float yPos = startY + y * CellSize;
            Handles.DrawLine(new Vector3(startX, yPos), new Vector3(startX + totalSize, yPos));
        }

        Handles.EndGUI();
    }
}