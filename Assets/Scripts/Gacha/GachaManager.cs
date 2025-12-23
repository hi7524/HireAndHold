using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

    // 동시 실행 방지 락
    private bool isExecuting = false;
    public bool IsExecuting => isExecuting;

    private  void Start()
    {
       
        InitializeTables();
    }

    private void InitializeTables()
    {
        
        var normalGacha = GetGachaData(GachaType.Normal);
        var premiumGacha = GetGachaData(GachaType.Premium);
        if (normalGacha != null)
        {
            Init(GachaType.Normal, normalGacha.Catalog_ID, basicGachaItems);
        }
        else
        {
            Debug.LogError("[GachaManager] normalGacha가 null입니다!");
        }

        if (premiumGacha != null)
        {
            Init(GachaType.Premium, premiumGacha.Catalog_ID, premiumGachaItems);
        }
        else
        {
            Debug.LogError("[GachaManager] premiumGacha가 null입니다!");
        }
    }

    private void Init(GachaType gachaType, int catalogId, List<GachaItem> gachaItems)
    {
        var catalog = DataTableManager.Get<DataTable_UnitCatalog>(DataTableIds.UnitCatalog).Get(catalogId);
        if (catalog == null)
        {
            Debug.LogError($"[GachaManager] 카탈로그를 찾을 수 없음: {catalogId}");
            return;
        }

        foreach (var item in catalog)
        {
            var unitData = DataTableManager.UnitTable.Get(item.TARGET_ID);
            if (unitData == null)
            {
                Debug.LogError($"[GachaManager] UnitData 없음: {item.TARGET_ID}");
                continue;
            }

            GachaItem gachaItem = new GachaItem
            {
                unitId = item.TARGET_ID,
                probability = item.Probability,
                weight = (int)(item.Probability * 10000),
                rarity = ConvertRankToRarity(unitData.RANK),

                isDuplicate = false,
                isNew = false
            };

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

    /// <summary>
    /// 가챠 데이터 가져오기
    /// </summary>
    private UnitGachaData GetGachaData(GachaType type)
    {
        int gachaTypeId = type == GachaType.Normal ? 1 : 2;
        return DataTableManager.UnitGachaTable.GetByType(gachaTypeId);
    }

    /// <summary>
    /// 가챠 비용 계산
    /// </summary>
    private int GetGachaCost(GachaType type, int count)
    {
        var data = GetGachaData(type);
        if (data == null) return 0;

        if (count == 10)
        {
            return data.Draw10_ItemNum;
        }
        return data.ItemNum * count;
    }

    /// <summary>
    /// 비용 아이템 ID 가져오기
    /// </summary>
    private int GetCostItemId(GachaType type, int count)
    {
        var data = GetGachaData(type);
        if (data == null) return 0;

        return count == 10 ? data.Draw10_ItemID : data.ItemID;
    }

    /// <summary>
    /// 안전한 가챠 실행 (비동기, DB 연동)
    /// </summary>
    public async UniTask<GachaResult> ExecuteGachaAsync(GachaType type, int count)
    {
        // 동시 실행 방지
        if (isExecuting)
        {
            Debug.LogWarning("[GachaManager] 이미 가챠 진행 중");
            OnGachaError?.Invoke("가챠가 진행 중입니다.");
            return null;
        }

        isExecuting = true;

        try
        {
            // 가챠 데이터 확인
            var gachaData = GetGachaData(type);
            if (gachaData == null)
            {
                Debug.LogError("[GachaManager] 가챠 데이터를 찾을 수 없습니다.");
                OnGachaError?.Invoke("가챠 정보를 찾을 수 없습니다.");
                return null;
            }

            // 비용 계산
            int costItemId = GetCostItemId(type, count);
            int costAmount = GetGachaCost(type, count);

            // 비용 아이템에 따라 차감
            bool deductSuccess = await DeductCostAsync(costItemId, costAmount);
            if (!deductSuccess)
            {
                return null;
            }
            // 뽑기 실행
            List<GachaItem> results = new List<GachaItem>();
            var gachaItems = GetGachaItemsByType(type);
            var totalWeight = GetTotalWeightByType(type);
            for (int i = 0; i < count; i++)
            {
                var gachaResult = GachaSingle(type);
                if (gachaResult == null)
                {
                    Debug.LogError($"[GachaManager] GachaSingle 반환값이 null! (i={i})");
                    continue;
                }
                results.Add(gachaResult);
            }

            if (results.Count == 0)
            {
                Debug.LogError("[GachaManager] 뽑기 결과가 0개입니다!");
                OnGachaError?.Invoke("뽑기 결과가 없습니다.");
                return null;
            }

            // 획득한 캐릭터 DB 저장
            List<string> failedCharacters = new List<string>();
            HashSet<int> processedUnitsThisGacha = new HashSet<int>();

            foreach (var item in results)
            {
                int unitId = item.unitId;
                string characterId = unitId.ToString();

                bool alreadyOwnedInDB = PlayData.HasCharacter(characterId);
                bool alreadyProcessedInThisGacha = processedUnitsThisGacha.Contains(unitId);

                if (!alreadyOwnedInDB && !alreadyProcessedInThisGacha)
                {
                    //신규 유닛
                    item.isDuplicate = false;
                    item.isNew = true; 

                    await DatabaseManager.Instance.AddCharacterAsync(characterId, 1);

                    processedUnitsThisGacha.Add(unitId);
                }
                else
                {
                    // 중복 유닛
                    item.isDuplicate = true;
                    item.isNew = false;

                    var unitData = DataTableManager.UnitTable.Get(unitId);
                    if (unitData != null && unitData.FRAGMENT_ITEM_ID > 0)
                    {
                        await DatabaseManager.Instance.AddItemAsync(
                            unitData.FRAGMENT_ITEM_ID, 1
                        );
                    }
                }

            }



            // 저장 실패한 캐릭터가 있으면 로그 기록
            if (failedCharacters.Count > 0)
            {
                Debug.LogError($"[GachaManager] {failedCharacters.Count}개 캐릭터 저장 실패");
            }

            // PlayData 캐릭터 캐시 동기화
            PlayData.SyncCharactersFromDatabase();

            // 업적 연동
            await UpdateGachaAchievementsAsync(type, count, results);

            // 결과 생성 및 이벤트 발생
            GachaResult result = new GachaResult(results, type);
            OnGachaComplete?.Invoke(result);
            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GachaManager] 가챠 실행 중 오류: {ex.Message}");
            OnGachaError?.Invoke("가챠 실행 중 오류가 발생했습니다.");
            return null;
        }
        finally
        {
            isExecuting = false;
        }

    }

    /// <summary>
    /// 비용 차감 (인벤토리 아이템 사용)
    /// </summary>
    private async UniTask<bool> DeductCostAsync(int itemId, int amount)
    {
        var itemData = DataTableManager.ItemTable.Get(itemId);
        if (itemData == null)
        {
            Debug.LogError($"[GachaManager] 아이템 정보를 찾을 수 없음: {itemId}");
            OnGachaError?.Invoke("아이템 정보를 찾을 수 없습니다.");
            return false;
        }

        // 캐시에서 보유량 확인
        int currentCount = PlayData.GetItemCount(itemId);
        if (currentCount < amount)
        {
            Debug.LogWarning($"[GachaManager] 아이템 부족: {itemData.ITEM_NAME} 보유 {currentCount}, 필요 {amount}");
            OnGachaError?.Invoke($"{itemData.ITEM_NAME}이(가) 부족합니다!");
            return false;
        }

        // DB에서 아이템 차감
        bool success = await DatabaseManager.Instance.AddItemAsync(itemId, -amount);

        if (success)
        {
            // 캐시 업데이트
            PlayData.SetItemCountImmediate(itemId, currentCount - amount);
        }
        else
        {
            Debug.LogError($"[GachaManager] {itemData.ITEM_NAME} 차감 실패");
            OnGachaError?.Invoke("아이템 차감에 실패했습니다.");
        }

        return success;
    }

    /// <summary>
    /// 가챠 실행 (하위 호환성 유지)
    /// </summary>
    public void ExecuteGacha(GachaType type, int count)
    {
        ExecuteGachaAsync(type, count).Forget();
    }

    private GachaItem GachaSingle(GachaType gachaType = GachaType.Normal)
    {
        List<GachaItem> source = GetGachaItemsByType(gachaType);
        int totalWeight = GetTotalWeightByType(gachaType);

        int randomWeight = UnityEngine.Random.Range(1, totalWeight + 1);
        var origin = GetItemByWeight(randomWeight, source);

        if (origin == null) return null;

        return new GachaItem
        {
            unitId = origin.unitId,
            unitName = origin.unitName,
            probability = origin.probability,
            weight = origin.weight,
            rarity = origin.rarity,
            isDuplicate = false,
            isNew = false
        };
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

    private GachaRarity ConvertRankToRarity(int rank)
    {
        return rank switch
        {
            1 => GachaRarity.Common,
            2 => GachaRarity.Rare,
            3 => GachaRarity.Unique,
            4 => GachaRarity.Epic,
            5 => GachaRarity.Legendary,
            _ => GachaRarity.Common
        };
    }

    /// <summary>
    /// 가챠 관련 업적 업데이트
    /// </summary>
    private async UniTask UpdateGachaAchievementsAsync(GachaType type, int count, List<GachaItem> results)
    {
        // 총 가챠 횟수 업적
        await AchievementManager.AddGachaTotalAsync(count);

        // 타입별 업적
        if (type == GachaType.Normal)
        {
            if (count == 1)
                await AchievementManager.CompleteGachaNormalAsync();
            else if (count == 10)
                await AchievementManager.CompleteGachaNormal10Async();
        }
        else if (type == GachaType.Premium)
        {
            if (count == 1)
                await AchievementManager.CompleteGachaPremiumAsync();
            else if (count == 10)
                await AchievementManager.CompleteGachaPremium10Async();
        }

        // 레어리티별 업적 (에픽, 레전더리, 유니크 획득)
        foreach (var item in results)
        {
            switch (item.rarity)
            {
                case GachaRarity.Unique:
                    await AchievementManager.CompleteGachaGetUniqueAsync();
                    break;
                case GachaRarity.Epic:
                    await AchievementManager.CompleteGachaGetEpicAsync();
                    break;
                case GachaRarity.Legendary:
                    await AchievementManager.CompleteGachaGetLegendAsync();
                    break;
            }
        }
    }
}
