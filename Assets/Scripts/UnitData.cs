using UnityEngine;

public class UnitData : MonoBehaviour
{ 
    public string unitName;
    public Sprite icon;
    public int unitId;

    public UnitData(string name, Sprite sprite, int unitid)
    {
        unitName = name;
        icon = sprite;
        unitId = unitid;
    }
}
