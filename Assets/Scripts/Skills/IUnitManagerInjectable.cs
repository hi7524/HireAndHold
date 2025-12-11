/// <summary>
/// BattleUnitManager 의존성 주입을 위한 인터페이스
/// 버프 스킬(깃발 스킬 등)에서 구현하여 FindObjectsByType 대신 사용
/// </summary>
public interface IUnitManagerInjectable
{
    void SetBattleUnitManager(BattleUnitManager manager);
}
