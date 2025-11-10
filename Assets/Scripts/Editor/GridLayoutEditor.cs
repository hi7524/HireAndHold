using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(GridLayoutData))]
public class GridLayoutEditor : Editor
{
    private const float CellSize = 35f;
    
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
        EditorGUILayout.LabelField("Click cells to toggle (White=Valid)");
        
        if (data.validCells != null && data.validCells.Length == data.width * data.height)
        {
            for (int y = data.height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < data.width; x++)
                {
                    int index = y * data.width + x;
                    Color prevColor = GUI.backgroundColor;
                    GUI.backgroundColor = data.validCells[index] ? Color.white : Color.gray;
                    
                    if (GUILayout.Button("", GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
                    {
                        data.validCells[index] = !data.validCells[index];
                        EditorUtility.SetDirty(data);
                    }
                    
                    GUI.backgroundColor = prevColor;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
        if (GUI.changed)
            EditorUtility.SetDirty(data);
    }
}
#endif