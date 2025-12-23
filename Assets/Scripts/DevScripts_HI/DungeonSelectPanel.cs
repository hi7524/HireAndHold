using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DungeonSelectPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI curStageText;
    [SerializeField] private TextMeshProUGUI requireResourcesText;

    [SerializeField] private Button prevBtn;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Button enterButton;

    private int maxStage;
    private int curSelectedStage = 1;
    private Dictionary<int, int> stageToIdMap = new Dictionary<int, int>(); // Stage -> DungeonID 매핑

    private void Start()
    {
        InitializeDungeonList();
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
    }

    private void UpdateCurStageText(int stage)
    {
        curStageText.text = $"{stage}단계";
    }

    /// <summary>
    /// 보유 중인 강화석 수 업데이트
    /// </summary>
    private void UpdateRequireResourcesText()
    {
        if (requireResourcesText != null)
        {
            int enhanceStone = PlayData.EnhanceStone;
            requireResourcesText.text = $"보유: {enhanceStone}";
        }
    }

    /// <summary>
    /// 현재 선택된 스테이지에 해당하는 던전 ID를 PlayData에 설정
    /// </summary>
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

    /// <summary>
    /// 던전 입장 버튼 클릭 시 호출
    /// </summary>
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

        Debug.Log($"[DungeonSelectPanel] 던전 입장 시작 - 스테이지: {curSelectedStage}, 던전 ID: {PlayData.OreDungeonID}");

        await EnterDungeonAsync();
    }

    /// <summary>
    /// 던전으로 씬 전환
    /// </summary>
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
}