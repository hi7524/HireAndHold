using UnityEngine;

[CreateAssetMenu(fileName = "GridLayout", menuName = "Scriptable/Grid/Layout Data")]
public class GridLayoutData : ScriptableObject
{
    public int width = 7;
    public int height = 4;
    
    public bool[] validCells;
    
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