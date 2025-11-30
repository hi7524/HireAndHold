using System;
using UnityEngine;

[Serializable]
public class DeckUnitModel
{
    public int unitId;
    public string unitName;
    public Sprite icon;
    public UnitData rawData;
    public PlayerUnit playerUnit;
}
