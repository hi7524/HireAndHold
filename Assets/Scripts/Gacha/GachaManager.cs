using System;
using System.Collections.Generic;
using UnityEngine;

public class GachaManager : MonoBehaviour
{
    [Header("Gacha Tables")]
    [SerializeField] private List<GachaItem> basicGachaItems = new List<GachaItem>();
    [SerializeField] private List<GachaItem> premiumGachaItems = new List<GachaItem>();

    private Dictionary<GachaType, int> totalWeightByType = new Dictionary<GachaType, int>();

    // 이벤트 (UI에서 구독)
    public event Action<GachaResult> OnGachaComplete;
    public event Action<string> OnGachaError;
    
     private async void Start()
    {
        await DataTableManager.InitAsync();
        InitializeTables();
    }

    private void InitializeTables()
    {
        Init(GachaType.Normal, 1001, basicGachaItems);
        Init(GachaType.Premium, 1002, premiumGachaItems);
    }

    private void Init(GachaType gachaType, int catalogId, List<GachaItem> gachaItems)
    {
        var catalog = DataTableManager.Get<DataTable_UnitCatalog>(DataTableIds.UnitCatalog).Get(catalogId);
        foreach (var item in catalog)
        {
            GachaItem gachaItem = new GachaItem();
            gachaItem.unitId = item.Unit_ID;
            gachaItem.probability = item.Probability;
            gachaItem.weight = (int)item.Weight * 100;
            gachaItems.Add(gachaItem);
        }
        BuildCumulativeTable(gachaType, gachaItems);

    }
    private void BuildCumulativeTable(GachaType gachaType, List<GachaItem> gachaItems)
    {
        int currentTotalWeight = 0;
        foreach (var item in gachaItems)
        {
            currentTotalWeight += item.weight;
            item.cumulativeWeight = currentTotalWeight;
        }
        totalWeightByType[gachaType] = currentTotalWeight;
    }


    // 실행
    public GachaResult ExecuteGacha(GachaType type, int count)
    {
        // // 조건 검사
        // if (!CanExecuteGacha(type, count))
        // {
        //     OnGachaError?.Invoke("재화가 부족합니다!");
        //     return null;
        // }

        // 뽑기 실행
        List<GachaItem> results = new List<GachaItem>();
        for (int i = 0; i < count; i++)
        {
            results.Add(GachaSingle(type));
        }

        // // 재화 차감
        // ConsumeGachaCost(type, count);

        // 결과 생성
        GachaResult result = new GachaResult(results, type);

        // 이벤트 발생 (UI가 알아서 처리)
        OnGachaComplete?.Invoke(result);

        Debug.Log($"[GachaManager] {type} {count}회 뽑기 완료");
        return result;
    }
    private GachaItem GachaSingle(GachaType gachaType = GachaType.Normal)
    {
        List<GachaItem> targetGachaItems = GetGachaItemsByType(gachaType);
        int targetTotalWeight = GetTotalWeightByType(gachaType);

        int randomWeight = UnityEngine.Random.Range(1, targetTotalWeight + 1);
        return GetItemByWeight(randomWeight, targetGachaItems);
    }
    private GachaItem GetItemByWeight(int randomWeight, List<GachaItem> gachaItems)
    {
        foreach (var item in gachaItems)
        {
            if (randomWeight <= item.cumulativeWeight)
            {
                return item;
            }
        }
        return null; 
    }
    private List<GachaItem> GetGachaItemsByType(GachaType gachaType)
    {
        return gachaType switch
        {
            GachaType.Normal => basicGachaItems,
            GachaType.Premium => premiumGachaItems,
            _ => basicGachaItems
        };
    }

    private int GetTotalWeightByType(GachaType gachaType)
    {
        return totalWeightByType.TryGetValue(gachaType, out int weight) ? weight : 0;
    }
    

    // /// <summary>
    // /// 가챠 실행 가능 여부
    // /// </summary>
    // private bool CanExecuteGacha(GachaType type, int count)
    // {
    //     int cost = GetGachaCost(type, count);
    //     int currentGem = PlayerDataManager.Instance.GetGem(); // 예시
        
    //     return currentGem >= cost;
    // }

    // /// <summary>
    // /// 가챠 비용 계산
    // /// </summary>
    // private int GetGachaCost(GachaType type, int count)
    // {
    //     int singleCost = type == GachaType.Normal ? 100 : 300;
    //     return singleCost * count;
    // }

    // /// <summary>
    // /// 재화 차감
    // /// </summary>
    // private void ConsumeGachaCost(GachaType type, int count)
    // {
    //     int cost = GetGachaCost(type, count);
    //     // PlayerDataManager.Instance.ConsumeGem(cost);
    //     Debug.Log($"[GachaManager] 보석 {cost}개 소모");
    // }

    // /// <summary>
    // /// 가챠 아이템 풀 가져오기 (테스트용)
    // /// </summary>
    // public List<GachaItem> GetGachaTable(GachaType type)
    // {
    //     return type == GachaType.Normal ? normalGachaTable : premiumGachaTable;
    // }
}
