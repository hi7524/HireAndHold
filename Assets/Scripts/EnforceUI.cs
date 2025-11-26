using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class EnforceUI : MonoBehaviour
{
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI unitInfoText;
    public TextMeshProUGUI resourceText;

    public TextMeshProUGUI normalLevelText;
    public TextMeshProUGUI normalCostText;
    public TextMeshProUGUI classText;
    public Button normalEnforceButton;

    public TextMeshProUGUI heroLevelText;
    public TextMeshProUGUI heroCostText;
    public TextMeshProUGUI heroEffectsText;
    public Button heroEnforceButton;

    private DataTable_Unit unitTable;
    private DataTable_NormalEnforce normalEnforceTable;
    private DataTable_HeroEnforce heroEnforceTable;
    private DataTable_HeroEnforceEffect heroEffectTable;

    private UnitManager unitManager;
    private NormalEnforceSystem normalEnforceSystem;
    private HeroEnforceSystem heroEnforceSystem;

    private int testUnitId = 11101;

    private async void Start()
    {
        unitTable = new DataTable_Unit();
        normalEnforceTable = new DataTable_NormalEnforce();
        heroEnforceTable = new DataTable_HeroEnforce();
        heroEffectTable = new DataTable_HeroEnforceEffect();

        try
        {
            await unitTable.LoadAsync("UnitTable");
            await normalEnforceTable.LoadAsync("NormalEnforceTable");
            await heroEnforceTable.LoadAsync("HeroEnforceTable");
            await heroEffectTable.LoadAsync("HeroEnforceEffectTable");
            Debug.Log("테이블 로드 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError("테이블 로드 실패: " + e.Message);
            return;
        }

        unitManager = new UnitManager(unitTable, normalEnforceTable, heroEnforceTable, heroEffectTable);
        normalEnforceSystem = new NormalEnforceSystem(unitManager, normalEnforceTable);
        heroEnforceSystem = new HeroEnforceSystem(unitManager, heroEnforceTable, heroEffectTable);

        normalEnforceSystem.TempGold = 999999;
        heroEnforceSystem.TempGold = 999999;
        normalEnforceSystem.TempMaterial = 9999;

        heroEnforceSystem.TempUnitFragments[testUnitId] = 100f;

        var unit = unitManager.AddUnit(testUnitId);
        if (unit == null)
        {
            Debug.LogError($"Unit id {testUnitId} 없음");
            return;
        }

        Debug.Log($"유닛 추가 완료: id={unit.UnitID}, Rank={unit.UnitRank}");

        UpdateUI();

        if (normalEnforceButton != null)
        {
            normalEnforceButton.onClick.AddListener(OnClick_NormalEnforce);
        }

        if (heroEnforceButton != null)
        {
            heroEnforceButton.onClick.AddListener(OnClick_HeroEnforce);
        }
    }

    private void UpdateUI()
    {
        var unit = unitManager.GetPlayerUnit(testUnitId);
        if (unit == null)
        {
            return;
        }
        if (attackText != null)
        {
            attackText.text = $"최종 공격력: {unit.CurrentAttack}";
        }

        if (unitInfoText != null)
        {
            unitInfoText.text = $"유닛 ID: {unit.UnitID}\n랭크: {unit.UnitRank}";
        }

        if (classText != null)
        {
            classText.text = $"등급: {unit.UnitRank}";
        }

        if (resourceText != null)
        {
            float fragments = heroEnforceSystem.TempUnitFragments.ContainsKey(testUnitId)
                ? heroEnforceSystem.TempUnitFragments[testUnitId]
                : 0f;
            resourceText.text = $"골드: {normalEnforceSystem.TempGold}\n" +
                               $"강화석: {normalEnforceSystem.TempMaterial}\n" +
                               $"유닛 조각: {fragments}";
        }

        UpdateNormalEnforceUI(unit);

        UpdateHeroEnforceUI(unit);
    }

    private void UpdateNormalEnforceUI(PlayerUnit unit)
    {
        if (normalLevelText != null)
        {
            normalLevelText.text = $"일반 강화 레벨: {unit.NormalEnforceLevel} / 20";
        }

        var (gold, material) = normalEnforceSystem.GetNextEnforceCost(testUnitId);

        if (normalCostText != null)
        {
            if (gold > 0)
            {
                normalCostText.text = $"다음 강화 비용\n골드: {gold} / 재료: {material}";
            }
            else
            {
                normalCostText.text = "최대 레벨 도달";
            }
        }

        if (normalEnforceButton != null)
        {
            bool canEnforce = normalEnforceSystem.CanEnforce(testUnitId, out _);
            normalEnforceButton.interactable = canEnforce;
        }
    }

    private void UpdateHeroEnforceUI(PlayerUnit unit)
    {
        if (heroLevelText != null)
        {
            heroLevelText.text = $"영웅 강화 레벨: {unit.HeroEnforceLevel} / 4";
        }

        var (gold, fragments) = heroEnforceSystem.GetNextEnforceCost(testUnitId);

        Debug.Log($"Next Hero Enforce Cost → Gold:{gold}, Fragment:{fragments}");

        if (heroCostText != null)
        {
            if (gold > 0)
            {
                heroCostText.text = $"다음 강화 비용\n골드: {gold} / 조각: {fragments}";
            }
            else
            {
                heroCostText.text = "최대 레벨 도달";
            }
        }

        if (heroEffectsText != null)
        {
            var effects = heroEnforceSystem.GetCurrentEffects(testUnitId);
            if (effects.Count > 0)
            {
                string effectsStr = "적용된 효과:\n";
                foreach (var effect in effects)
                {
                    effectsStr += $"• {effect.Enforce_Effect_DESCRIPTION}\n";
                }
                heroEffectsText.text = effectsStr;
            }
            else
            {
                heroEffectsText.text = "적용된 효과 없음";
            }
        }

        if (heroEnforceButton != null)
        {
            bool canEnforce = heroEnforceSystem.CanEnforce(testUnitId, out _);
            heroEnforceButton.interactable = canEnforce;
        }
    }

    public void OnClick_NormalEnforce()
    {
        bool success = normalEnforceSystem.TryEnforce(testUnitId);

        if (success)
        {
            Debug.Log("일반 강화 성공!");

            heroEnforceSystem.TempGold = normalEnforceSystem.TempGold;
        }
        else
        {
            Debug.LogWarning("일반 강화 안됨");
        }

        UpdateUI();
    }

    public void OnClick_HeroEnforce()
    {
        bool success = heroEnforceSystem.TryEnforce(testUnitId);

        if (success)
        {
            Debug.Log("영웅 강화 됨");

            normalEnforceSystem.TempGold = heroEnforceSystem.TempGold;

            var unit = unitManager.GetPlayerUnit(testUnitId);
            if (unit != null)
            {
                var skillEffects = unit.GetSkillEffects();
                Debug.Log($"누적 스킬 효과:");
                Debug.Log($"공격력 증가: +{skillEffects.TotalAttackUp}%");
                Debug.Log($"스킬 데미지: +{skillEffects.TotalSkillDamageUp}%");
                Debug.Log($"투사체: +{skillEffects.TotalProjectileUp}");
                Debug.Log($"지속시간: +{skillEffects.TotalDurationUp}");
                Debug.Log($"쿨타임: -{skillEffects.TotalCoolTimeDown}%");
            }
        }
        else
        {
            Debug.LogWarning("영웅 강화 실패");
        }

        UpdateUI();
    }

    public void OnClick_AddGold()
    {
        normalEnforceSystem.TempGold += 10000;
        heroEnforceSystem.TempGold += 10000; 
        UpdateUI();
        Debug.Log("골드 10000 추가");
    }

    public void OnClick_AddMaterial()
    {
        normalEnforceSystem.TempMaterial += 100;
        UpdateUI();
        Debug.Log("강화석 100 추가");
    }

    public void OnClick_AddFragment()
    {
        if (heroEnforceSystem.TempUnitFragments.ContainsKey(testUnitId))
        {
            heroEnforceSystem.TempUnitFragments[testUnitId] += 10f;
        }
        else
        {
            heroEnforceSystem.TempUnitFragments[testUnitId] = 10f;
        }
        UpdateUI();
        Debug.Log("유닛 조각 10 추가");
    }
}
