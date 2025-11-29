using UnityEngine;

public class DeckData : MonoBehaviour
{ 
    public string unitName;
    public Sprite icon;
    public int unitId;

    public DeckData(string name, Sprite sprite, int unitid)
    {
        unitName = name;
        icon = sprite;
        unitId = unitid;
    }
}