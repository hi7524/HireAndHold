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
    [SerializeField] private GameObject skillEmptyPrefab;

    private const int NORMAL_MAX = 20;
    private const int HERO_MAX = 4;

    private int currentDisplayStar = 1;

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

        Debug.Log($"[UnitInfoDisplay] UpdateDisplay - unitId: {unitId}, LEVEL: {unitData.LEVEL}, currentDisplayStar: {currentDisplayStar}");

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
        if (skillListParent == null)
        {
            Debug.LogError("[UnitInfoDisplay] skillListParent가 null입니다!");
            return;
        }

        Debug.Log($"[UnitInfoDisplay] RefreshSkillList 시작 - currentDisplayStar: {currentDisplayStar}, UNIT_SKILL1: {unitData.UNIT_SKILL1}, UNIT_SKILL2: {unitData.UNIT_SKILL2}");
        Debug.Log($"[UnitInfoDisplay] 제거 전 자식 개수: {skillListParent.childCount}");

        while (skillListParent.childCount > 0)
        {
            var child = skillListParent.GetChild(0);
            DestroyImmediate(child.gameObject);
        }

        Debug.Log($"[UnitInfoDisplay] 제거 후 자식 개수: {skillListParent.childCount}");

        // 1성일 때는 빈 프리팹 표시
        if (currentDisplayStar == 1)
        {
            Debug.Log("[UnitInfoDisplay] 1성 - skillEmptyPrefab 생성");
            if (skillEmptyPrefab != null)
            {
                Instantiate(skillEmptyPrefab, skillListParent);
            }
            else
            {
                Debug.LogWarning("[UnitInfoDisplay] skillEmptyPrefab이 null입니다!");
            }
            return;
        }

        Debug.Log($"[UnitInfoDisplay] {currentDisplayStar}성 - 실제 스킬 표시");

        // 2성 이상일 때만 실제 스킬 표시
        var skillTable = DataTableManager.SkillTable;
        if (skillTable == null)
        {
            Debug.LogError("[UnitInfoDisplay] SkillTable이 null입니다!");
            return;
        }

        if (skillItemPrefab == null)
        {
            Debug.LogError("[UnitInfoDisplay] skillItemPrefab이 null입니다!");
            return;
        }

        Debug.Log($"[UnitInfoDisplay] 스킬 생성 준비 완료 - SKILL1: {unitData.UNIT_SKILL1}, SKILL2: {unitData.UNIT_SKILL2}");

        if (currentDisplayStar == 2)
        {
            if (unitData.UNIT_SKILL1 > 0)
            {
                Debug.Log($"[UnitInfoDisplay] 2성 UNIT_SKILL1 생성 시작: {unitData.UNIT_SKILL1}");
                CreateSkillItem(unitData.UNIT_SKILL1, skillTable);
            }
        }
        else if (currentDisplayStar == 3)
        {
            if (unitData.UNIT_SKILL2 > 0)
            {
                Debug.Log($"[UnitInfoDisplay] 3성 UNIT_SKILL2 생성 시작: {unitData.UNIT_SKILL2}");
                CreateSkillItem(unitData.UNIT_SKILL2, skillTable);
            }
        }

        Debug.Log($"[UnitInfoDisplay] RefreshSkillList 완료 - 최종 자식 개수: {skillListParent.childCount}");
    }

    private void CreateSkillItem(int skillId, DataTable_Skill skillTable)
    {
        Debug.Log($"[UnitInfoDisplay] CreateSkillItem 시작 - skillId: {skillId}");

        if (skillItemPrefab == null || skillListParent == null)
        {
            Debug.LogError("[UnitInfoDisplay] skillItemPrefab 또는 skillListParent가 null!");
            return;
        }

        SkillData skill = skillTable.Get(skillId);
        if (skill == null)
        {
            Debug.LogWarning($"[UnitInfoDisplay] SkillData 없음: {skillId}");
            return;
        }

        Debug.Log($"[UnitInfoDisplay] SkillData 발견 - SKILL_NAME: {skill.SKILL_NAME}, SKILL_DESCRIPTION: {skill.SKILL_DESCRIPTION}, SKILL_ICON: {skill.SKILL_ICON}");

        var go = Instantiate(skillItemPrefab, skillListParent);
        if (go == null)
        {
            Debug.LogError("[UnitInfoDisplay] 프리팹 인스턴스화 실패!");
            return;
        }

        Debug.Log($"[UnitInfoDisplay] 프리팹 생성 완료: {go.name}");

        var icon = go.transform.Find("Icon")?.GetComponent<Image>();
        var nameText = go.transform.Find("SkillName")?.GetComponent<TextMeshProUGUI>();
        var descText = go.transform.Find("SkillDesc")?.GetComponent<TextMeshProUGUI>();

        Debug.Log($"[UnitInfoDisplay] 컴포넌트 찾기 - icon: {icon != null}, nameText: {nameText != null}, descText: {descText != null}");

        // String 테이블에서 스킬 이름 가져오기
        if (nameText != null)
        {
            string skillName = null;

            if (int.TryParse(skill.SKILL_NAME, out int nameId))
            {
                skillName = DataTableManager.GetString(nameId);
                Debug.Log($"[UnitInfoDisplay] 스킬 이름 조회 - nameId: {nameId}, result: {skillName}");
            }
            else
            {
                Debug.LogWarning($"[UnitInfoDisplay] SKILL_NAME 파싱 실패: {skill.SKILL_NAME}");
            }

            nameText.text = !string.IsNullOrEmpty(skillName) ? skillName : skill.SKILL_NAME;
        }
        else
        {
            Debug.LogWarning("[UnitInfoDisplay] nameText를 찾지 못했습니다!");
        }

        // String 테이블에서 스킬 설명 가져오기
        if (descText != null)
        {
            string skillDesc = null;

            if (int.TryParse(skill.SKILL_DESCRIPTION, out int descId))
            {
                skillDesc = DataTableManager.GetString(descId);
                Debug.Log($"[UnitInfoDisplay] 스킬 설명 조회 - descId: {descId}, result: {skillDesc}");
            }
            else
            {
                Debug.LogWarning($"[UnitInfoDisplay] SKILL_DESCRIPTION 파싱 실패: {skill.SKILL_DESCRIPTION}");
            }

            descText.text = !string.IsNullOrEmpty(skillDesc) ? skillDesc : skill.SKILL_DESCRIPTION;
        }
        else
        {
            Debug.LogWarning("[UnitInfoDisplay] descText를 찾지 못했습니다!");
        }

        if (icon != null && !string.IsNullOrEmpty(skill.SKILL_ICON))
        {
            Debug.Log($"[UnitInfoDisplay] 스킬 아이콘 로드 시작: {skill.SKILL_ICON}");
            LoadSkillIconAsync(icon, skill.SKILL_ICON).Forget();
        }
        else
        {
            Debug.LogWarning($"[UnitInfoDisplay] 아이콘 로드 실패 - icon null: {icon == null}, SKILL_ICON: {skill.SKILL_ICON}");
        }

        Debug.Log($"[UnitInfoDisplay] CreateSkillItem 완료 - skillId: {skillId}");
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
