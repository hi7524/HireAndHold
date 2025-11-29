using UnityEngine;
using Cysharp.Threading.Tasks;

// 테스트용 유닛 스탯 관련 스크립트
public class UnitStats : MonoBehaviour
{
    public int id = 11101;

    private UnitData unitData;

    private Stat attackDamage;
    private Stat attackRange;
    private Stat attackCoolTime;
    private Stat criticalRate;
    private Stat criticalDamage;
    private int unitSkill1ID;
    private int unitSkill2ID;
    private string unitIconID;

    private async void Start()
    {
        await DataTableManager.InitAsync();
        unitData = DataTableManager.UnitTable.Get(id);

        if (unitData != null)
        {
            Debug.Log($"[UnitStats] 유닛 데이터 로드 성공!");
            Debug.Log($"ID: {unitData.UNIT_ID}, Name: {unitData.NAME}, Rank: {unitData.RANK}, Level: {unitData.LEVEL}, " +
                      $"Attack: {unitData.ATTACK}, Range: {unitData.ATTACK_RANGE}, Cooltime: {unitData.ATTACK_COOLTIME}, " +
                      $"BoltNum: {unitData.BOLT_NUM}, Crit: {unitData.ATTACK_CRITICAL}%, CritDmg: {unitData.CRITICAL_DAMAGE}x, " +
                      $"Skill1: {unitData.UNIT_SKILL1}, Skill2: {unitData.UNIT_SKILL2}");
        }
        else
        {
            Debug.LogError($"[UnitStats] Unit ID {id}를 찾을 수 없습니다.");
        }

        if (unitData != null)
        {
            // 데이터 테이블에서 로드한 값으로 Stat 초기화
            attackDamage = new Stat(unitData.ATTACK);
            attackRange = new Stat(unitData.ATTACK_RANGE);
            attackCoolTime = new Stat(unitData.ATTACK_COOLTIME);
            criticalRate = new Stat(unitData.ATTACK_CRITICAL);
            criticalDamage = new Stat(unitData.CRITICAL_DAMAGE);
        }

        //  저장된 영구 강화 불러오기 예시
        // PlayerUpgradeData upgradeData = SaveManager.Instance.LoadUpgradeData();
        // Attack.SetUpgradeValue(upgradeData.AttackUpgrade);
        // Defense.SetUpgradeValue(upgradeData.DefenseUpgrade);
        // MaxHealth.SetUpgradeValue(upgradeData.HealthUpgrade);
    }
}