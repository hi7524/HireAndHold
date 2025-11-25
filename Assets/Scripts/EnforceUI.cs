using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class EnforceUI : MonoBehaviour
{
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI resourceText;
    public TextMeshProUGUI nextEnforceCostText;
    public Button enforceButton;

    private DataTable_Unit unitTable;
    private DataTable_NormalEnforce enforceTable;
    private UnitManager unitManager;
    private NormalEnforceSystem enforceSystem;

    private int testUnitId = 11101;

    private async void Start()
    {
        //테이블 생성
        unitTable = new DataTable_Unit();
        enforceTable = new DataTable_NormalEnforce();

        try
        {
            await unitTable.LoadAsync("UnitTable");
            await enforceTable.LoadAsync("NormalEnforceTable");
            Debug.Log("테이블 로드 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError("테이블 로드 실패: " + e.Message);
            return;
        }

        // 시스템 생성
        unitManager = new UnitManager(unitTable, enforceTable);
        enforceSystem = new NormalEnforceSystem(unitManager, enforceTable);

        // 테스트 유닛 추가
        var unit = unitManager.AddUnit(testUnitId);
        if (unit == null)
        {
            Debug.LogError($"Unit id {testUnitId} 없음");
            return;
        }

        Debug.Log($"유닛 추가 완료: id ={unit.UnitID}, Rank={unit.UnitRank}");

        // 초기화
        UpdateUI();

        // 버튼 이벤트 연결
        if (enforceButton != null)
        {
            enforceButton.onClick.AddListener(OnClick_Enforce);
        }
    }

    private void UpdateUI()
    {
        var unit = unitManager.GetPlayerUnit(testUnitId);
        if (unit == null)
        {
            return;
        }

        attackText.text = $"공격력: {unit.CurrentAttack}";
        levelText.text = $"강화 레벨: {unit.NormalEnforceLevel} / 20";
        resourceText.text = $"골드: {enforceSystem.TempGold} / 재료: {enforceSystem.TempMaterial}";

        var (gold, material) = enforceSystem.GetNextEnforceCost(testUnitId);
        if (gold > 0 && nextEnforceCostText != null)
        {
            nextEnforceCostText.text = $"다음 강화 비용\n골드: {gold} / 재료: {material}";
        }

        if (enforceButton != null)
        {
            bool canEnforce = enforceSystem.CanEnforce(testUnitId, out _);
            enforceButton.interactable = canEnforce;
        }
    }

    public void OnClick_Enforce()
    {
        bool success = enforceSystem.TryEnforce(testUnitId);

        if (success)
        {
            Debug.Log("강화 성공");
        }
        else
        {
            Debug.LogWarning("강화 실패");
        }

        UpdateUI();
    }
}
