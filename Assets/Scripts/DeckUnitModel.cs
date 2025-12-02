using System;
using UnityEngine;

[Serializable]
public class DeckUnitModel
{
    public int unitId;
    public string unitName;
    public string iconAddress;
    public Sprite icon;
    public UnitData rawData;
    public PlayerUnit playerUnit;

    public void FixMissingAddress()
    {
        if (string.IsNullOrEmpty(iconAddress) && rawData != null)
        {
            iconAddress = rawData.UNIT_ICON;  
        }
    }



}
