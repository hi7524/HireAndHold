using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class DungeonSelectPanel : MonoBehaviour
{
    [SerializeField] private int dungoenSettingID = 1401;
    [Space]
    [SerializeField] private TextMeshProUGUI dungeonNameText;
    [SerializeField] private TextMeshProUGUI dungeonDescriptionText;
    [SerializeField] private TextMeshProUGUI curStageText;
    [SerializeField] private Image requireResourcesIcon;
    [SerializeField] private TextMeshProUGUI requireResourcesText;

    [SerializeField] private Button prevBtn;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Button enterButton;

    private const int requireAmount = 1;
    private int maxStage;
    private int curSelectedStage = 1;
    private Dictionary<int, int> stageToIdMap = new Dictionary<int, int>();

    private async void Start()
    {
        InitializeDungeonList();

        // 던전 입장권 자정 체크 및 리셋
        await CheckAndResetDungeonEntries();

        await UpdateDungeonInfo();

        UpdateCurStageText(curSelectedStage);
        UpdateButtonStates();
        UpdateRequireResourcesText();
    }

    private void OnEnable()
    {
        // PlayData의 재화 변경 이벤트 구독
        PlayData.OnCurrencyChanged += UpdateRequireResourcesText;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        PlayData.OnCurrencyChanged -= UpdateRequireResourcesText;
    }

    private void InitializeDungeonList()
    {
        stageToIdMap.Clear();

        // OreDungeon 테이블에서 던전 개수 가져오기
        if (DataTableManager.OreDungeonTable != null)
        {
            var allDungeons = DataTableManager.OreDungeonTable.GetAll();
            maxStage = 0;

            // Stage -> DungeonID 매핑 생성 및 최대 스테이지 찾기
            foreach (var dungeon in allDungeons)
            {
                stageToIdMap[dungeon.Stage] = dungeon.DungeonID;

                if (dungeon.Stage > maxStage)
                    maxStage = dungeon.Stage;
            }

            if (maxStage == 0)
            {
                Debug.LogWarning("[DungeonSelectPanel] OreDungeon 테이블이 비어있습니다. 기본값 3으로 설정합니다.");
                maxStage = 3;
            }
        }
        else
        {
            Debug.LogWarning("[DungeonSelectPanel] OreDungeonTable이 null입니다. 기본값 3으로 설정합니다.");
            maxStage = 3;
        }

        Debug.Log($"[DungeonSelectPanel] 최대 던전 스테이지: {maxStage}");

        // 초기 선택 스테이지의 ID 설정
        UpdateSelectedDungeonId();
    }

    public void AddCurStage()
    {
        if (curSelectedStage >= maxStage)
            return;

        curSelectedStage++;
        UpdateCurStageText(curSelectedStage);
        UpdateButtonStates();
        UpdateSelectedDungeonId();
    }

    public void MinusCurStage()
    {
        if (curSelectedStage <= 1)
            return;

        curSelectedStage--;
        UpdateCurStageText(curSelectedStage);
        UpdateButtonStates();
        UpdateSelectedDungeonId();
    }

    private void UpdateButtonStates()
    {
        prevBtn.interactable = curSelectedStage > 1;
        nextBtn.interactable = curSelectedStage < maxStage;

        // 입장권이 부족하면 입장 버튼 비활성화
        UpdateEnterButtonState();
    }

    private void UpdateEnterButtonState()
    {
        if (enterButton != null)
        {
            var dungeonSetting = DataTableManager.DungeonSettingTable?.Get(dungoenSettingID);
            if (dungeonSetting != null)
            {
                int currentEntries = PlayData.GetItemCount(dungeonSetting.Conditions_Of_Entry_ItemID);
                enterButton.interactable = currentEntries >= requireAmount;
            }
        }
    }

    private void UpdateCurStageText(int stage)
    {
        curStageText.text = $"{stage}단계";
    }

    // 보유 중인 입장권 수 업데이트
    private void UpdateRequireResourcesText()
    {
        if (requireResourcesText != null)
        {
            // 현재 선택된 던전의 입장권 개수 표시 (보유량/필요량)
            var dungeonSetting = DataTableManager.DungeonSettingTable?.Get(dungoenSettingID);
            if (dungeonSetting != null)
            {
                int currentEntries = PlayData.GetItemCount(dungeonSetting.Conditions_Of_Entry_ItemID);
                requireResourcesText.text = $"{currentEntries}/{requireAmount}";
            }
        }

        // 입장 버튼 상태도 함께 업데이트
        UpdateEnterButtonState();
    }

    // 던전 입장권 자정 체크 및 리셋
    private async UniTask CheckAndResetDungeonEntries()
    {
        // 현재 던전의 입장권 체크 및 리셋
        await DatabaseManager.Instance.CheckAndResetDungeonEntriesAsync(dungoenSettingID);

        // PlayData의 던전 입장권 캐시 동기화
        PlayData.SyncDungeonEntriesFromDatabase();

        // 아이템 캐시도 동기화
        PlayData.SyncItemsFromDatabase();

        Debug.Log($"[DungeonSelectPanel] 던전 입장권 체크 완료 - 던전 ID: {dungoenSettingID}");
    }

    // 현재 선택된 스테이지에 해당하는 던전 ID를 PlayData에 설정
    private void UpdateSelectedDungeonId()
    {
        if (stageToIdMap.TryGetValue(curSelectedStage, out int dungeonId))
        {
            PlayData.SetSelectedOreDungeonId(dungeonId);
            Debug.Log($"[DungeonSelectPanel] 스테이지 {curSelectedStage} 선택 → 던전 ID: {dungeonId}");
        }
        else
        {
            Debug.LogWarning($"[DungeonSelectPanel] 스테이지 {curSelectedStage}에 해당하는 던전 ID를 찾을 수 없습니다.");
        }
    }

    // 던전 입장 버튼 클릭 시 호출
    public async void OnEnterDungeonButtonClick()
    {
        // 선택된 던전 ID가 유효한지 확인
        if (!stageToIdMap.ContainsKey(curSelectedStage))
        {
            Debug.LogError("[DungeonSelectPanel] 유효하지 않은 던전 스테이지입니다.");
            return;
        }

        // PlayData에 던전 ID 설정 (이미 UpdateSelectedDungeonId에서 설정되었지만 한 번 더 확인)
        UpdateSelectedDungeonId();

        // 던전 설정 가져오기
        var dungeonSetting = DataTableManager.DungeonSettingTable?.Get(dungoenSettingID);
        if (dungeonSetting == null)
        {
            Debug.LogError($"[DungeonSelectPanel] 던전 설정을 찾을 수 없음: {dungoenSettingID}");
            return;
        }

        // 입장권 체크 (이중 체크 - 버튼은 비활성화되지만 안전을 위해)
        int entryItemId = dungeonSetting.Conditions_Of_Entry_ItemID;
        if (!PlayData.HasEnoughItem(entryItemId, requireAmount))
        {
            Debug.LogWarning($"[DungeonSelectPanel] 입장권 부족 - ItemID: {entryItemId}");
            return;
        }

        // 입장권 소모
        bool success = await DatabaseManager.Instance.ConsumeDungeonEntryAsync(dungoenSettingID);
        if (!success)
        {
            Debug.LogError("[DungeonSelectPanel] 입장권 소모 실패");
            return;
        }

        // 캐시 동기화
        PlayData.SyncDungeonEntriesFromDatabase();
        PlayData.SyncItemsFromDatabase();
        UpdateRequireResourcesText();

        Debug.Log($"[DungeonSelectPanel] 던전 입장 시작 - 스테이지: {curSelectedStage}, 던전 ID: {PlayData.OreDungeonID}");

        await EnterDungeonAsync();
    }

    // 던전으로 씬 전환
    private async UniTask EnterDungeonAsync()
    {
        LoadingRequest request = new("OreDungeon");

        request.AddTask("던전 준비", async (ct) =>
        {
            // 필요한 데이터 준비 또는 검증
            await UniTask.Delay(300, cancellationToken: ct);
        }, weight: 1.0f);

        request.onLoadingComplete = () =>
        {
            Debug.Log($"[DungeonSelectPanel] 던전 입장 완료 - 던전 ID: {PlayData.OreDungeonID}");
        };

        await LoadingSceneManager.Instance.LoadSceneWithLoading(request);
    }

    // DungeonSetting 테이블에서 던전 이름, 설명, 아이콘을 업데이트
    private async UniTask UpdateDungeonInfo()
    {
        Debug.Log($"[DungeonSelectPanel] UpdateDungeonInfo 시작 - dungoenSettingID: {dungoenSettingID}");

        if (DataTableManager.DungeonSettingTable == null)
        {
            Debug.LogWarning("[DungeonSelectPanel] DungeonSettingTable이 null입니다.");
            return;
        }

        var dungeonSetting = DataTableManager.DungeonSettingTable.Get(dungoenSettingID);
        if (dungeonSetting == null)
        {
            Debug.LogWarning($"[DungeonSelectPanel] DungeonSettingID {dungoenSettingID}를 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"[DungeonSelectPanel] DungeonSetting 찾음 - Dungeon_Name ID: {dungeonSetting.Dungeon_Name}, Dungeon_Description ID: {dungeonSetting.Dungeon_Description}");

        // 던전 이름 업데이트
        if (dungeonNameText != null)
        {
            string dungeonName = DataTableManager.GetString(dungeonSetting.Dungeon_Name);
            Debug.Log($"[DungeonSelectPanel] Dungeon_Name 결과: {dungeonName}");
            if (!string.IsNullOrEmpty(dungeonName))
            {
                dungeonNameText.text = dungeonName;
            }
            else
            {
                Debug.LogWarning($"[DungeonSelectPanel] Dungeon_Name StringID {dungeonSetting.Dungeon_Name}를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("[DungeonSelectPanel] dungeonNameText가 null입니다. Inspector에서 연결하세요.");
        }

        // 던전 설명 업데이트
        if (dungeonDescriptionText != null)
        {
            string dungeonDescription = DataTableManager.GetString(dungeonSetting.Dungeon_Description);
            Debug.Log($"[DungeonSelectPanel] Dungeon_Description 결과: {dungeonDescription}");
            if (!string.IsNullOrEmpty(dungeonDescription))
            {
                dungeonDescriptionText.text = dungeonDescription;
            }
            else
            {
                Debug.LogWarning($"[DungeonSelectPanel] Dungeon_Description StringID {dungeonSetting.Dungeon_Description}를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("[DungeonSelectPanel] dungeonDescriptionText가 null입니다. Inspector에서 연결하세요.");
        }

        // 필요 재화 아이콘 업데이트
        if (requireResourcesIcon != null && DataTableManager.ItemTable != null)
        {
            var itemData = DataTableManager.ItemTable.Get(dungeonSetting.Conditions_Of_Entry_ItemID);
            if (itemData != null)
            {
                Debug.Log($"[DungeonSelectPanel] Item 찾음 - ItemID: {itemData.ITEM_ID}, ITEM_ICON: {itemData.ITEM_ICON}");

                if (!string.IsNullOrEmpty(itemData.ITEM_ICON))
                {
                    try
                    {
                        var sprite = await Addressables.LoadAssetAsync<Sprite>(itemData.ITEM_ICON).ToUniTask();
                        requireResourcesIcon.sprite = sprite;
                        Debug.Log($"[DungeonSelectPanel] 아이콘 로드 성공: {itemData.ITEM_ICON}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[DungeonSelectPanel] 아이콘 로드 실패: {itemData.ITEM_ICON}, 에러: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[DungeonSelectPanel] ITEM_ICON이 비어있습니다.");
                }
            }
            else
            {
                Debug.LogWarning($"[DungeonSelectPanel] ItemID {dungeonSetting.Conditions_Of_Entry_ItemID}를 찾을 수 없습니다.");
            }
        }
        else if (requireResourcesIcon == null)
        {
            Debug.LogWarning("[DungeonSelectPanel] requireResourcesIcon이 null입니다. Inspector에서 연결하세요.");
        }
    }
}