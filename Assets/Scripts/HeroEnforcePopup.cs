using Cysharp.Threading.Tasks;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroEnforcePopup : MonoBehaviour
{
    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Unit Display")]
    [SerializeField] private Image unitImage;

    [Header("Level Info")]
    [SerializeField] private TextMeshProUGUI levelCurrent;
    [SerializeField] private TextMeshProUGUI levelNext;

    [Header("Cost Info")]
    [SerializeField] private TextMeshProUGUI pieceHave;
    [SerializeField] private TextMeshProUGUI pieceNeed;
    [SerializeField] private TextMeshProUGUI goldHave;
    [SerializeField] private TextMeshProUGUI goldNeed;

    [Header("Cost Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lackColor = Color.red;

    [Header("Effect List")]
    [SerializeField] private Transform effectListParent;
    [SerializeField] private GameObject effectItemPrefab;

    [Header("Effect Colors")]
    [SerializeField] private Color activeEffectColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color inactiveEffectColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("Success Effect")]
    [SerializeField] private EnforceSuccessEffect successEffect;

    private HeroEnforceSystem enforceSystem;
    private DataTable_Unit unitTable;
    private DataTable_HeroEnforce heroTable;
    private DataTable_HeroEnforceEffect effectTable;
    private UnitInfoUI mainUI;
    private UIPopupManager popupManager;

    private int currentUnitId;
    private Unit currentPreviewUnit;
    private bool isProcessing;

    private const int HERO_MAX = 4;

    private void Start()
    {
        SetupPopup();

        if (activeEffectColor == default)
            activeEffectColor = new Color(1f, 1f, 1f, 1f);

        if (inactiveEffectColor == default)
            inactiveEffectColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
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
        HeroEnforceSystem system,
        DataTable_Unit uTable,
        DataTable_HeroEnforce hTable,
        DataTable_HeroEnforceEffect eTable,
        UnitInfoUI ui)
    {
        enforceSystem = system;
        unitTable = uTable;
        heroTable = hTable;
        effectTable = eTable;
        mainUI = ui;
    }

    public void Open(int unitId, Unit previewUnit)
    {
        if (closeButton != null && closeButton.onClick.GetPersistentEventCount() == 0)
            SetupPopup();

        if (popupRoot != null)
            popupRoot.SetActive(true);

        currentUnitId = unitId;
        currentPreviewUnit = previewUnit;

        if (currentPreviewUnit == null)
        {
            Debug.LogWarning("[HeroEnforcePopup] previewUnit is null");
            return;
        }

        var character = DatabaseManager.Instance.GetCharacter(unitId.ToString());
        if (character == null)
        {
            popupManager?.ShowAlert("미보유 유닛은 영웅강화가 불가합니다.");
            return;
        }

        int heroLv = character.heroEnforceLevel;
        if (heroLv >= HERO_MAX)
        {
            popupManager?.ShowAlert("최대 영웅강화입니다.");
            return;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        var data = unitTable.Get(currentUnitId);
        if (data != null && unitImage != null)
        {
            var sp = SpriteCache.Instance.GetCachedSpriteOrNull(data.UNIT_ICON);
            if (sp != null) unitImage.sprite = sp;
        }

        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
            return;

        int heroLv = character.heroEnforceLevel;
        int nextLv = heroLv + 1;
        var next = nextLv <= HERO_MAX ? heroTable.Get(currentUnitId, nextLv) : null;

        if (levelCurrent != null)
            levelCurrent.text = $"★{heroLv}";

        if (levelNext != null)
            levelNext.text = heroLv < HERO_MAX ? $"★{nextLv}" : "MAX";

        var unitData = unitTable.Get(currentUnitId);
        int fragmentItemId = unitData.FRAGMENT_ITEM_ID;

        int havePieces = PlayData.GetItemCount(fragmentItemId);
        int needPieces = (int)(next?.IngredientNum ?? 0);
        long currentGold = PlayData.Gold;
        long needGold = next?.Gold_Cost ?? 0;

        if (pieceHave != null)
            pieceHave.text = havePieces.ToString();

        if (pieceNeed != null)
            pieceNeed.text = needPieces.ToString();

        if (goldHave != null)
            goldHave.text = currentGold.ToString();

        if (goldNeed != null)
            goldNeed.text = needGold.ToString();

        // 재화 부족 체크 및 색상 변경
        UpdateCostColors(currentGold, needGold, havePieces, needPieces);

        RefreshEffectList(heroLv);
    }

    private void UpdateCostColors(long currentGold, long needGold, int havePieces, int needPieces)
    {
        // 골드 색상 체크
        if (goldHave != null)
            goldHave.color = currentGold >= needGold ? normalColor : lackColor;

        if (goldNeed != null)
            goldNeed.color = currentGold >= needGold ? normalColor : lackColor;

        // 조각 색상 체크
        if (pieceHave != null)
            pieceHave.color = havePieces >= needPieces ? normalColor : lackColor;

        if (pieceNeed != null)
            pieceNeed.color = havePieces >= needPieces ? normalColor : lackColor;

        // 확인 버튼 활성화 상태
        if (confirmButton != null)
        {
            bool canEnforce = currentGold >= needGold && havePieces >= needPieces;
            confirmButton.interactable = canEnforce && !isProcessing;
        }
    }

    private void RefreshEffectList(int heroLv)
    {
        if (effectListParent == null)
            return;

        foreach (Transform t in effectListParent)
            Destroy(t.gameObject);

        if (heroTable == null || effectTable == null || effectItemPrefab == null)
            return;

        for (int lv = 1; lv <= HERO_MAX; lv++)
        {
            var e = heroTable.Get(currentUnitId, lv);
            if (e == null) continue;

            var eff = effectTable.Get(e.Hero_Enforce_EffectID);
            if (eff == null) continue;

            var go = Instantiate(effectItemPrefab, effectListParent);

            bool isActive = lv <= heroLv;

            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                string desc = effectTable.FormatEffect(eff);
                txt.text = $"LV {lv}: {desc}";
                txt.color = isActive ? activeEffectColor : inactiveEffectColor;
            }

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                Color imgColor = img.color;
                imgColor.a = isActive ? 1f : 0.5f;
                img.color = imgColor;
            }

            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = go.AddComponent<CanvasGroup>();

            canvasGroup.alpha = isActive ? 1f : 0.6f;
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
            var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
            if (character == null)
            {
                popupManager?.ShowAlert("미보유 유닛입니다.");
                return;
            }

            if (currentPreviewUnit == null || !currentPreviewUnit.IsInitialized)
            {
                popupManager?.ShowAlert("유닛 데이터 준비 중입니다.");
                return;
            }

            int beforeLv = character.heroEnforceLevel;

            bool ok = await enforceSystem.TryEnforceAsync(currentPreviewUnit);
            if (!ok)
            {
                popupManager?.ShowAlert("재료 부족!");
                return;
            }

            PlayData.SyncCharactersFromDatabase();
            PlayData.NotifyCharacterUpdated(currentUnitId.ToString());

            mainUI?.RefreshUI();
            RefreshUI();

            var updatedChar = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
            int afterLv = updatedChar.heroEnforceLevel;

            var ef = heroTable.Get(currentUnitId, afterLv);
            var effData = effectTable.Get(ef.Hero_Enforce_EffectID);
            var desc = effectTable.FormatEffect(effData);

            successEffect?.PlayEffect().Forget();

            popupManager?.ShowSuccess(
                "영웅 강화 성공!",
                $"★{beforeLv} → ★{afterLv}\n효과: {desc}"
            );
        }
        finally
        {
            isProcessing = false;

            // 재화 체크 후 버튼 활성화 결정
            var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
            if (character != null && confirmButton != null)
            {
                int heroLv = character.heroEnforceLevel;
                var next = heroTable.Get(currentUnitId, heroLv + 1);

                var unitData = unitTable.Get(currentUnitId);
                int fragmentItemId = unitData.FRAGMENT_ITEM_ID;
                int havePieces = PlayData.GetItemCount(fragmentItemId);
                int needPieces = (int)(next?.IngredientNum ?? 0);
                long needGold = next?.Gold_Cost ?? 0;

                bool canEnforce = PlayData.Gold >= needGold && havePieces >= needPieces;
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

        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
            return;

        int heroLv = character.heroEnforceLevel;
        var next = heroTable.Get(currentUnitId, heroLv + 1);

        long currentGold = PlayData.Gold;
        long needGold = next?.Gold_Cost ?? 0;

        if (goldHave != null)
            goldHave.text = currentGold.ToString();

        if (goldNeed != null)
            goldNeed.text = needGold.ToString();

        var unitData = unitTable.Get(currentUnitId);
        int fragmentItemId = unitData.FRAGMENT_ITEM_ID;

        int havePieces = PlayData.GetItemCount(fragmentItemId);
        int needPieces = (int)(next?.IngredientNum ?? 0);

        if (pieceHave != null)
            pieceHave.text = havePieces.ToString();

        if (pieceNeed != null)
            pieceNeed.text = needPieces.ToString();

        // 재화 부족 체크 및 색상 업데이트
        UpdateCostColors(currentGold, needGold, havePieces, needPieces);
    }

    private void OnEnable()
    {
        PlayData.OnCurrencyChanged += RefreshCostUI;
    }

    private void OnDisable()
    {
        PlayData.OnCurrencyChanged -= RefreshCostUI;
    }
}
