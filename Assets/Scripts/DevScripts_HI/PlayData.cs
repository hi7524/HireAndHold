using System.Collections.Generic;
using UnityEngine;

public static class PlayData
{
    public static Dictionary<int, float> unitFragments = new();

    public static HashSet<int> selectedUnitIds = new HashSet<int>{ 11101, 11104, 11107, 11110, 11113 };

    //편성된 덱 배열
    public static int[] selectedDeckUnitIds = new int[5];
    public static string[] selectedDeckUnitIconAddresses = new string[5];


}
