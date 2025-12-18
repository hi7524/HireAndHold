using UnityEngine;

[System.Serializable]
public class GachaItem
{
    public int unitId;

    public string unitName;
    //public Sprite unitIcon;

    [Range(0f, 100f)]
    public float probability;

    public int weight;

    [HideInInspector]
    public int cumulativeWeight;

    public GachaRarity rarity;

    public bool isDuplicate;
}

public enum GachaRarity
{
    Common,
    Rare,
    Unique,
    Epic,
    Legendary
}

public enum GachaType
{
    Normal,
    Premium
}
