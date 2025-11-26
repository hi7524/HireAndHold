using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(GridLayoutData))]
public class GridLayoutEditor : Editor
{
    private const float CellSize = 35f;
    private int tabIndex = 0;
    private int selectedCrossBuffIndex = -1;
    private int selectedRegionBuffIndex = -1;

    public override void OnInspectorGUI()
    {
        GridLayoutData data = (GridLayoutData)target;

        // 크기 설정
        EditorGUILayout.BeginHorizontal();
        data.width = EditorGUILayout.IntField("Width", data.width);
        data.height = EditorGUILayout.IntField("Height", data.height);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Initialize Grid"))
        {
            data.validCells = new bool[data.width * data.height];
            for (int i = 0; i < data.validCells.Length; i++)
                data.validCells[i] = true;
        }

        EditorGUILayout.Space(10);

        // 버프 설정
        EditorGUILayout.LabelField("Buff Settings", EditorStyles.boldLabel);
        bool newEnableCrossBuffs = EditorGUILayout.Toggle("Enable Cross Buffs", data.enableCrossBuffs);
        if (newEnableCrossBuffs && !data.enableCrossBuffs)
        {
            data.enableCrossBuffs = true;
            data.enableRegionBuffs = false;
        }

        bool newEnableRegionBuffs = EditorGUILayout.Toggle("Enable Region Buffs", data.enableRegionBuffs);
        if (newEnableRegionBuffs && !data.enableRegionBuffs)
        {
            data.enableRegionBuffs = true;
            data.enableCrossBuffs = false;
        }

        EditorGUILayout.Space(10);

        // 탭 선택
        string[] tabNames;
        if (data.enableCrossBuffs)
        {
            tabNames = new string[] { "Grid Layout", "Cross Buffs" };
        }
        else if (data.enableRegionBuffs)
        {
            tabNames = new string[] { "Grid Layout", "Region Buffs" };
        }
        else
        {
            tabNames = new string[] { "Grid Layout" };
        }

        // 현재 선택된 탭이 유효한 범위를 벗어나면 0으로 초기화
        if (tabIndex >= tabNames.Length)
        {
            tabIndex = 0;
        }

        tabIndex = GUILayout.Toolbar(tabIndex, tabNames);

        EditorGUILayout.Space(10);

        switch (tabIndex)
        {
            case 0:
                DrawGridLayoutTab(data);
                break;
            case 1:
                if (data.enableCrossBuffs)
                    DrawCrossBuffsTab(data);
                else if (data.enableRegionBuffs)
                    DrawRegionBuffsTab(data);
                break;
            case 2:
                DrawRegionBuffsTab(data);
                break;
        }

        if (GUI.changed)
            EditorUtility.SetDirty(data);
    }

    private void DrawGridLayoutTab(GridLayoutData data)
    {
        EditorGUILayout.LabelField("Click cells to toggle (White=Valid, Gray=Invalid)", EditorStyles.helpBox);

        if (data.validCells != null && data.validCells.Length == data.width * data.height)
        {
            DrawGrid(data, (x, y, index) =>
            {
                Color prevColor = GUI.backgroundColor;
                GUI.backgroundColor = data.validCells[index] ? Color.white : Color.gray;

                if (GUILayout.Button("", GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
                {
                    data.validCells[index] = !data.validCells[index];
                    EditorUtility.SetDirty(data);
                }

                GUI.backgroundColor = prevColor;
            });
        }
    }

    private void DrawCrossBuffsTab(GridLayoutData data)
    {
        EditorGUILayout.LabelField("Cross Buffs (십자가 버프)", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        // 버프 리스트
        for (int i = 0; i < data.crossBuffs.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            bool isSelected = selectedCrossBuffIndex == i;
            if (GUILayout.Toggle(isSelected, $"Cross Buff {i}", EditorStyles.foldoutHeader))
            {
                selectedCrossBuffIndex = i;
            }
            else if (isSelected)
            {
                selectedCrossBuffIndex = -1;
            }

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                data.crossBuffs.RemoveAt(i);
                if (selectedCrossBuffIndex == i) selectedCrossBuffIndex = -1;
                EditorUtility.SetDirty(data);
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (isSelected)
            {
                data.crossBuffs[i].horizontalBuffName = EditorGUILayout.TextField("Horizontal Buff Name", data.crossBuffs[i].horizontalBuffName);
                data.crossBuffs[i].verticalBuffName = EditorGUILayout.TextField("Vertical Buff Name", data.crossBuffs[i].verticalBuffName);
                data.crossBuffs[i].centerPos = EditorGUILayout.Vector2IntField("Center Position", data.crossBuffs[i].centerPos);
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("+ Add Cross Buff"))
        {
            data.crossBuffs.Add(new CrossBuffData());
            selectedCrossBuffIndex = data.crossBuffs.Count - 1;
            EditorUtility.SetDirty(data);
        }

        // 선택된 버프의 그리드 프리뷰
        if (selectedCrossBuffIndex >= 0 && selectedCrossBuffIndex < data.crossBuffs.Count)
        {
            EditorGUILayout.Space(10);

            DrawGrid(data, (x, y, index) =>
            {
                var centerPos = data.crossBuffs[selectedCrossBuffIndex].centerPos;
                bool isCrossCell = (x == centerPos.x || y == centerPos.y);
                bool isCenterCell = (x == centerPos.x && y == centerPos.y);

                Color prevColor = GUI.backgroundColor;

                if (isCenterCell)
                    GUI.backgroundColor = Color.blue;
                else if (isCrossCell && data.validCells[index])
                    GUI.backgroundColor = new Color(0.53f, 0.81f, 0.98f);
                else
                    GUI.backgroundColor = data.validCells[index] ? Color.white : Color.gray;

                if (GUILayout.Button("", GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
                {
                    data.crossBuffs[selectedCrossBuffIndex].centerPos = new Vector2Int(x, y);
                    EditorUtility.SetDirty(data);
                }

                GUI.backgroundColor = prevColor;
            });
        }
    }

    private void DrawRegionBuffsTab(GridLayoutData data)
    {
        EditorGUILayout.LabelField("Region Buffs (영역 버프)", EditorStyles.boldLabel);

        // 버프 리스트
        for (int i = 0; i < data.regionBuffs.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            bool isSelected = selectedRegionBuffIndex == i;
            if (GUILayout.Toggle(isSelected, $"Region Buff {i}", EditorStyles.foldoutHeader))
            {
                selectedRegionBuffIndex = i;
            }
            else if (isSelected)
            {
                selectedRegionBuffIndex = -1;
            }

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                data.regionBuffs.RemoveAt(i);
                if (selectedRegionBuffIndex == i) selectedRegionBuffIndex = -1;
                EditorUtility.SetDirty(data);
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (isSelected)
            {
                data.regionBuffs[i].buffName = EditorGUILayout.TextField("Buff Name", data.regionBuffs[i].buffName);

                EditorGUILayout.LabelField($"Cells: {data.regionBuffs[i].regionCells.Count}");

                if (GUILayout.Button("Clear All Cells"))
                {
                    data.regionBuffs[i].regionCells.Clear();
                    EditorUtility.SetDirty(data);
                }
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("+ Add Region Buff"))
        {
            data.regionBuffs.Add(new RegionBuffData());
            selectedRegionBuffIndex = data.regionBuffs.Count - 1;
            EditorUtility.SetDirty(data);
        }

        // 선택된 버프의 그리드 프리뷰
        if (selectedRegionBuffIndex >= 0 && selectedRegionBuffIndex < data.regionBuffs.Count)
        {
            EditorGUILayout.Space(10);

            DrawGrid(data, (x, y, index) =>
            {
                var regionBuff = data.regionBuffs[selectedRegionBuffIndex];
                Vector2Int cellPos = new Vector2Int(x, y);
                bool isInRegion = regionBuff.regionCells.Contains(cellPos);

                Color prevColor = GUI.backgroundColor;

                if (isInRegion)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = data.validCells[index] ? Color.white : Color.gray;

                if (GUILayout.Button("", GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
                {
                    if (isInRegion)
                    {
                        regionBuff.regionCells.Remove(cellPos);
                    }
                    else
                    {
                        regionBuff.regionCells.Add(cellPos);
                    }
                    EditorUtility.SetDirty(data);
                }

                GUI.backgroundColor = prevColor;
            });
        }
    }

    private void DrawGrid(GridLayoutData data, System.Action<int, int, int> cellDrawAction)
    {
        if (data.validCells == null || data.validCells.Length != data.width * data.height)
            return;

        for (int y = data.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < data.width; x++)
            {
                int index = y * data.width + x;
                cellDrawAction(x, y, index);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif