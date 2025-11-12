using System;
using System.Collections.Generic;
using UnityEngine;

public class GachaResult
{
    public List<GachaItem> items;
    public GachaType type;
    public int count;
    public DateTime timestamp;

    public GachaResult(List<GachaItem> items, GachaType type)
    {
        this.items = items;
        this.type = type;
        this.count = items.Count;
        this.timestamp = DateTime.Now;
    }
}