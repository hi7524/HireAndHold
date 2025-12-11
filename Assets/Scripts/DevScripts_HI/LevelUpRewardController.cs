using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpRewardController : MonoBehaviour
{
    [Header("UI Prefabs")]
    [SerializeField] private UnitCardUi unitCardUiPrf;
    [SerializeField] private SkillCardUi skillCardPrf;
    [Header("UI")]
    [SerializeField] private Button reRollBtn;
    [SerializeField] private Button confirmBtn;
    [Header("Others")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StageUiManager uiManager;
    [SerializeField] private PlayerStageGold playerStageGold;
    [SerializeField] private PlayerExperience playerExp;
    [SerializeField] private PassiveSkillManager passiveSkillManager;
    [SerializeField] private DragManager dragManager;

    private int rerollCost = 50;

    private UnitCardUi[] unitCardUIs;
    private SkillCardUi[] skillCardUIs;

    // 현재 레벨업 보상으로 생성된 유닛들을 추적
    private readonly List<GridUnit> currentLevelUpRewardUnits = new();

    // 현재 선택된 스킬 카드 추적
    private SkillCardUi selectedSkillCard = null;

    private Dictionary<int, UnitGridData> gridDataCache = new Dictionary<int, UnitGridData>();
    private Dictionary<int, Sprite> spriteCache = new Dictionary<int, Sprite>();
    public Dictionary<int, Sprite> SpriteCache => spriteCache; // 테스트용**

    private bool isSelectedReward = false;
    private int defaultGoldReward = 25;


    // 초기 세팅
    private void Start()
    {
        // 각 프리팹 카드 3장씩 생성
        CreateUnitCardPrf(3);
        CreateSkillCardPrf(3);

        playerExp.OnLevelUp += DrawLevelUpReward;
        playerStageGold.OnChangeGold += UpdateRerollBtn;
        CacheAllData();

        SelectUnitOnGameStart();
    }

    private void OnDestroy()
    {
        playerExp.OnLevelUp -= DrawLevelUpReward;
        playerStageGold.OnChangeGold -= UpdateRerollBtn;

        // UnitCardUI 이벤트 구독 해제
        if (unitCardUIs != null)
        {
            foreach (var card in unitCardUIs)
            {
                if (card != null)
                {
                    card.OnUnitDropSuccess -= OnUnitCardDropSuccess;
                }
            }
        }

        // 캐시 클리어 (AddressablePreloader가 관리하므로 Release 불필요)
        gridDataCache.Clear();
        spriteCache.Clear();
    }

    // 레벨업 보상 유닛이 생성되었을 때 호출
    public void OnLevelUpRewardUnitSpawned(GridUnit unit)
    {
        if (unit != null && !currentLevelUpRewardUnits.Contains(unit))
        {
            currentLevelUpRewardUnits.Add(unit);
        }
    }

    // 이전 레벨업 보상 유닛들의 인벤토리 배치 허용
    private void EnableInventoryPlacementForPreviousRewards()
    {
        foreach (var unit in currentLevelUpRewardUnits)
        {
            if (unit != null)
            {
                unit.SetInventoryPlaceable(true);
            }
        }
        currentLevelUpRewardUnits.Clear();
    }

    // 스테이지 시작 전
    public void SelectUnitOnGameStart()
    {
        // 유닛뽑기
        DrawUnitID();
        SetActiveCards(unitCardUIs, true);

        // 관련 UI 활성화 및 비활성화
        uiManager.SetLevelUpRewardPanelActive(true);
        uiManager.SetGameControllBtnsActive(false);

        // 리롤 버튼 상태 갱신
        UpdateRerollBtn();

        // 게임 시작 시에만 확인 버튼 비활성화 (보상 선택 필수)
        if (confirmBtn != null)
            confirmBtn.interactable = false;
    }

    // 레벌업시 보상 랜덤 뽑기
    public void DrawLevelUpReward()
    {
        for (int i = 0; i < unitCardUIs.Length; i++)
        {
            unitCardUIs[i].SetDragState(true);
            unitCardUIs[i].SetColor();
        }

        DrawReward();

        // 리롤 버튼 상태 갱신
        UpdateRerollBtn();

        // 레벨업 시에는 확인 버튼 항상 활성화 (보상 패스 가능)
        if (confirmBtn != null)
            confirmBtn.interactable = true;

        gameManager.PauseGame();
    }

    // 실제 보상 뽑기
    private void DrawReward()
    {
        // 보상 획득 여부 초기화 (새로운 보상이므로 리셋)
        isSelectedReward = false;

        // 기존 카드들을 모두 비활성화
        SetActiveCards(skillCardUIs, false);
        SetActiveCards(unitCardUIs, false);
        uiManager.SetGameControllBtnsActive(false);

        // 플레이어 레벨이 3의 배수일 때 스킬 뽑기
        if (playerExp.Level % 3 == 0)
        {
            DrawPassiveSkills();
            SetActiveCards(skillCardUIs, true);
        }
        // 그 외에는 유닛 뽑기
        else
        {
            DrawUnitID();
            SetActiveCards(unitCardUIs, true);
        }
    }

    // 스킬 카드가 선택되었을 때 호출
    public void OnSkillCardSelected(SkillCardUi clickedCard)
    {
        // 이전에 선택된 카드가 있으면 포커스 해제
        if (selectedSkillCard != null)
        {
            selectedSkillCard.SetFocus(false);
        }

        // 새로운 카드 선택
        selectedSkillCard = clickedCard;
        selectedSkillCard.SetFocus(true);
        isSelectedReward = true;

        // 스킬 선택 시 확인 버튼 활성화
        if (confirmBtn != null)
            confirmBtn.interactable = true;
    }

    // 완료 버튼 클릭
    public void OnClickConfirmBtn()
    {
        Debug.Log("[LevelUpRewardController] OnClickConfirmBtn 호출됨");

        // 드래그 중인 유닛이 있으면 드래그 취소
        if (dragManager != null)
        {
            dragManager.CancelDrag();
        }

        // 보상을 선택하지 않은채로 확인 버튼을 누를 경우 25G 지급
        if (!isSelectedReward)
        {
            playerStageGold.AddGold(defaultGoldReward);
            uiManager.UpdateInfoText($"보상 선택을 패스하고 {defaultGoldReward}G 지급");
        }

        // 스킬 카드가 선택된 경우 스킬 적용
        if (selectedSkillCard != null && selectedSkillCard.IsSelected)
        {
            bool success = selectedSkillCard.ApplySkill();
            if (!success)
            {
                Debug.LogWarning("스킬 적용 실패. 다시 선택해주세요.");
                return;
            }

            // 선택 상태 초기화
            selectedSkillCard.SetFocus(false);
            selectedSkillCard = null;
        }

        // 현재 레벨업 보상 유닛들의 인벤토리 배치 허용
        EnableInventoryPlacementForPreviousRewards();

        // 관련 UI 비활성화 및 활성화 (패널을 끄면 자식들도 자동으로 꺼짐)
        uiManager.SetLevelUpRewardPanelActive(false);
        uiManager.SetGameControllBtnsActive(true);

        if (!gameManager.IsGameStarted)
            gameManager.StartGame();
        else
            gameManager.ResumeGame();
    }

    // 모든 선택 가능한 유닛의 GridData, sprite를 미리 캐싱 (AddressablePreloader에서 가져옴)
    private void CacheAllData()
    {
        gridDataCache.Clear();
        spriteCache.Clear();

        foreach (int unitId in PlayData.selectedUnitIds)
        {
            var unitData = DataTableManager.UnitTable?.Get(unitId);

            if (unitData == null)
                continue;

            // GridData 캐시에서 가져오기
            if (!string.IsNullOrEmpty(unitData.GRID_DATA))
            {
                var gridData = AddressablePreloader.Instance.GetCachedGridData(unitData.GRID_DATA);
                if (gridData != null)
                    gridDataCache[unitId] = gridData;
            }

            // Sprite 캐시에서 가져오기 (1성, 2성, 3성 모두)
            if (!string.IsNullOrEmpty(unitData.UNIT_ICON))
            {
                // 1성 스프라이트
                var sprite = AddressablePreloader.Instance.GetCachedSprite(unitData.UNIT_ICON);
                if (sprite != null)
                    spriteCache[unitId] = sprite;

                // 2성 스프라이트 (unitId + 101)
                int star2UnitId = unitId + 101;
                var unitData2 = DataTableManager.UnitTable?.Get(star2UnitId);
                if (unitData2 != null && !string.IsNullOrEmpty(unitData2.UNIT_ICON))
                {
                    var sprite2 = AddressablePreloader.Instance.GetCachedSprite(unitData2.UNIT_ICON);
                    if (sprite2 != null)
                        spriteCache[star2UnitId] = sprite2;
                }

                // 3성 스프라이트 (unitId + 202)
                int star3UnitId = unitId + 202;
                var unitData3 = DataTableManager.UnitTable?.Get(star3UnitId);
                if (unitData3 != null && !string.IsNullOrEmpty(unitData3.UNIT_ICON))
                {
                    var sprite3 = AddressablePreloader.Instance.GetCachedSprite(unitData3.UNIT_ICON);
                    if (sprite3 != null)
                        spriteCache[star3UnitId] = sprite3;
                }
            }
        }
    }

    // 유닛 3개 중복 없이 뽑기
    public void DrawUnitID()
    {
        List<int> tempList = new List<int>(PlayData.selectedUnitIds);

        for (int i = tempList.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            int temp = tempList[i];
            tempList[i] = tempList[randomIndex];
            tempList[randomIndex] = temp;
        }

        for (int i = 0; i < 3 && i < unitCardUIs.Length; i++)
        {
            int unitId = tempList[i];
            unitCardUIs[i].SetUnitID(unitId);

            // 캐시에서 GridData 가져오기
            if (gridDataCache.TryGetValue(unitId, out var gridData))
            {
                unitCardUIs[i].SetGridData(gridData);
                unitCardUIs[i].SetImage(spriteCache[unitId]);
            }
            else
            {
                Debug.LogError($"UnitID {unitId}의 GridData가 캐시에 없습니다.");
            }
        }
    }
    public void DrawPassiveSkills()
    {
        if (passiveSkillManager == null)
        {
            Debug.LogError("[LevelUpRewardController] PassiveSkillManager가 할당되지 않았습니다!");
            return;
        }

        // 중복 없이 3개의 패시브 스킬 ID 가져오기
        List<int> randomSkillIds = passiveSkillManager.GetRandomPassiveSkillsForReward(3);

        // 가져온 스킬 ID들을 각 카드에 설정
        for (int i = 0; i < skillCardUIs.Length; i++)
        {
            if (i < randomSkillIds.Count)
            {
                // 스킬 ID가 있으면 설정하고 활성화
                skillCardUIs[i].SetPassiveSkillId(randomSkillIds[i]);
                skillCardUIs[i].gameObject.SetActive(true);
            }
            else
            {
                // 스킬이 부족하면 돈을 주거나
                Debug.LogWarning($"[LevelUpRewardController] {i + 1}번째 스킬 카드를 표시할 수 없습니다.");
            }
        }
    }

    public int DrawSkill()
    {
        return 1; // 수정 필요**
    }

    // 리롤 버튼 상호작용 상태 설정
    private void UpdateRerollBtn()
    {
        // 이미 보상을 획득했으면 리롤 버튼 상태 업데이트 안 함
        if (isSelectedReward)
            return;

        if (playerStageGold.Gold < rerollCost)
            reRollBtn.interactable = false;
        else
            reRollBtn.interactable = true;
    }

    // 리롤
    public void OnClickRerollBtn()
    {
        if (playerStageGold.UseGold(rerollCost))
        {
            // 선택된 스킬 카드 초기화
            if (selectedSkillCard != null)
            {
                selectedSkillCard.SetFocus(false);
                selectedSkillCard = null;
            }

            // 현재 레벨업 보상 유닛들의 인벤토리 배치 허용
            EnableInventoryPlacementForPreviousRewards();

            DrawReward(); // DrawReward 내부에서 isSelectedReward = false로 초기화됨

            // 리롤 시 카드 드래그 다시 활성화
            for (int i = 0; i < unitCardUIs.Length; i++)
            {
                unitCardUIs[i].SetDragState(true);
                unitCardUIs[i].SetColor();
            }

            // 리롤 후 버튼 상태 갱신
            UpdateRerollBtn();
        }
    }

    // UnitCardUI 생성
    private void CreateUnitCardPrf(int amount)
    {
        unitCardUIs = new UnitCardUi[amount];

        for (int i = 0; i < amount; i++)
        {
            var card = Instantiate(unitCardUiPrf, transform);
            unitCardUIs[i] = card;
            card.gameObject.SetActive(false);

            // 각 카드의 드롭 성공 이벤트 구독
            card.OnUnitDropSuccess += OnUnitCardDropSuccess;
        }
    }

    // SkillCardUI 생성
    private void CreateSkillCardPrf(int amount)
    {
        skillCardUIs = new SkillCardUi[amount];

        for (int i = 0; i < amount; i++)
        {
            var card = Instantiate(skillCardPrf, transform);
            skillCardUIs[i] = card;
            card.SetLevelUpRewardController(this);
            card.SetPassiveSkillManager(passiveSkillManager);
            card.gameObject.SetActive(false);
        }
    }

    // 일부 활성화
    private void SetActiveCards(BaseCardUi[] cardArray, bool value)
    {
        for (int i = 0; i < cardArray.Length; i++)
        {
            cardArray[i].gameObject.SetActive(value);
        }
    }

    // 유닛 카드의 드롭 성공 처리
    private void OnUnitCardDropSuccess()
    {
        for (int i = 0; i < unitCardUIs.Length; i++)
        {
            unitCardUIs[i].SetDragState(false);
            unitCardUIs[i].SetColor(new Color(0.267f, 0.267f, 0.267f));
        }

        reRollBtn.interactable = false;
        isSelectedReward = true;

        // 유닛 드롭 시 확인 버튼 활성화
        if (confirmBtn != null)
            confirmBtn.interactable = true;
    }
}