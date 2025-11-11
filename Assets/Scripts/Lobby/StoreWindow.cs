using System.Collections.Generic;
using UnityEngine;

public class StoreWindow : GenericWindow
{

    [SerializeField] private List<SampleItems> gachaItems;
    
    private int totalWeight;


    private void BuildCumulativeTable()
    {
        totalWeight = 0;
        foreach (var item in gachaItems)
        {
            totalWeight += item.weight;
            item.weight = totalWeight;
        }
    }
    private SampleItems GachaSingle()
    {
        int randomWeight = Random.Range(1, totalWeight + 1);
        return GetItemByWeight(randomWeight);
    }
    private SampleItems GetItemByWeight(int randomWeight)
    {
        foreach (var item in gachaItems)
        {
            if (randomWeight <= item.weight)
            {
                return item;
            }
        }
        return null; // Should not reach here if weights are set correctly
    }
    public void OnClickedGachaButton()
    {
        BuildCumulativeTable();
        for (int i = 0; i < 10; i++)
        {
            var item = GachaSingle();
            Debug.Log($"Gacha Result: {i} : {item.unitId} {item.probability}" );
        }
    }
}
