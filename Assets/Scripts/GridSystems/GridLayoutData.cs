using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GridLayout", menuName = "Scriptable Objects/GridLayoutData")]
public class GridLayoutData : ScriptableObject
{
    public int width = 7;
    public int height = 4;

    public bool[] validCells;

    [Header("Buff Settings")]
    public bool enableCrossBuffs = true;
    public bool enableRegionBuffs = false;

    public List<CrossBuffData> crossBuffs = new List<CrossBuffData>();
    public List<RegionBuffData> regionBuffs = new List<RegionBuffData>();


    public bool IsValidCell(int x, int y)
    {
        int index = y * width + x;
        if (index < 0 || index >= validCells.Length)
            return false;

        return validCells[index];
    }

    public bool IsValidCell(Vector2Int pos)
    {
        return IsValidCell(pos.x, pos.y);
    }
}

[Serializable]
public class CrossBuffData
{
    public string horizontalBuffName = "Horizontal Buff";
    public string verticalBuffName = "Vertical Buff";
    public Vector2Int centerPos;
}

[Serializable]
public class RegionBuffData
{
    public string buffName = "Region Buff";
    public List<Vector2Int> regionCells = new List<Vector2Int>();
}