using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class LevelUpRewardController : MonoBehaviour
{
    [Header("UI Prefabs")]
    [SerializeField] private UnitCardUi unitCardUiPrf;
    [SerializeField] private SkillCardUi skillCardPrf;
    [Header("UI")]
    [SerializeField] private Button reRollBtn;
    [SerializeField] private Button confirmBtn;
    [SerializeField] private TextMeshProUGUI rerollCostText;
    [Header("Others")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StageUiManager uiManager;
    [SerializeField] private PlayerStageGold playerCredit;
    [SerializeField] private PlayerExperience playerExp;
    [SerializeField] private PassiveSkillManager passiveSkillManager;
    [SerializeField] private DragManager dragManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BattleUnitManager battleUnitManager;
    [SerializeField] private StageManager stageManager;

    private int rerollCost = 50;

    private UnitCardUi[] unitCardUIs;
    private SkillCardUi[] skillCardUIs;

    private HorizontalLayoutGroup layoutGroup;

    // 현재 레벨업 보상으로 생성된 유닛들을 추적
    private readonly List<GridUnit> currentLevelUpRewardUnits = new();

    // 현재 선택된 스킬 카드 추적
    private SkillCardUi selectedSkillCard = null;

    private Dictionary<int, UnitGridData> gridDataCache = new Dictionary<int, UnitGridData>();
    private Dictionary<int, Sprite> spriteCache = new Dictionary<int, Sprite>();
    public Dictionary<int, Sprite> SpriteCache => spriteCache; // 테스트용**

    private bool isSelectedReward = false;
    private int defaultGoldReward = 25; // 보상 패스 시 지급 골드
    private int passiveSkillGoldReward = 50; // 패시브 스킬 골드 카드 보상
    private bool isSkillCardSelecting = false; // 스킬 카드 선택 중 플래그

    // 애니메이션 상수
    private const float CardAnimDuration = 0.25f; // 카드 스케일 애니메이션 시간
    private const float CardAnimDelayInterval = 0.08f; // 카드 애니메이션 간격
    private const float SelectedCardMoveY = 60f; // 선택된 카드 Y축 이동 거리
    private const float SelectedCardScale = 1.1f; // 선택된 카드 최대 스케일
    private const float SelectedCardWaitTime = 0.2f; // 선택된 카드 대기 시간

    private const int DefaultRerollCost = 50;

    // 초기 세팅
    private void Start()
    {
        // 각 프리팹 카드 3장씩 생성
        CreateUnitCardPrf(3);
        CreateSkillCardPrf(3);

        // Layout Group 캐싱
        layoutGroup = GetComponent<HorizontalLayoutGroup>();

        playerExp.OnLevelUp += DrawLevelUpReward;
        playerCredit.OnChangeGold += UpdateRerollBtn;

        // 드래그 이벤트 구독
        if (dragManager != null)
        {
            dragManager.OnDragStarted += OnDragStarted;
            dragManager.OnDragEnded += OnDragEnded;
        }
        else
        {
            Debug.LogError("DragManager를 할당해주세요.");
        }

        CacheAllData();

        SelectUnitOnGameStart();
    }

    // 드래그 시작 시 호출
    private void OnDragStarted()
    {
        if (confirmBtn != null)
        {
            confirmBtn.interactable = false;
        }
    }

    // 드래그 종료 시 호출
    private void OnDragEnded()
    {
        if (confirmBtn == null)
            return;

        // 게임이 시작되지 않았고 보상을 선택하지 않았으면 비활성화 유지
        if (!gameManager.IsGameStarted && !isSelectedReward)
        {
            confirmBtn.interactable = false;
        }
        else
        {
            confirmBtn.interactable = true;
        }
    }

    private void OnEnable()
    {
        // 패널 활성화 시 모든 카드 상태 초기화
        ResetAllCards();
    }

    private void OnDisable()
    {
        // 패널 비활성화 시 모든 애니메이션 정리 및 카드 강제 비활성화
        CleanupAllAnimations();
    }

    private void OnDestroy()
    {
        playerExp.OnLevelUp -= DrawLevelUpReward;
        playerCredit.OnChangeGold -= UpdateRerollBtn;

        // 드래그 이벤트 구독 해제
        if (dragManager != null)
        {
            dragManager.OnDragStarted -= OnDragStarted;
            dragManager.OnDragEnded -= OnDragEnded;
        }

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
        isSkillCardSelecting = false; // 스킬 카드 선택 플래그 초기화

        // 기존 카드들을 모두 비활성화
        SetActiveCards(skillCardUIs, false);
        SetActiveCards(unitCardUIs, false);
        uiManager.SetGameControllBtnsActive(false);

        // 그리드가 꽉 차있고 모든 유닛이 2성 이상이면 무조건 스킬 뽑기
        bool forceSkillDraw = ShouldForceSkillDraw();

        // 강제 스킬 뽑기 조건이거나 플레이어 레벨이 3의 배수일 때 스킬 뽑기
        if (forceSkillDraw || playerExp.Level % 3 == 0)
        {
            DrawPassiveSkills();
            SetActiveCards(skillCardUIs, true);

            if (forceSkillDraw)
            {
                Debug.Log("[LevelUpRewardController] 그리드가 꽉 차고 모든 유닛이 2성 이상이므로 패시브 스킬을 강제로 뽑습니다.");
            }
        }
        // 그 외에는 유닛 뽑기
        else
        {
            DrawUnitID();

            // 드래그 상태 활성화 (카드 활성화 전에 설정)
            for (int i = 0; i < unitCardUIs.Length; i++)
            {
                unitCardUIs[i].SetDragState(true);
                unitCardUIs[i].SetColor();
            }

            SetActiveCards(unitCardUIs, true);
        }
    }

    // 강제로 스킬 뽑기를 해야 하는지 확인
    private bool ShouldForceSkillDraw()
    {
        // GridManager와 BattleUnitManager가 할당되지 않았으면 false
        if (gridManager == null || battleUnitManager == null)
            return false;

        // 그리드가 꽉 차있고 모든 유닛이 2성 이상이면 true
        return gridManager.IsGridFull() && battleUnitManager.AreAllUnitsAtLeastTwoStar();
    }

    // 스킬 카드가 즉시 획득되었을 때 호출
    public void OnSkillCardAcquired(SkillCardUi selectedCard)
    {
        // 이미 선택 중이면 무시
        if (isSkillCardSelecting)
            return;

        // 선택 중 플래그 설정
        isSkillCardSelecting = true;

        // Layout Group 비활성화 (위치 고정)
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        // 선택된 카드를 제외하고 나머지만 역순으로 작아지면서 비활성화
        for (int i = 0; i < skillCardUIs.Length; i++)
        {
            var card = skillCardUIs[i];

            // 선택된 카드는 특별한 애니메이션 적용
            if (card == selectedCard)
            {
                // 기존 애니메이션 정리
                card.transform.DOKill();

                Vector3 originalPos = card.transform.localPosition;

                // 시퀀스로 애니메이션 체이닝
                Sequence selectedSequence = DOTween.Sequence();
                selectedSequence.SetUpdate(true); // TimeScale 무시

                selectedSequence.Append(card.transform.DOLocalMoveY(originalPos.y + SelectedCardMoveY, CardAnimDuration).SetEase(Ease.OutQuad));
                selectedSequence.Join(card.transform.DOScale(SelectedCardScale, CardAnimDuration).SetEase(Ease.OutQuad));

                // 카드가 위로 올라간 후 별 색칠 시작
                selectedSequence.AppendCallback(() =>
                {
                    bool canFillStar = card.FillNextStar();
                });

                selectedSequence.AppendInterval(SelectedCardWaitTime);

                selectedSequence.Append(card.transform.DOScale(0f, CardAnimDuration).SetEase(Ease.InBack));

                selectedSequence.OnComplete(() => card.gameObject.SetActive(false));

                continue;
            }

            int reverseIndex = skillCardUIs.Length - 1 - i; // 역순 인덱스

            // 기존 애니메이션 정리
            card.transform.DOKill();

            card.transform.DOScale(Vector3.zero, CardAnimDuration)
                .SetDelay(reverseIndex * CardAnimDelayInterval)
                .SetEase(Ease.InBack)
                .SetUpdate(true) // TimeScale 무시
                .OnComplete(() => card.gameObject.SetActive(false));
        }

        isSelectedReward = true;

        // 스킬 획득 시 확인 버튼 활성화
        if (confirmBtn != null)
            confirmBtn.interactable = true;
    }

    // 골드 카드가 선택되었을 때 호출
    public void OnGoldCardAcquired(SkillCardUi selectedCard, int goldAmount)
    {
        // 이미 선택 중이면 무시
        if (isSkillCardSelecting)
            return;

        // 선택 중 플래그 설정
        isSkillCardSelecting = true;

        // 골드 지급 (스테이지 클리어 골드로 누적)
        if (stageManager != null)
            stageManager.AddBonusGold(goldAmount);
        uiManager.UpdateInfoText($"{goldAmount}G 획득!");

        // Layout Group 비활성화 (위치 고정)
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        // 선택된 카드를 제외하고 나머지만 역순으로 작아지면서 비활성화
        for (int i = 0; i < skillCardUIs.Length; i++)
        {
            var card = skillCardUIs[i];

            // 선택된 카드는 특별한 애니메이션 적용
            if (card == selectedCard)
            {
                card.transform.DOKill();

                Vector3 originalPos = card.transform.localPosition;

                Sequence selectedSequence = DOTween.Sequence();
                selectedSequence.SetUpdate(true);

                selectedSequence.Append(card.transform.DOLocalMoveY(originalPos.y + SelectedCardMoveY, CardAnimDuration).SetEase(Ease.OutQuad));
                selectedSequence.Join(card.transform.DOScale(SelectedCardScale, CardAnimDuration).SetEase(Ease.OutQuad));

                selectedSequence.AppendInterval(SelectedCardWaitTime);

                selectedSequence.Append(card.transform.DOScale(0f, CardAnimDuration).SetEase(Ease.InBack));

                selectedSequence.OnComplete(() => card.gameObject.SetActive(false));

                continue;
            }

            int reverseIndex = skillCardUIs.Length - 1 - i;

            card.transform.DOKill();

            card.transform.DOScale(Vector3.zero, CardAnimDuration)
                .SetDelay(reverseIndex * CardAnimDelayInterval)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() => card.gameObject.SetActive(false));
        }

        isSelectedReward = true;

        if (confirmBtn != null)
            confirmBtn.interactable = true;
    }

    // 스킬 카드 선택 가능 여부 확인
    public bool CanSelectSkillCard()
    {
        return !isSkillCardSelecting;
    }

    // 완료 버튼 클릭
    public void OnClickConfirmBtn()
    {
        // 보상을 선택하지 않은채로 확인 버튼을 누를 경우 25G 지급 (인게임 골드)
        if (!isSelectedReward)
        {
            playerCredit.AddCredit(defaultGoldReward);
            uiManager.UpdateInfoText($"보상 선택을 패스하고 {defaultGoldReward}크레딧 지급");
        }

        // 스킬은 이미 카드 클릭 시 적용되었으므로 여기서는 처리 안 함

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
                var sprite = AddressablePreloader.Instance.GetCachedSprite(unitData.UNIT_ICON_FULL);
                if (sprite != null)
                    spriteCache[unitId] = sprite;

                // 2성 스프라이트 (unitId + 101)
                int star2UnitId = unitId + 101;
                var unitData2 = DataTableManager.UnitTable?.Get(star2UnitId);
                if (unitData2 != null && !string.IsNullOrEmpty(unitData2.UNIT_ICON))
                {
                    var sprite2 = AddressablePreloader.Instance.GetCachedSprite(unitData2.UNIT_ICON_FULL);
                    if (sprite2 != null)
                        spriteCache[star2UnitId] = sprite2;
                }

                // 3성 스프라이트 (unitId + 202)
                int star3UnitId = unitId + 202;
                var unitData3 = DataTableManager.UnitTable?.Get(star3UnitId);
                if (unitData3 != null && !string.IsNullOrEmpty(unitData3.UNIT_ICON))
                {
                    var sprite3 = AddressablePreloader.Instance.GetCachedSprite(unitData3.UNIT_ICON_FULL);
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
                skillCardUIs[i].SetAsPassiveSkillCard(randomSkillIds[i]);
                skillCardUIs[i].gameObject.SetActive(true);
            }
            else
            {
                // 스킬이 부족하면 골드 카드로 대체
                skillCardUIs[i].SetAsGoldCard(passiveSkillGoldReward);
                skillCardUIs[i].gameObject.SetActive(true);
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
        rerollCostText.text =  rerollCost.ToString();

        // 이미 보상을 획득했으면 리롤 버튼 상태 업데이트 안 함
        if (isSelectedReward)
            return;

        if (playerCredit.Credit < rerollCost)
            reRollBtn.interactable = false;
        else
            reRollBtn.interactable = true;
    }

    // 리롤
    public void OnClickRerollBtn()
    {
        if (playerCredit.UseCredit(rerollCost))
        {
            // 버튼 스케일 애니메이션 (기존 애니메이션 정리 후 실행)
            if (reRollBtn != null)
            {
                reRollBtn.transform.DOKill();
                reRollBtn.transform.DOScale(0.9f, 0.1f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        reRollBtn.transform.DOScale(1f, 0.1f)
                            .SetEase(Ease.OutQuad)
                            .SetUpdate(true);
                    });
            }

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

            // 리롤 코스트 증가
            rerollCost *= 2;

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
        if (value)
        {
            // Layout Group 활성화 (위치 재계산)
            if (layoutGroup != null)
                layoutGroup.enabled = true;

            // 활성화할 때 순차적 스케일 애니메이션
            for (int i = 0; i < cardArray.Length; i++)
            {
                var card = cardArray[i];

                // 기존 애니메이션 정리
                card.transform.DOKill();

                card.gameObject.SetActive(true);

                // 초기 스케일을 0으로 설정
                card.transform.localScale = Vector3.zero;

                // 순차적으로 스케일 애니메이션
                card.transform.DOScale(Vector3.one, CardAnimDuration)
                    .SetDelay(i * CardAnimDelayInterval)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true); // TimeScale 무시
            }
        }
        else
        {
            // Layout Group 비활성화 (위치 고정)
            if (layoutGroup != null)
                layoutGroup.enabled = false;

            // 비활성화할 때 역순으로 작아지면서 비활성화
            for (int i = 0; i < cardArray.Length; i++)
            {
                var card = cardArray[i];
                int reverseIndex = cardArray.Length - 1 - i; // 역순 인덱스

                // 기존 애니메이션 정리
                card.transform.DOKill();

                card.transform.DOScale(Vector3.zero, CardAnimDuration)
                    .SetDelay(reverseIndex * CardAnimDelayInterval)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true) // TimeScale 무시
                    .OnComplete(() => card.gameObject.SetActive(false));
            }
        }
    }

    // 유닛 카드의 드롭 성공 처리
    private void OnUnitCardDropSuccess()
    {
        // 드래그 상태 비활성화
        for (int i = 0; i < unitCardUIs.Length; i++)
        {
            unitCardUIs[i].SetDragState(false);
        }

        // 역순 애니메이션으로 카드 비활성화
        SetActiveCards(unitCardUIs, false);

        reRollBtn.interactable = false;
        isSelectedReward = true;

        // 유닛 드롭 시 확인 버튼 활성화
        if (confirmBtn != null)
            confirmBtn.interactable = true;
    }

    // 모든 카드 애니메이션 정리 및 강제 비활성화
    private void CleanupAllAnimations()
    {
        if (unitCardUIs != null)
        {
            foreach (var card in unitCardUIs)
            {
                if (card != null)
                {
                    card.transform.DOKill();
                    card.gameObject.SetActive(false);
                }
            }
        }

        if (skillCardUIs != null)
        {
            foreach (var card in skillCardUIs)
            {
                if (card != null)
                {
                    card.transform.DOKill();
                    card.SetFocus(false);
                    card.gameObject.SetActive(false);
                }
            }
        }
    }

    // 모든 카드 상태 초기화
    private void ResetAllCards()
    {
        if (unitCardUIs != null)
        {
            foreach (var card in unitCardUIs)
            {
                if (card != null)
                {
                    card.transform.localScale = Vector3.one;
                    card.transform.localPosition = Vector3.zero;
                    card.gameObject.SetActive(false);
                }
            }
        }

        if (skillCardUIs != null)
        {
            foreach (var card in skillCardUIs)
            {
                if (card != null)
                {
                    card.transform.localScale = Vector3.one;
                    card.transform.localPosition = Vector3.zero;
                    card.SetFocus(false);
                    card.gameObject.SetActive(false);
                }
            }
        }
    }
}