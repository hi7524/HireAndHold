using Cysharp.Threading.Tasks;
using GameData;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class UnitInfoDisplay : MonoBehaviour
{
    [Header("Unit Basic Info")]
    [SerializeField] private Image unitImage;
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private TextMeshProUGUI classText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Hero Enforce")]
    [SerializeField] private TextMeshProUGUI heroStarText;
    [SerializeField] private Transform heroEffectListParent;

    [Header("Hero Effect Prefabs")]
    [SerializeField] private GameObject heroEffectLockedPrefab;
    [SerializeField] private GameObject heroEffectUnlockedPrefab;

    [Header("Skill UI")]
    [SerializeField] private Transform skillListParent;
    [SerializeField] private GameObject skillItemPrefab;
    [SerializeField] private GameObject skillEmptyPrefab;

    [Header("Grid Preview")]
    [SerializeField] private GameObject gridPreviewPanel;
    [SerializeField] private Transform gridPreviewTransform;
    [SerializeField] private float gridCellSize = 20f;

    private const int NORMAL_MAX = 20;
    private const int HERO_MAX = 4;

    private int currentDisplayStar = 1;
    private int currentBaseUnitId = -1;
    private int currentUnitId = -1;

    [SerializeField] private float longPressTime = 0.35f;
    private CancellationTokenSource longPressCTS;


    private EventTrigger unitImageEventTrigger;
    private bool isPressingIcon = false;
    private GridPreviewHelper gridPreviewHelper;
    private UnitGridData currentGridData;
    private AsyncOperationHandle<UnitGridData> gridDataHandle;

    private void Awake()
    {
        SetupUnitIconEvents();

        if (gridPreviewPanel != null)
        {
            var cg = gridPreviewPanel.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = gridPreviewPanel.AddComponent<CanvasGroup>();

            cg.blocksRaycasts = false;   
            cg.interactable = false;

            gridPreviewPanel.SetActive(false);
        }
    }


    private void SetupUnitIconEvents()
    {
        if (unitImage == null)
        {
            Debug.LogWarning("[UnitInfoDisplay] unitImage가 null입니다.");
            return;
        }

        unitImage.raycastTarget = true;

        unitImageEventTrigger = unitImage.gameObject.GetComponent<EventTrigger>();
        if (unitImageEventTrigger == null)
        {
            unitImageEventTrigger = unitImage.gameObject.AddComponent<EventTrigger>();
        }

        unitImageEventTrigger.triggers.Clear();

        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) =>
        {
            OnUnitIconPointerDown((PointerEventData)data);
        });
        unitImageEventTrigger.triggers.Add(pointerDown);

        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) =>
        {
            OnUnitIconPointerUp((PointerEventData)data);
        });
        unitImageEventTrigger.triggers.Add(pointerUp);

        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener(_ =>
        {
            isPressingIcon = false;
            longPressCTS?.Cancel();
            HideGridPreview();
        });
        unitImageEventTrigger.triggers.Add(pointerExit);

        Debug.Log("[UnitInfoDisplay] 유닛 아이콘 이벤트 설정 완료");
    }


    private void OnUnitIconPointerDown(PointerEventData eventData)
    {
        isPressingIcon = true;

        longPressCTS?.Cancel();
        longPressCTS = new CancellationTokenSource();

        WaitLongPress(longPressCTS.Token).Forget();
    }


    private void OnUnitIconPointerUp(PointerEventData eventData)
    {
        isPressingIcon = false;

        longPressCTS?.Cancel();
        HideGridPreview();
    }

    private async UniTaskVoid WaitLongPress(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(longPressTime),
                cancellationToken: token
            );
        }
        catch
        {
            return;
        }

        if (!isPressingIcon)
            return;

        ShowGridPreview();
    }



    private async void ShowGridPreview()
    {
        if (gridPreviewPanel != null)
        {
            gridPreviewPanel.SetActive(true);
        }

        await LoadAndShowGridPreview();
    }

    private void HideGridPreview()
    {
        if (gridPreviewPanel != null)
        {
            gridPreviewPanel.SetActive(false);
        }

        if (gridPreviewHelper != null)
        {
            gridPreviewHelper.Hide();
        }
    }

    private async UniTask LoadAndShowGridPreview()
    {
        if (currentUnitId <= 0)
        {
            Debug.LogWarning("[UnitInfoDisplay] currentUnitId가 유효하지 않습니다.");
            return;
        }

        if (gridPreviewTransform == null)
        {
            Debug.LogError("[UnitInfoDisplay] gridPreviewTransform이 null입니다!");
            return;
        }

        var unitTable = DataTableManager.UnitTable;
        if (unitTable == null)
        {
            Debug.LogError("[UnitInfoDisplay] UnitTable이 아직 초기화되지 않았습니다.");
            return;
        }

        var unitData = unitTable.Get(currentUnitId);

        if (unitData == null)
        {
            Debug.LogError($"[UnitInfoDisplay] UnitData를 찾을 수 없습니다. UnitID: {currentUnitId}");
            return;
        }

        string gridDataKey = unitData.GRID_DATA;
        if (string.IsNullOrEmpty(gridDataKey))
        {
            Debug.LogWarning($"[UnitInfoDisplay] GRID_DATA가 비어있습니다. UnitID: {currentUnitId}");
            return;
        }

        if (gridDataHandle.IsValid())
        {
            Addressables.Release(gridDataHandle);
        }

        gridDataHandle = Addressables.LoadAssetAsync<UnitGridData>(gridDataKey);
        await gridDataHandle.ToUniTask();

        if (gridDataHandle.Status == AsyncOperationStatus.Succeeded)
        {
            currentGridData = gridDataHandle.Result;

            if (gridPreviewHelper == null)
            {
                gridPreviewHelper = new GridPreviewHelper(gridPreviewTransform, gridCellSize);
            }

            gridPreviewHelper.Clear();
            gridPreviewHelper.CreatePreview(currentGridData);
            gridPreviewHelper.Show();

            Debug.Log($"[UnitInfoDisplay] 그리드 프리뷰 표시 완료 - UnitID: {currentUnitId}");
        }
        else
        {
            Debug.LogError($"[UnitInfoDisplay] 그리드 데이터 로드 실패: {gridDataKey}");
        }
    }

    public void UpdateDisplay(
        int unitId,
        UnitData unitData,
        OwnedCharacter character,
        Unit previewUnit)
    {
        if (!DataTableManager.IsInitialized)
        {
            Debug.LogWarning("[UnitInfoDisplay] 테이블이 아직 로딩되지 않았습니다.");
            return;
        }

        if (unitData == null)
        {
            Debug.LogWarning("[UnitInfoDisplay] unitData가 null입니다.");
            return;
        }

        currentDisplayStar = unitData.LEVEL;
        currentUnitId = unitId;

        if (character != null && int.TryParse(character.id, out int baseId))
        {
            currentBaseUnitId = baseId;
        }

        Debug.Log($"[UnitInfoDisplay] UpdateDisplay - unitId: {unitId}, baseUnitId: {currentBaseUnitId}, LEVEL: {unitData.LEVEL}");

        bool owned = character != null;

        if (unitNameText != null)
            unitNameText.text = unitData.StringName;

        if (classText != null)
        {
            string rankName = GetRankName(unitData.RANK);
            classText.text = rankName;
        }

        if (powerText != null)
        {
            int atk = CalculateFinalAttack(unitData, character, currentDisplayStar);
            powerText.text = atk.ToString();
        }

        if (levelText != null)
        {
            levelText.text = owned ? $"{character.enforceLevel}/{NORMAL_MAX}" : $"-/{NORMAL_MAX}";
        }

        if (unitImage != null)
        {
            var sprite = SpriteCache.Instance.GetCachedSpriteOrNull(unitData.UNIT_ICON);
            if (sprite != null)
                unitImage.sprite = sprite;
        }

        int heroLv = owned ? character.heroEnforceLevel : 0;
        if (heroStarText != null)
            heroStarText.text = $"★ {heroLv}/{HERO_MAX}";

        int heroUnitId = currentBaseUnitId > 0 ? currentBaseUnitId : unitId;
        RefreshHeroEffectList(heroUnitId, heroLv);
        RefreshSkillList(unitData);
    }

    public void RefreshHeroEffects()
    {
        if (!DataTableManager.IsInitialized || currentBaseUnitId < 0)
            return;

        PlayData.SyncCharactersFromDatabase();
        var character = DatabaseManager.Instance.GetCharacter(currentBaseUnitId.ToString());
        int heroLv = character != null ? character.heroEnforceLevel : 0;

        if (heroStarText != null)
            heroStarText.text = $"★ {heroLv}/{HERO_MAX}";

        RefreshHeroEffectList(currentBaseUnitId, heroLv);
        Debug.Log($"[UnitInfoDisplay] RefreshHeroEffects 완료 - baseUnitId: {currentBaseUnitId}, heroLv: {heroLv}");
    }

    private string GetRankName(int rank)
    {
        return rank switch
        {
            1 => "노멀",
            2 => "레어",
            3 => "유니크",
            4 => "레전드",
            5 => "에픽",
            _ => $"등급 {rank}"
        };
    }

    private void RefreshSkillList(UnitData unitData)
    {
        if (skillListParent == null)
            return;

        for (int i = skillListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(skillListParent.GetChild(i).gameObject);
        }

        if (currentDisplayStar == 1)
        {
            if (skillEmptyPrefab != null)
                Instantiate(skillEmptyPrefab, skillListParent);
            return;
        }

        var skillTable = DataTableManager.SkillTable;
        if (skillTable == null || skillItemPrefab == null)
            return;

        if (currentDisplayStar == 2 && unitData.UNIT_SKILL1 > 0)
        {
            CreateSkillItem(unitData.UNIT_SKILL1, skillTable);
        }
        else if (currentDisplayStar == 3 && unitData.UNIT_SKILL2 > 0)
        {
            CreateSkillItem(unitData.UNIT_SKILL2, skillTable);
        }
    }

    private void CreateSkillItem(int skillId, DataTable_Skill skillTable)
    {
        if (skillItemPrefab == null || skillListParent == null)
            return;

        SkillData skill = skillTable.Get(skillId);
        if (skill == null)
            return;

        var go = Instantiate(skillItemPrefab, skillListParent);
        if (go == null)
            return;

        var icon = go.transform.Find("Icon")?.GetComponent<Image>();
        var nameText = go.transform.Find("SkillName")?.GetComponent<TextMeshProUGUI>();
        var descText = go.transform.Find("SkillDesc")?.GetComponent<TextMeshProUGUI>();

        if (nameText != null)
        {
            string skillName = int.TryParse(skill.SKILL_NAME, out int nameId)
                ? DataTableManager.GetString(nameId)
                : skill.SKILL_NAME;
            nameText.text = !string.IsNullOrEmpty(skillName) ? skillName : skill.SKILL_NAME;
        }

        if (descText != null)
        {
            string skillDesc = int.TryParse(skill.SKILL_DESCRIPTION, out int descId)
                ? DataTableManager.GetString(descId)
                : skill.SKILL_DESCRIPTION;
            descText.text = !string.IsNullOrEmpty(skillDesc) ? skillDesc : skill.SKILL_DESCRIPTION;
        }

        if (icon != null && !string.IsNullOrEmpty(skill.SKILL_ICON))
        {
            var sp = SpriteCache.Instance.GetCachedSpriteOrNull(skill.SKILL_ICON);
            if (sp != null)
                icon.sprite = sp;
        }
    }

    private void RefreshHeroEffectList(int unitId, int heroLv)
    {
        if (heroEffectListParent == null)
            return;

        for (int i = heroEffectListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(heroEffectListParent.GetChild(i).gameObject);
        }

        var heroTable = DataTableManager.heroEnforceTable;
        var effectTable = DataTableManager.heroEnforceEffectTable;

        if (heroTable == null || effectTable == null)
            return;

        for (int lv = 1; lv <= HERO_MAX; lv++)
        {
            var enforce = heroTable.Get(unitId, lv);
            if (enforce == null)
                continue;

            var effect = effectTable.Get(enforce.Hero_Enforce_EffectID);
            if (effect == null)
                continue;

            string desc = effectTable.FormatEffect(effect);
            bool unlocked = lv <= heroLv;
            GameObject prefab = unlocked ? heroEffectUnlockedPrefab : heroEffectLockedPrefab;

            if (prefab == null)
                continue;

            var go = Instantiate(prefab, heroEffectListParent);
            go.transform.SetSiblingIndex(lv - 1);

            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.text = $"LV {lv}. {desc}";
        }

        Debug.Log($"[UnitInfoDisplay] RefreshHeroEffectList 완료 - unitId: {unitId}, heroLv: {heroLv}");
    }

    private int CalculateFinalAttack(UnitData starUnitData, OwnedCharacter character, int star)
    {
        if (starUnitData == null)
            return 0;

        float baseAttack = starUnitData.ATTACK;
        float normalEnforceAtk = 0f;

        if (character != null)
        {
            int rank = starUnitData.RANK;
            int enforceLv = character.enforceLevel;
            var table = DataTableManager.NormalEnforceTable;

            if (table != null)
            {
                foreach (var kv in table.All)
                {
                    var d = kv.Value;
                    if (d.Class == rank && d.Normal_Enforce_LV <= enforceLv)
                        normalEnforceAtk += d.AttackUp;
                }
            }
        }

        float attackAfterNormal = baseAttack + normalEnforceAtk;
        float heroMultiplier = 1f;

        if (character != null)
        {
            int heroLv = character.heroEnforceLevel;
            int.TryParse(character.id, out int baseId);

            var heroTable = DataTableManager.heroEnforceTable;
            var effectTable = DataTableManager.heroEnforceEffectTable;

            if (heroTable != null && effectTable != null && baseId > 0)
            {
                for (int lv = 1; lv <= heroLv; lv++)
                {
                    var row = heroTable.Get(baseId, lv);
                    if (row == null)
                        continue;

                    var effect = effectTable.Get(row.Hero_Enforce_EffectID);
                    if (effect == null)
                        continue;

                    if (effect.Attack_Up > 1f)
                        heroMultiplier *= effect.Attack_Up;
                }
            }
        }

        float finalAtk = attackAfterNormal * heroMultiplier;
        return Mathf.RoundToInt(finalAtk);
    }

    private void OnDisable()
    {
        isPressingIcon = false;
        HideGridPreview();
    }

    private void OnDestroy()
    {
        if (gridPreviewHelper != null)
        {
            gridPreviewHelper.Clear();
        }

        if (gridDataHandle.IsValid())
        {
            Addressables.Release(gridDataHandle);
        }
    }
}
