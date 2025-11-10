using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridVisualizer))]
public class GridVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GridVisualizer visualizer = (GridVisualizer)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("OnEditMode");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Grid", GUILayout.Height(22)))
        {
            visualizer.ClearGrid();
            visualizer.VisualizeGridData(visualizer.GetLayoutData());
        }

        if (GUILayout.Button("Clear Grid", GUILayout.Height(22)))
        {
            visualizer.ClearGrid();
        }
        EditorGUILayout.EndHorizontal();
    }
}