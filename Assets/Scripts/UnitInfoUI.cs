using System;
using Cysharp.Threading.Tasks;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class UnitInfoUI : MonoBehaviour
{
    #region ===== Main Info UI =====

    [Header("Main Root")]
    [SerializeField] private GameObject mainRoot;

    [Header("Unit Basic Info")]
    [SerializeField] private Image unitImage;
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private TextMeshProUGUI classText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI heroStarText;
    [SerializeField] private TextMeshProUGUI heroProgressText;

    [Header("Owned Resource UI")]
    [SerializeField] private TextMeshProUGUI playerGoldText;
    [SerializeField] private TextMeshProUGUI playerStoneText;
    [SerializeField] private TextMeshProUGUI playerPieceText;

    [Header("Hero Effect List (Main)")]
    [SerializeField] private Transform heroEffectListParent;
    [SerializeField] private GameObject heroEffectItemPrefab;
    [SerializeField] private Color heroEffectUnlockedColor = Color.white;
    [SerializeField] private Color heroEffectLockedColor = Color.gray;

    [Header("Main Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Button normalEnforceButton;
    [SerializeField] private Button heroEnforceButton;

    [Header("Replace Popup")]
    [SerializeField] private GameObject replacePopupRoot;
    [SerializeField] private Transform replaceSlotParent;
    [SerializeField] private DeckSlot replaceSlotPrefab;
    [SerializeField] private Button replaceCancelButton;
    [SerializeField] private Image replaceSelectedUnitImage;


    #endregion


    #region ===== Normal Enforce Popup =====

    [Header("Normal Enforce Popup")]
    [SerializeField] private GameObject normalPopupRoot;
    [SerializeField] private Image normalPopupUnitImage;

    [SerializeField] private TextMeshProUGUI normalLevelCurrent;
    [SerializeField] private TextMeshProUGUI normalLevelNext;
    [SerializeField] private TextMeshProUGUI normalPowerCurrent;
    [SerializeField] private TextMeshProUGUI normalPowerNext;

    [SerializeField] private TextMeshProUGUI normalStoneHave;
    [SerializeField] private TextMeshProUGUI normalStoneNeed;
    [SerializeField] private TextMeshProUGUI normalGoldHave;
    [SerializeField] private TextMeshProUGUI normalGoldNeed;

    [SerializeField] private Button normalConfirmButton;
    [SerializeField] private Button normalCloseButton;

    #endregion


    #region ===== Hero Enforce Popup =====

    [Header("Hero Enforce Popup")]
    [SerializeField] private GameObject heroPopupRoot;
    [SerializeField] private Image heroPopupUnitImage;

    [SerializeField] private TextMeshProUGUI heroPopupLevelCurrent;
    [SerializeField] private TextMeshProUGUI heroPopupLevelNext;

    [SerializeField] private TextMeshProUGUI heroPopupPieceHave;
    [SerializeField] private TextMeshProUGUI heroPopupPieceNeed;
    [SerializeField] private TextMeshProUGUI heroPopupGoldHave;
    [SerializeField] private TextMeshProUGUI heroPopupGoldNeed;

    [SerializeField] private Transform heroPopupEffectListParent;
    [SerializeField] private GameObject heroPopupEffectItemPrefab;

    [SerializeField] private Button heroConfirmButton;
    [SerializeField] private Button heroCloseButton;

    #endregion


    #region ===== Alert / Success Popup =====

    [Header("Alert Popup")]
    [SerializeField] private GameObject alertRoot;
    [SerializeField] private TextMeshProUGUI alertMessage;
    [SerializeField] private Button alertOk;

    [Header("Success Popup")]
    [SerializeField] private GameObject successRoot;
    [SerializeField] private TextMeshProUGUI successTitle;
    [SerializeField] private TextMeshProUGUI successDetail;
    [SerializeField] private Button successOk;

    #endregion


    #region ===== Data =====

    private DataTable_Unit unitTable;
    private DataTable_NormalEnforce normalTable;
    private DataTable_HeroEnforce heroTable;
    private DataTable_HeroEnforceEffect effectTable;

    private NormalEnforceSystem normalSystem;
    private HeroEnforceSystem heroSystem;

    private BattleUnitManager battleUnitManager;
    private int currentUnitId = -1;
    private Unit previewUnit;

    private DeckControl deckControl;

    private const int NORMAL_MAX = 20;
    private const int HERO_MAX = 4;

    #endregion



    #region ===== Unity Life Cycle =====

    private async void Start()
    {
        unitTable = new DataTable_Unit();
        normalTable = new DataTable_NormalEnforce();
        heroTable = new DataTable_HeroEnforce();
        effectTable = new DataTable_HeroEnforceEffect();

        await unitTable.LoadAsync("UnitTable");
        await normalTable.LoadAsync("NormalEnforceTable");
        await heroTable.LoadAsync("HeroEnforceTable");
        await effectTable.LoadAsync("HeroEnforceEffectTable");

        mainRoot.SetActive(false);
        normalPopupRoot.SetActive(false);
        heroPopupRoot.SetActive(false);
        alertRoot.SetActive(false);
        successRoot.SetActive(false);
        replacePopupRoot.SetActive(false);

        normalCloseButton.onClick.AddListener(() => normalPopupRoot.SetActive(false));
        heroCloseButton.onClick.AddListener(() => heroPopupRoot.SetActive(false));
        alertOk.onClick.AddListener(() => alertRoot.SetActive(false));
        successOk.onClick.AddListener(() => successRoot.SetActive(false));

        normalConfirmButton.onClick.AddListener(() => ConfirmNormal().Forget());
        heroConfirmButton.onClick.AddListener(() => ConfirmHero().Forget());
    }

    public void SetUnitManager(BattleUnitManager manager)
    {
        battleUnitManager = manager;

        normalSystem = new NormalEnforceSystem(manager, normalTable, unitTable);
        heroSystem = new HeroEnforceSystem(manager, heroTable, effectTable);

        normalEnforceButton.onClick.AddListener(OpenNormal);
        heroEnforceButton.onClick.AddListener(OpenHero);

        equipButton.onClick.AddListener(OnEquipButtonClicked);
    }

    public void SetDeckControl(DeckControl control)
    {
        deckControl = control;
    }

    #endregion



    #region ===== Public API =====

    public void SetUnit(int id)
    {
        currentUnitId = id;
        Refresh().Forget();
    }

    #endregion



    #region ===== Main UI Refresh =====

    private async UniTask Refresh()
    {
        if (currentUnitId < 0)
            return;

        await CreatePreview();

        await UniTask.DelayFrame(1);

        var data = unitTable.Get(currentUnitId);
        if (data == null)
        {
            Debug.LogError("UnitData 없음: " + currentUnitId);
            return;
        }

        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        bool owned = character != null;

        unitNameText.text = data.StringName;
        classText.text = $"등급: {data.RANK}";

        float attack = previewUnit.GetAttackDamageStat().Value;
        powerText.text = attack.ToString();

        levelText.text = owned ?
            $"{character.enforceLevel}/{NORMAL_MAX}" :
            $"0/{NORMAL_MAX}";

        LoadSprite(data.UNIT_ICON).Forget();

        playerGoldText.text = PlayData.Gold.ToString();
        playerStoneText.text = PlayData.EnhanceStone.ToString();
        int ownedPieces = PlayData.unitFragments.ContainsKey(currentUnitId)
     ? (int)PlayData.unitFragments[currentUnitId]
     : 0;

        int heroLv = owned ? character.heroEnforceLevel : 0;
        heroStarText.text = $"영웅강화 등급: ★{heroLv}";
        heroProgressText.text = $"{heroLv}/{HERO_MAX}";

        RefreshHeroEffectList(heroLv);

        normalEnforceButton.interactable = owned && normalSystem.CanEnforce(previewUnit, out _);
        heroEnforceButton.interactable = owned && heroLv < HERO_MAX;

        mainRoot.SetActive(true);
    }

    private async UniTaskVoid LoadSprite(string key)
    {
        try
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(key).Task;
            unitImage.sprite = sprite;
            normalPopupUnitImage.sprite = sprite;
            heroPopupUnitImage.sprite = sprite;

            if (replaceSelectedUnitImage != null)
                replaceSelectedUnitImage.sprite = sprite;
        }
        catch { }
    }

    private async UniTask CreatePreview()
    {
        if (previewUnit != null)
            Destroy(previewUnit.gameObject);

        var go = new GameObject("PreviewUnit_" + currentUnitId);
        previewUnit = go.AddComponent<Unit>();
        previewUnit.SetUnitID(currentUnitId);

        await UniTask.DelayFrame(1);
    }

    private void RefreshHeroEffectList(int heroLv)
    {
        foreach (Transform t in heroEffectListParent)
            Destroy(t.gameObject);

        for (int lv = 1; lv <= HERO_MAX; lv++)
        {
            var enforce = heroTable.Get(currentUnitId, lv);
            if (enforce == null) continue;

            var eff = effectTable.Get(enforce.Hero_Enforce_EffectID);
            string desc = effectTable.FormatEffect(eff);

            var go = Instantiate(heroEffectItemPrefab, heroEffectListParent);
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();


            if (txt == null)
            {
                Debug.LogError("heroEffectItemPrefab 안에 TMP Text 없음!");
                continue;
            }

            txt.text = $"LV {lv}: {desc}";
            txt.color = lv <= heroLv ? heroEffectUnlockedColor : heroEffectLockedColor;
        }
    }

    #endregion



    #region ===== Normal Enforce Popup =====

    private void OpenNormal()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
        {
            ShowAlert("미보유 유닛은 강화할 수 없습니다.");
            return;
        }

        int lv = character.enforceLevel;
        int nextLv = Mathf.Min(lv + 1, NORMAL_MAX);

        float currAtk = previewUnit.GetAttackDamageStat().Value;
        float nextAtk = normalSystem.GetNextAttack(previewUnit);

        var (goldCost, stoneCost) = normalSystem.GetNextEnforceCost(previewUnit);

        normalLevelCurrent.text = lv.ToString();
        normalLevelNext.text = nextLv > NORMAL_MAX ? "MAX" : nextLv.ToString();

        normalPowerCurrent.text = currAtk.ToString();
        normalPowerNext.text = nextLv > NORMAL_MAX ? "-" : nextAtk.ToString();

        normalStoneHave.text = PlayData.EnhanceStone.ToString();
        normalStoneNeed.text = stoneCost.ToString();

        normalGoldHave.text = PlayData.Gold.ToString();
        normalGoldNeed.text = goldCost.ToString();

        normalPopupRoot.SetActive(true);
    }

    private async UniTaskVoid ConfirmNormal()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
        {
            ShowAlert("미보유 유닛입니다.");
            return;
        }

        int beforeLv = character.enforceLevel;
        float beforeAtk = previewUnit.GetAttackDamageStat().Value;

        bool ok = await normalSystem.TryEnforceAsync(previewUnit);

        if (!ok)
        {
            ShowAlert("재료가 부족합니다.");
            return;
        }

        await DatabaseManager.Instance.LoadUserDataAsync();
        PlayData.SyncFromDatabase();

        int afterLv = character.enforceLevel;
        float afterAtk = previewUnit.GetAttackDamageStat().Value;

        ShowSuccess("강화 성공",$"레벨 {beforeLv} → {afterLv}\n전투력 {beforeAtk} → {afterAtk}");

        await Refresh();
        RefreshNormalPopup();

    }

    private void RefreshNormalPopup()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null) return;

        int lv = character.enforceLevel;
        int nextLv = Mathf.Min(lv + 1, NORMAL_MAX);

        float currAtk = previewUnit.GetAttackDamageStat().Value;
        float nextAtk = normalSystem.GetNextAttack(previewUnit);

        var (goldCost, stoneCost) = normalSystem.GetNextEnforceCost(previewUnit);

        normalLevelCurrent.text = lv.ToString();
        normalLevelNext.text = nextLv > NORMAL_MAX ? "MAX" : nextLv.ToString();

        normalPowerCurrent.text = currAtk.ToString();
        normalPowerNext.text = nextLv > NORMAL_MAX ? "-" : nextAtk.ToString();

        normalStoneHave.text = PlayData.EnhanceStone.ToString();
        normalStoneNeed.text = stoneCost.ToString();

        normalGoldHave.text = PlayData.Gold.ToString();
        normalGoldNeed.text = goldCost.ToString();
    }


    #endregion



    #region ===== Hero Enforce Popup =====

    private void OpenHero()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
        {
            ShowAlert("미보유 유닛은 영웅강화가 불가합니다.");
            return;
        }

        int heroLv = character.heroEnforceLevel;
        if (heroLv >= HERO_MAX)
        {
            ShowAlert("최대 영웅강화입니다.");
            return;
        }
        var nextData = heroTable.Get(currentUnitId, heroLv + 1);

        var next = heroTable.Get(currentUnitId, heroLv + 1);

        heroPopupLevelCurrent.text = $"★{heroLv}";
        heroPopupLevelNext.text = heroLv + 1 <= HERO_MAX ? $"★{heroLv + 1}" : "MAX";

        heroPopupPieceNeed.text = next?.IngredientNum.ToString();
        heroPopupGoldNeed.text = next?.Gold_Cost.ToString();

        int havePieces = PlayData.GetUnitFragments(currentUnitId);
        int needPieces = (int)nextData.IngredientNum;

        if (havePieces < needPieces)
        {
            ShowAlert("조각이 부족합니다");
            return;
        }


        heroPopupPieceHave.text = havePieces.ToString();
        heroPopupPieceNeed.text = needPieces.ToString();


        foreach (Transform t in heroPopupEffectListParent)
            Destroy(t.gameObject);

        for (int lv = 1; lv <= HERO_MAX; lv++)
        {
            var e = heroTable.Get(currentUnitId, lv);
            if (e == null) continue;

            var eff = effectTable.Get(e.Hero_Enforce_EffectID);
            if (eff == null) continue;

            var go = Instantiate(heroPopupEffectItemPrefab, heroPopupEffectListParent);
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = $"{(lv <= heroLv ? "[활성]" : "[잠김]")} LV {lv}: {eff.DescriptionText}";

        }

        heroPopupRoot.SetActive(true);
    }

    private async UniTaskVoid ConfirmHero()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
        {
            ShowAlert("미보유 유닛입니다.");
            return;
        }

        int beforeLv = character.heroEnforceLevel;

        bool ok = await heroSystem.TryEnforceAsync(previewUnit);
        if (!ok)
        {
            ShowAlert("재료 부족!");
            return;
        }

        await DatabaseManager.Instance.LoadUserDataAsync();
        PlayData.SyncFromDatabase();

        int afterLv = character.heroEnforceLevel;
        var ef = heroTable.Get(currentUnitId, afterLv);
        var effData = effectTable.Get(ef.Hero_Enforce_EffectID);
        var desc = effectTable.FormatEffect(effData);

        ShowSuccess("영웅 강화 성공!", $"★{beforeLv} → ★{afterLv}\n효과: {desc}");


        await Refresh();
        RefreshHeroPopup();

    }

    private void RefreshHeroPopup()
    {
        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null) return;

        int heroLv = character.heroEnforceLevel;
        var next = heroTable.Get(currentUnitId, heroLv + 1);

        int havePieces = PlayData.GetUnitFragments(currentUnitId);
        int needPieces = (int)(next?.IngredientNum ?? 0);

        heroPopupLevelCurrent.text = $"★{heroLv}";
        heroPopupLevelNext.text = heroLv < HERO_MAX ? $"★{heroLv + 1}" : "MAX";

        heroPopupPieceHave.text = havePieces.ToString();
        heroPopupPieceNeed.text = needPieces.ToString();

        heroPopupGoldHave.text = PlayData.Gold.ToString();
        heroPopupGoldNeed.text = (next?.Gold_Cost ?? 0).ToString();

        foreach (Transform t in heroPopupEffectListParent)
            Destroy(t.gameObject);

        for (int lv = 1; lv <= HERO_MAX; lv++)
        {
            var e = heroTable.Get(currentUnitId, lv);
            if (e == null) continue;

            var eff = effectTable.Get(e.Hero_Enforce_EffectID);
            if (eff == null) continue;

            var go = Instantiate(heroPopupEffectItemPrefab, heroPopupEffectListParent);
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            string desc = effectTable.FormatEffect(eff);
            txt.text = $"{(lv <= heroLv ? "[활성]" : "[잠김]")} LV {lv}: {desc}";

        }
    }

        #endregion

    #region ===== Equip (장착) =====

    private void OnEquipButtonClicked()
    {
        if (deckControl == null)
        {
            ShowAlert("덱 컨트롤러를 찾을 수 없습니다.");
            return;
        }

        var character = DatabaseManager.Instance.GetCharacter(currentUnitId.ToString());
        if (character == null)
        {
            ShowAlert("미보유 유닛은 장착할 수 없습니다.");
            return;
        }

        int activePreset = PlayData.currentSelectedPreset;
        bool alreadyEquipped = false;
        int equippedSlotIndex = -1;

        for (int i = 0; i < 5; i++)
        {
            if (PlayData.selectedDeckUnitIds[activePreset, i] == currentUnitId)
            {
                alreadyEquipped = true;
                equippedSlotIndex = i;
                break;
            }
        }

        if (alreadyEquipped)
        {
            ShowAlert($"이미 슬롯 {equippedSlotIndex + 1}번에 편성되어 있습니다.");
            return;
        }

        int emptySlotIndex = -1;
        for (int i = 0; i < 5; i++)
        {
            if (PlayData.selectedDeckUnitIds[activePreset, i] == 0)
            {
                emptySlotIndex = i;
                break;
            }
        }

        if (emptySlotIndex != -1)
        {
            // 빈 슬롯에 추가
            EquipToSlot(emptySlotIndex);
        }
        else
        {
            // 모든 슬롯이 차있으면 교체 팝업 표시
            ShowReplacePopup();
        }
    }

    private async void EquipToSlot(int slotIndex)
    {
        int activePreset = PlayData.currentSelectedPreset;

        var data = unitTable.Get(currentUnitId);
        if (data == null) return;

        PlayData.selectedDeckUnitIds[activePreset, slotIndex] = currentUnitId;
        PlayData.selectedDeckUnitIconAddresses[activePreset, slotIndex] = data.UNIT_ICON;

        await DatabaseManager.Instance.SavePresetFromPlayDataAsync(activePreset);

        deckControl.ApplyPresetToSelectedUnitIds();
        deckControl.LoadPresets();
        deckControl.OnClickPresetButton(activePreset);

        ShowSuccess("장착 완료", $"{data.StringName}이(가) 슬롯 {slotIndex + 1}번에 장착되었습니다.");

        mainRoot.SetActive(false);
    }

    private void ShowReplacePopup()
    {
        replacePopupRoot.SetActive(true);
        RefreshReplaceSlots();
    }

    #endregion

    #region ===== Replace Popup Implementation =====

    private void RefreshReplaceSlots()
    {
        foreach (Transform child in replaceSlotParent)
            Destroy(child.gameObject);

        int preset = PlayData.currentSelectedPreset;

        for (int i = 0; i < 5; i++)
        {
            int slotIndex = i;
            int unitId = PlayData.selectedDeckUnitIds[preset, i];
            if (unitId == 0) continue;

            var model = deckControl.GetDeckUnitModelFromPreset(preset, slotIndex);

            if (model == null) continue;

            var slot = Instantiate(replaceSlotPrefab, replaceSlotParent);

            slot.SetCommittedExternal(model);
            slot.SetInteractable(true);

            // 교체 전용 클릭
            slot.onSlotClickedExternal = _ =>
            {
                ReplaceSlot(slotIndex);
            };
        }
    }



    private async void ReplaceSlot(int slotIndex)
    {
        int activePreset = PlayData.currentSelectedPreset;

        // 기존 유닛 정보 가져오기
        int oldUnitId = PlayData.selectedDeckUnitIds[activePreset, slotIndex];
        var oldData = unitTable.Get(oldUnitId);
        var newData = unitTable.Get(currentUnitId);

        // 교체 수행
        PlayData.selectedDeckUnitIds[activePreset, slotIndex] = currentUnitId;
        PlayData.selectedDeckUnitIconAddresses[activePreset, slotIndex] = newData.UNIT_ICON;

        await DatabaseManager.Instance.SavePresetFromPlayDataAsync(activePreset);

        deckControl.ApplyPresetToSelectedUnitIds();
        deckControl.LoadPresets();
        deckControl.OnClickPresetButton(activePreset);

        replacePopupRoot.SetActive(false);
        mainRoot.SetActive(false);

        ShowSuccess("교체 완료",$"슬롯 {slotIndex + 1}번\n{oldData?.StringName ?? "빈 슬롯"} → {newData.StringName}");
    }

    #endregion



    #region ===== Alert / Success =====

    private void ShowAlert(string msg)
    {
        alertMessage.text = msg;
        alertRoot.SetActive(true);
    }

    private void ShowSuccess(string title, string detail)
    {
        successTitle.text = title;
        successDetail.text = detail;
        successRoot.SetActive(true);
    }

    #endregion
}
