using Cysharp.Threading.Tasks;
using GameData;
using TMPro;
using Tutorial;
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

    [Header("Cost Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lackColor = Color.red;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("Success Effect")]
    [SerializeField] private EnforceSuccessEffect successEffect;

    private NormalEnforceSystem enforceSystem;
    private UnitInfoUI mainUI;
    private UIPopupManager popupManager;
    private DataTable_Unit unitTable;

    private int currentUnitId;
    private Unit currentPreviewUnit;
    private bool isProcessing;
    private bool isInitialized;

    private const int NORMAL_MAX = 20;

    private void Start()
    {
        SetupPopup();
    }

    private void SetupPopup()
    {
        if (isInitialized) return;

        if (popupRoot != null && !popupRoot.activeSelf)
            popupRoot.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => popupRoot.SetActive(false));
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        isInitialized = true;
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
        if (previewUnit == null || !previewUnit.IsInitialized)
        {
            Debug.LogError("[NormalEnforcePopup] previewUnit not ready");
            return;
        }

        if (popupRoot != null)
            popupRoot.SetActive(true);

        currentUnitId = unitId;
        currentPreviewUnit = previewUnit;

        RefreshUI();
    }


    private void RefreshUI()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null || currentPreviewUnit == null)
            return;

        var data = unitTable.Get(currentUnitId);
        if (data != null && unitImage != null)
        {
            var sp = SpriteCache.Instance.GetCachedSpriteOrNull(data.UNIT_ICON);
            if (sp != null) unitImage.sprite = sp;
        }

        int lv = character.enforceLevel;

        levelCurrent.text = lv.ToString();
        levelNext.text = lv < NORMAL_MAX ? (lv + 1).ToString() : "MAX";

        float currAtk = currentPreviewUnit.GetAttackDamageStat().Value;
        powerCurrent.text = currAtk.ToString();

        powerNext.text = lv < NORMAL_MAX
            ? enforceSystem.GetNextAttack(currentPreviewUnit).ToString()
            : "-";

        var (goldCost, stoneCost) = enforceSystem.GetNextEnforceCost(currentPreviewUnit);

        long currentGold = PlayData.Gold;
        int currentStone = PlayData.EnhanceStone;

        goldHave.text = currentGold.ToString();
        goldNeed.text = goldCost.ToString();
        stoneHave.text = currentStone.ToString();
        stoneNeed.text = stoneCost.ToString();

        // 재화 부족 체크 및 색상 변경
        UpdateCostColors(currentGold, goldCost, currentStone, stoneCost);
    }

    private void UpdateCostColors(long currentGold, long goldCost, int currentStone, int stoneCost)
    {
        // 골드 색상 체크
        if (goldHave != null)
            goldHave.color = currentGold >= goldCost ? normalColor : lackColor;

        if (goldNeed != null)
            goldNeed.color = currentGold >= goldCost ? normalColor : lackColor;

        // 강화석 색상 체크
        if (stoneHave != null)
            stoneHave.color = currentStone >= stoneCost ? normalColor : lackColor;

        if (stoneNeed != null)
            stoneNeed.color = currentStone >= stoneCost ? normalColor : lackColor;

        // 확인 버튼 활성화 상태
        if (confirmButton != null)
        {
            bool canEnforce = currentGold >= goldCost && currentStone >= stoneCost;
            confirmButton.interactable = canEnforce && !isProcessing;
        }
    }


    private void OnConfirmClicked()
    {
        if (isProcessing) return;
        _ = TryEnforceAsync();
    }

    private async UniTask TryEnforceAsync()
    {
        if (isProcessing) return;
        isProcessing = true;
        confirmButton.interactable = false;

        try
        {
            if (popupManager == null)
            {
                Debug.LogError("[NormalEnforcePopup] popupManager is null!");
                return;
            }

            if (currentPreviewUnit == null || !currentPreviewUnit.IsInitialized)
            {
                Debug.LogWarning("[NormalEnforcePopup] 유닛 데이터 준비 중");
                await popupManager.ShowAlertAsync("유닛 데이터 준비 중입니다.");
                return;
            }

            var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
            if (character == null)
            {
                Debug.LogWarning("[NormalEnforcePopup] 미보유 유닛");
                await popupManager.ShowAlertAsync("미보유 유닛입니다.");
                return;
            }

            int beforeLv = character.enforceLevel;
            int beforeAtk = Mathf.RoundToInt(currentPreviewUnit.GetAttackDamageStat().Value);

            Debug.Log($"[NormalEnforcePopup] 강화 시도: 레벨 {beforeLv}, 공격력 {beforeAtk}");

            bool ok = await enforceSystem.TryEnforceAsync(currentPreviewUnit);

            if (!ok)
            {
                Debug.LogWarning("[NormalEnforcePopup] 재료 부족");
                await popupManager.ShowAlertAsync("재료가 부족합니다.");
                return;
            }

            PlayData.SyncCharactersFromDatabase();
            PlayData.NotifyCharacterUpdated(currentUnitId.ToString());

            character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());

            int afterLv = character.enforceLevel;
            int afterAtk = Mathf.RoundToInt(currentPreviewUnit.GetAttackDamageStat().Value);

            Debug.Log($"[NormalEnforcePopup] 강화 성공: 레벨 {afterLv}, 공격력 {afterAtk}");

            if (successEffect != null)
            {
                successEffect.PlayEffect().Forget();
            }

            await popupManager.ShowSuccessAsync(
                "강화 성공",
                $"레벨 {beforeLv} → {afterLv}\n전투력 {beforeAtk} → {afterAtk}"
            );

            mainUI?.RefreshUI();
            RefreshUI();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NormalEnforcePopup] TryEnforceAsync 예외: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            isProcessing = false;
            if (confirmButton != null)
            {
                // 재화 체크 후 버튼 활성화 결정
                var (goldCost, stoneCost) = enforceSystem.GetNextEnforceCost(currentPreviewUnit);
                bool canEnforce = PlayData.Gold >= goldCost && PlayData.EnhanceStone >= stoneCost;
                confirmButton.interactable = canEnforce;
            }
        }
    }


    private void RefreshCostUI()
    {
        if (popupRoot == null || !popupRoot.activeSelf)
            return;

        if (currentPreviewUnit == null)
            return;

        var (goldCost, stoneCost) = enforceSystem.GetNextEnforceCost(currentPreviewUnit);

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

        // 재화 부족 체크 및 색상 업데이트
        UpdateCostColors(currentGold, goldCost, currentStone, stoneCost);
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
