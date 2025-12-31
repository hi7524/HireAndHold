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

    private void Start()
    {
        InitializeTables();

        // 게임 시작 시 가챠 아이콘 미리 로드
        PreloadGachaIconsAsync().Forget();
    }

    /// <summary>
    /// 가챠 아이콘 미리 로드 (백그라운드) - 일반 아이콘만
    /// </summary>
    private async UniTaskVoid PreloadGachaIconsAsync()
    {
        await UniTask.Delay(1000);

        HashSet<string> iconAddresses = new HashSet<string>();

        foreach (var item in basicGachaItems)
        {
            var unitData = DataTableManager.UnitTable?.Get(item.unitId);
            if (unitData != null && !string.IsNullOrEmpty(unitData.UNIT_ICON))
            {
                iconAddresses.Add(unitData.UNIT_ICON);
            }
        }

        foreach (var item in premiumGachaItems)
        {
            var unitData = DataTableManager.UnitTable?.Get(item.unitId);
            if (unitData != null && !string.IsNullOrEmpty(unitData.UNIT_ICON))
            {
                iconAddresses.Add(unitData.UNIT_ICON);
            }
        }

        Debug.Log($"[GachaManager] 프리로드 시작: {iconAddresses.Count}개 아이콘 (일반 아이콘만)");

        await SpriteCache.Instance.PreloadSpritesAsync(iconAddresses);

        Debug.Log($"[GachaManager] 프리로드 완료: 성공 {SpriteCache.Instance.CachedCount}개");
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

    private UnitGachaData GetGachaData(GachaType type)
    {
        int gachaTypeId = type == GachaType.Normal ? 1 : 2;
        return DataTableManager.UnitGachaTable.GetByType(gachaTypeId);
    }

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

    private int GetCostItemId(GachaType type, int count)
    {
        var data = GetGachaData(type);
        if (data == null) return 0;

        return count == 10 ? data.Draw10_ItemID : data.ItemID;
    }

    /// <summary>
    /// 안전한 가챠 실행 - 재화 검사를 가장 먼저!
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

        // 재화 복구용 변수 (catch에서 접근 가능하도록)
        int costItemId = 0;
        int costAmount = 0;
        bool costDeducted = false;

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
            costItemId = GetCostItemId(type, count);
            costAmount = GetGachaCost(type, count);



            // ⭐ 재화 검사를 가장 먼저! (DB 차감 전)
            var itemData = DataTableManager.ItemTable.Get(costItemId);
            if (itemData == null)
            {
                Debug.LogError($"[GachaManager] 아이템 정보를 찾을 수 없음: {costItemId}");
                OnGachaError?.Invoke("아이템 정보를 찾을 수 없습니다.");
                return null;
            }

            int currentCount = PlayData.GetItemCount(costItemId);
            string itemName = DataTableManager.GetString(itemData.ITEM_NAME) ?? $"아이템 {costItemId}";
            if (currentCount < costAmount)
            {
                // ⭐ 재화 부족 → 즉시 에러 이벤트 발생 후 중단!
                Debug.LogWarning($"[GachaManager] {itemName} 부족: 보유 {currentCount}, 필요 {costAmount}");
                OnGachaError?.Invoke($"뽑기 아이템이 부족합니다.");
                return null;
            }

            // 재화 로컬 즉시 차감
            DeductCostLocal(costItemId, costAmount);
            costDeducted = true;

            // 디버그!
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"[가챠 재화 검사]");
            Debug.Log($"타입: {type}, 횟수: {count}");
            Debug.Log($"필요 아이템 ID: {costItemId}");
            Debug.Log($"필요 수량: {costAmount}");
            Debug.Log($"현재 보유량: {currentCount}");
            Debug.Log($"검사 결과: {(currentCount >= costAmount ? "✅ 충분" : "❌ 부족")}");
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            // 뽑기 실행
            List<GachaItem> results = new List<GachaItem>();
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
                // 재화 복구 (로컬에서 이미 차감됨)
                int restoredCount = PlayData.GetItemCount(costItemId);
                PlayData.SetItemCountImmediate(costItemId, restoredCount + costAmount);
                Debug.LogError("[GachaManager] 뽑기 결과가 0개입니다! 재화 복구됨.");
                OnGachaError?.Invoke("뽑기 결과가 없습니다.");
                return null;
            }

            // 아이콘 미리 로드
            await PreloadResultIconsAsync(results);

            // 캐릭터/조각 처리 - 로컬 즉시 업데이트, Firebase는 백그라운드
            HashSet<int> processedUnitsThisGacha = new HashSet<int>();
            List<UniTask> firebaseSaveTasks = new List<UniTask>();

            // 재화 차감도 Firebase에 저장
            firebaseSaveTasks.Add(DeductCostFirebaseAsync(costItemId, costAmount));

            foreach (var item in results)
            {
                int unitId = item.unitId;
                string characterId = unitId.ToString();

                bool alreadyOwnedInDB = PlayData.HasCharacter(characterId);
                bool alreadyProcessedInThisGacha = processedUnitsThisGacha.Contains(unitId);

                if (!alreadyOwnedInDB && !alreadyProcessedInThisGacha)
                {
                    item.isDuplicate = false;
                    item.isNew = true;

                    // 로컬 캐시에 캐릭터 즉시 추가
                    DatabaseManager.Instance.AddCharacterLocal(characterId, 1);
                    // Firebase는 백그라운드
                    firebaseSaveTasks.Add(DatabaseManager.Instance.AddCharacterFirebaseAsync(characterId, 1));
                    processedUnitsThisGacha.Add(unitId);
                }
                else
                {
                    item.isDuplicate = true;
                    item.isNew = false;

                    var unitData = DataTableManager.UnitTable.Get(unitId);
                    if (unitData != null && unitData.FRAGMENT_ITEM_ID > 0)
                    {
                        // 로컬 캐시에 조각 즉시 추가
                        PlayData.SetItemCountImmediate(unitData.FRAGMENT_ITEM_ID,
                            PlayData.GetItemCount(unitData.FRAGMENT_ITEM_ID) + 1);
                        // Firebase는 백그라운드
                        firebaseSaveTasks.Add(DatabaseManager.Instance.AddItemAsync(unitData.FRAGMENT_ITEM_ID, 1));
                    }
                }
            }

            // Firebase 저장 작업 생성
            var saveTask = UniTask.WhenAll(firebaseSaveTasks);

            // PendingSaveManager로도 추적 (앱 종료 시 보장)
            PendingSaveManager.Track(saveTask);

            PlayData.SyncCharactersFromDatabase();

            UpdateGachaAchievementsAsync(type, count, results).Forget();

            GachaResult result = new GachaResult(results, type);
            // 저장 작업을 result에 첨부 (문 열림 애니메이션 중 대기용)
            result.SaveTask = saveTask;

            OnGachaComplete?.Invoke(result);
            return result;
        }
        catch (Exception ex)
        {
            // 재화 복구 (로컬에서 이미 차감된 경우)
            if (costDeducted && costItemId > 0 && costAmount > 0)
            {
                int restoredCount = PlayData.GetItemCount(costItemId);
                PlayData.SetItemCountImmediate(costItemId, restoredCount + costAmount);
                Debug.LogWarning($"[GachaManager] 오류 발생으로 재화 복구: {costAmount}개");
            }
            Debug.LogError($"[GachaManager] 가챠 실행 중 오류: {ex.Message}");
            OnGachaError?.Invoke("가챠 실행 중 오류가 발생했습니다.");
            return null;
        }
        finally
        {
            isExecuting = false;
        }
    }

    private async UniTask PreloadResultIconsAsync(List<GachaItem> results)
    {
        List<UniTask> loadTasks = new List<UniTask>();

        foreach (var item in results)
        {
            var unitData = DataTableManager.UnitTable?.Get(item.unitId);
            if (unitData != null && !string.IsNullOrEmpty(unitData.UNIT_ICON))
            {
                loadTasks.Add(SpriteCache.Instance.LoadSpriteAsync(unitData.UNIT_ICON).AsUniTask());
            }
        }

        if (loadTasks.Count > 0)
        {
            await UniTask.WhenAll(loadTasks);
            Debug.Log($"[GachaManager] 결과 아이콘 {loadTasks.Count}개 프리로드 완료");
        }
    }

    /// <summary>
    /// 재화 차감 - 로컬 즉시 업데이트, Firebase는 백그라운드
    /// </summary>
    private void DeductCostLocal(int itemId, int amount)
    {
        int currentCount = PlayData.GetItemCount(itemId);
        PlayData.SetItemCountImmediate(itemId, currentCount - amount);
    }

    private UniTask DeductCostFirebaseAsync(int itemId, int amount)
    {
        return DatabaseManager.Instance.AddItemAsync(itemId, -amount);
    }

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

    private async UniTask UpdateGachaAchievementsAsync(GachaType type, int count, List<GachaItem> results)
    {
        await AchievementManager.AddGachaTotalAsync(count);

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
