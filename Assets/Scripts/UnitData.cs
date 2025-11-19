using UnityEngine;

public class UnitData : MonoBehaviour
{ 
    public string unitName;
    public Sprite icon;

    public UnitData(string name, Sprite sprite)
    {
        unitName = name;
        icon = sprite;
    }
}
