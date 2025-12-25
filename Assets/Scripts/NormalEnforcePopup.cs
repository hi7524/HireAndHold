using Cysharp.Threading.Tasks;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class NormalEnforcePopup : MonoBehaviour
{
    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Unit Display")]
    [SerializeField] private Image unitImage;

    [Header("Level Info")]
    [SerializeField] private TextMeshProUGUI levelCurrent;
    [SerializeField] private TextMeshProUGUI levelNext;
    [SerializeField] private TextMeshProUGUI powerCurrent;
    [SerializeField] private TextMeshProUGUI powerNext;

    [Header("Cost Info")]
    [SerializeField] private TextMeshProUGUI stoneHave;
    [SerializeField] private TextMeshProUGUI stoneNeed;
    [SerializeField] private TextMeshProUGUI goldHave;
    [SerializeField] private TextMeshProUGUI goldNeed;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    private NormalEnforceSystem enforceSystem;
    private UnitInfoUI mainUI;
    private UIPopupManager popupManager;
    private DataTable_Unit unitTable;

    private int currentUnitId;
    private Unit currentPreviewUnit;

    private const int NORMAL_MAX = 20;

    private void Start()
    {
        SetupPopup();
    }

    private void SetupPopup()
    {
        if (popupRoot != null && !popupRoot.activeSelf)
            popupRoot.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => popupRoot.SetActive(false));
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
    }

    public void SetPopupManager(UIPopupManager manager)
    {
        popupManager = manager;
    }

    public void SetEnforceSystem(
        NormalEnforceSystem system,
        UnitInfoUI ui,
        DataTable_Unit uTable)
    {
        enforceSystem = system;
        mainUI = ui;
        unitTable = uTable;
    }

    public void Open(int unitId, Unit previewUnit)
    {
        // 혹시 Start가 안 불렸을 경우를 대비
        if (closeButton != null && closeButton.onClick.GetPersistentEventCount() == 0)
            SetupPopup();

        if (popupRoot != null)
            popupRoot.SetActive(true);

        currentUnitId = unitId;
        currentPreviewUnit = previewUnit;

        Debug.Log($"[NormalEnforcePopup] Open: unitId={unitId}, 현재 골드={PlayData.Gold}, 현재 강화석={PlayData.EnhanceStone}");

        LoadUnitIconAsync().Forget();
        RefreshUI();
    }

    private async UniTaskVoid LoadUnitIconAsync()
    {
        if (unitImage == null || unitTable == null)
            return;

        var data = unitTable.Get(currentUnitId);
        if (data == null)
            return;

        var sprite = await SpriteCache.Instance.LoadSpriteAsync(data.UNIT_ICON);
        if (unitImage != null && sprite != null)
            unitImage.sprite = sprite;
    }

    private void RefreshUI()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null || currentPreviewUnit == null)
        {
            Debug.LogWarning($"[NormalEnforcePopup] RefreshUI: character or previewUnit is null");
            return;
        }

        int lv = character.enforceLevel; 

        if (levelCurrent != null)
            levelCurrent.text = lv.ToString();

        if (levelNext != null)
            levelNext.text = lv < NORMAL_MAX ? (lv + 1).ToString() : "MAX";

        float currAtk = currentPreviewUnit.GetAttackDamageStat().Value;

        if (powerCurrent != null)
            powerCurrent.text = currAtk.ToString();

        // 다음 공격력 계산
        if (lv < NORMAL_MAX)
        {
            float nextAtk = enforceSystem.GetNextAttack(currentPreviewUnit);
            if (powerNext != null)
                powerNext.text = nextAtk.ToString();
        }
        else
        {
            if (powerNext != null)
                powerNext.text = "-";
        }

        var (goldCost, stoneCost) = enforceSystem.GetNextEnforceCost(currentPreviewUnit);

        // PlayData에서 직접 가져오기
        long currentGold = PlayData.Gold;
        int currentStone = PlayData.EnhanceStone;

        Debug.Log($"[NormalEnforcePopup] RefreshUI: 보유 골드={currentGold}, 필요 골드={goldCost}, 보유 강화석={currentStone}, 필요 강화석={stoneCost}");

        if (stoneHave != null)
            stoneHave.text = currentStone.ToString();

        if (stoneNeed != null)
            stoneNeed.text = stoneCost.ToString();

        if (goldHave != null)
            goldHave.text = currentGold.ToString();

        if (goldNeed != null)
            goldNeed.text = goldCost.ToString();
    }


    private void OnConfirmClicked()
    {
        TryEnforce().Forget();
    }

    private async UniTaskVoid TryEnforce()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
        {
            popupManager?.ShowAlert("미보유 유닛입니다.");
            return;
        }

        if (currentPreviewUnit == null)
        {
            popupManager?.ShowAlert("유닛 데이터를 불러올 수 없습니다.");
            return;
        }

        int beforeLv = character.enforceLevel;
        int beforeAtk = Mathf.RoundToInt(currentPreviewUnit.GetAttackDamageStat().Value);

        Debug.Log($"[NormalEnforcePopup] 강화 시도: 강화 전 레벨={beforeLv}, 공격력={beforeAtk}");

        try
        {
            bool ok = await enforceSystem.TryEnforceAsync(currentPreviewUnit);

            if (!ok)
            {
                popupManager?.ShowAlert("재료가 부족합니다.");
                return;
            }

            // DB에서 최신 데이터 다시 로드
            character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());

            // PlayData 동기화
            PlayData.SyncCharactersFromDatabase();
            PlayData.NotifyCharacterUpdated(currentUnitId.ToString());

            int afterLv = character.enforceLevel;
            int afterAtk = Mathf.RoundToInt(currentPreviewUnit.GetAttackDamageStat().Value);

            Debug.Log($"[NormalEnforcePopup] 강화 성공: 강화 후 레벨={afterLv}, 공격력={afterAtk}, 남은 골드={PlayData.Gold}, 남은 강화석={PlayData.EnhanceStone}");

            popupManager?.ShowSuccess(
                "강화 성공",
                $"레벨 {beforeLv} → {afterLv}\n전투력 {beforeAtk} → {afterAtk}"
            );

            // UI 갱신
            if (mainUI != null)
            {
                mainUI.RefreshUI();
            }

            RefreshUI();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NormalEnforcePopup] 강화 실패: {ex}");
            popupManager?.ShowAlert("강화 중 오류가 발생했습니다.");
        }
    }

    private void RefreshCostUI()
    {
        if (popupRoot == null || !popupRoot.activeSelf)
            return;

        if (currentPreviewUnit == null)
            return;

        var (goldCost, stoneCost) = enforceSystem.GetNextEnforceCost(currentPreviewUnit);

        // PlayData에서 직접 가져오기
        long currentGold = PlayData.Gold;
        int currentStone = PlayData.EnhanceStone;

        Debug.Log($"[NormalEnforcePopup] RefreshCostUI: 보유 골드={currentGold}, 필요 골드={goldCost}, 보유 강화석={currentStone}, 필요 강화석={stoneCost}");

        if (goldHave != null)
            goldHave.text = currentGold.ToString();

        if (goldNeed != null)
            goldNeed.text = goldCost.ToString();

        if (stoneHave != null)
            stoneHave.text = currentStone.ToString();

        if (stoneNeed != null)
            stoneNeed.text = stoneCost.ToString();
    }

    private void OnEnable()
    {
        PlayData.OnCurrencyChanged += RefreshCostUI;
        Debug.Log("[NormalEnforcePopup] OnEnable: 재화 변경 이벤트 구독");
    }

    private void OnDisable()
    {
        PlayData.OnCurrencyChanged -= RefreshCostUI;
        Debug.Log("[NormalEnforcePopup] OnDisable: 재화 변경 이벤트 구독 해제");
    }
}
