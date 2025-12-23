using Cysharp.Threading.Tasks;
using GameData;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
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

    private const int NORMAL_MAX = 20;
    private const int HERO_MAX = 4;

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

        bool owned = character != null;

        if (unitNameText != null)
            unitNameText.text = unitData.StringName;

        if (classText != null)
            classText.text = $"등급 {unitData.RANK}";

        if (powerText != null && previewUnit != null)
            powerText.text = previewUnit.GetAttackDamageStat().Value.ToString();

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

        RefreshHeroEffectList(unitId, heroLv);
        RefreshSkillList(unitData);
    }

    private async UniTaskVoid LoadUnitIconAsync(string key)
    {
        if (unitImage == null || string.IsNullOrEmpty(key))
            return;

        try
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(key).Task;
            if (unitImage != null)
                unitImage.sprite = sprite;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UnitInfoDisplay] 유닛 아이콘 로드 실패: {key}, {ex.Message}");
        }
    }

    private void RefreshSkillList(UnitData unitData)
    {
        if (skillListParent == null || skillItemPrefab == null)
            return;

        for (int i = skillListParent.childCount - 1; i >= 0; i--)
        {
            var child = skillListParent.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }

        var skillTable = DataTableManager.SkillTable;
        if (skillTable == null)
            return;

        if (unitData.UNIT_SKILL1 > 0)
            CreateSkillItem(unitData.UNIT_SKILL1, skillTable);

        if (unitData.UNIT_SKILL2 > 0)
            CreateSkillItem(unitData.UNIT_SKILL2, skillTable);
    }

    private void CreateSkillItem(int skillId, DataTable_Skill skillTable)
    {
        if (skillItemPrefab == null || skillListParent == null)
            return;

        SkillData skill = skillTable.Get(skillId);
        if (skill == null)
        {
            Debug.LogWarning($"[UnitInfoDisplay] SkillData 없음: {skillId}");
            return;
        }

        var go = Instantiate(skillItemPrefab, skillListParent);
        if (go == null)
            return;

        var icon = go.transform.Find("Icon")?.GetComponent<Image>();
        var nameText = go.transform.Find("SkillName")?.GetComponent<TextMeshProUGUI>();
        var descText = go.transform.Find("SkillDesc")?.GetComponent<TextMeshProUGUI>();

        if (nameText != null)
            nameText.text = skill.SKILL_NAME;

        if (descText != null)
            descText.text = skill.SKILL_DESCRIPTION;

        if (icon != null && !string.IsNullOrEmpty(skill.SKILL_ICON))
            LoadSkillIconAsync(icon, skill.SKILL_ICON).Forget();
    }

    private async UniTaskVoid LoadSkillIconAsync(Image icon, string iconKey)
    {
        try
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(iconKey).Task;
            if (icon != null)
                icon.sprite = sprite;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UnitInfoDisplay] 스킬 아이콘 로드 실패: {iconKey}, {ex.Message}");
        }
    }

    private void RefreshHeroEffectList(int unitId, int heroLv)
    {
        if (heroEffectListParent == null)
            return;

        foreach (Transform child in heroEffectListParent)
            Destroy(child.gameObject);

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
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();

            if (txt != null)
                txt.text = $"LV {lv}. {desc}";
        }
    }
}
