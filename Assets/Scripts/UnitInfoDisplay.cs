using Cysharp.Threading.Tasks;
using GameData;
using TMPro;
using UnityEngine;
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

    private const int NORMAL_MAX = 20;
    private const int HERO_MAX = 4;

    private int currentDisplayStar = 1;
    private int currentBaseUnitId = -1;

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

        if (powerText != null && previewUnit != null)
        {
            int power = Mathf.RoundToInt(previewUnit.GetAttackDamageStat().Value);
            powerText.text = power.ToString();
        }

        if (levelText != null)
        {
            levelText.text = owned
                ? $"{character.enforceLevel}/{NORMAL_MAX}"
                : $"-/{NORMAL_MAX}";
        }

        LoadUnitIconAsync(unitData.UNIT_ICON).Forget();

        int heroLv = owned ? character.heroEnforceLevel : 0;
        if (heroStarText != null)
            heroStarText.text = $"★ {heroLv}/{HERO_MAX}";

        int heroUnitId = currentBaseUnitId > 0 ? currentBaseUnitId : unitId;
        RefreshHeroEffectList(heroUnitId, heroLv);

        RefreshSkillList(unitData);
    }


    public void RefreshHeroEffects()
    {
        if (!DataTableManager.IsInitialized)
            return;

        if (currentBaseUnitId < 0)
            return;

        PlayData.SyncCharactersFromDatabase();
        var character = DatabaseManager.Instance.GetCharacter(currentBaseUnitId.ToString());
        int heroLv = character != null ? character.heroEnforceLevel : 0;

        // Hero Star 텍스트 갱신
        if (heroStarText != null)
            heroStarText.text = $"★ {heroLv}/{HERO_MAX}";


        RefreshHeroEffectList(currentBaseUnitId, heroLv);

        Debug.Log($"[UnitInfoDisplay] RefreshHeroEffects 완료 - baseUnitId: {currentBaseUnitId}, heroLv: {heroLv}");
    }

    private string GetRankName(int rank)
    {
        switch (rank)
        {
            case 1:
                return "노멀";
            case 2:
                return "레어";
            case 3:
                return "유니크";
            case 4:
                return "레전드";
            case 5:
                return "에픽";
            default:
                return $"등급 {rank}";
        }
    }

    private async UniTaskVoid LoadUnitIconAsync(string key)
    {
        if (unitImage == null || string.IsNullOrEmpty(key))
            return;

        var sprite = await SpriteCache.Instance.LoadSpriteAsync(key);
        if (unitImage != null && sprite != null)
            unitImage.sprite = sprite;
    }

    private void RefreshSkillList(UnitData unitData)
    {
        if (skillListParent == null)
            return;

        while (skillListParent.childCount > 0)
        {
            DestroyImmediate(skillListParent.GetChild(0).gameObject);
        }

        if (currentDisplayStar == 1)
        {
            if (skillEmptyPrefab != null)
            {
                Instantiate(skillEmptyPrefab, skillListParent);
            }
            return;
        }

        var skillTable = DataTableManager.SkillTable;
        if (skillTable == null || skillItemPrefab == null)
            return;

        if (currentDisplayStar == 2)
        {
            if (unitData.UNIT_SKILL1 > 0)
            {
                CreateSkillItem(unitData.UNIT_SKILL1, skillTable);
            }
        }
        else if (currentDisplayStar == 3)
        {
            if (unitData.UNIT_SKILL2 > 0)
            {
                CreateSkillItem(unitData.UNIT_SKILL2, skillTable);
            }
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
            string skillName = null;
            if (int.TryParse(skill.SKILL_NAME, out int nameId))
            {
                skillName = DataTableManager.GetString(nameId);
            }
            nameText.text = !string.IsNullOrEmpty(skillName) ? skillName : skill.SKILL_NAME;
        }

        if (descText != null)
        {
            string skillDesc = null;
            if (int.TryParse(skill.SKILL_DESCRIPTION, out int descId))
            {
                skillDesc = DataTableManager.GetString(descId);
            }
            descText.text = !string.IsNullOrEmpty(skillDesc) ? skillDesc : skill.SKILL_DESCRIPTION;
        }

        if (icon != null && !string.IsNullOrEmpty(skill.SKILL_ICON))
        {
            LoadSkillIconAsync(icon, skill.SKILL_ICON).Forget();
        }
    }

    private async UniTaskVoid LoadSkillIconAsync(Image icon, string iconKey)
    {
        var sprite = await SpriteCache.Instance.LoadSpriteAsync(iconKey);
        if (icon != null && sprite != null)
            icon.sprite = sprite;
    }

    private void RefreshHeroEffectList(int unitId, int heroLv)
    {
        if (heroEffectListParent == null)
            return;

        while (heroEffectListParent.childCount > 0)
        {
            DestroyImmediate(heroEffectListParent.GetChild(0).gameObject);
        }

        var heroTable = DataTableManager.heroEnforceTable;
        var effectTable = DataTableManager.heroEnforceEffectTable;

        if (heroTable == null || effectTable == null)
            return;

        for (int lv = 1; lv <= HERO_MAX; lv++)
        {
            var enforce = heroTable.Get(unitId, lv);
            if (enforce == null) continue;

            var effect = effectTable.Get(enforce.Hero_Enforce_EffectID);
            if (effect == null) continue;

            string desc = effectTable.FormatEffect(effect);

            bool unlocked = lv <= heroLv;
            GameObject prefab = unlocked
                ? heroEffectUnlockedPrefab
                : heroEffectLockedPrefab;

            if (prefab == null) continue;

            var go = Instantiate(prefab, heroEffectListParent);
            go.transform.SetSiblingIndex(lv - 1);

            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.text = $"LV {lv}. {desc}";
        }

        Debug.Log($"[UnitInfoDisplay] RefreshHeroEffectList 완료 - unitId: {unitId}, heroLv: {heroLv}");
    }
}
