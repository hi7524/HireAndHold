using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛이 공격을 시작하는 범위를 정의하는 트리거 존.
/// BoxCollider2D와 함께 사용하여 적이 이 구역에 진입하면 공격을 시작합니다.
/// </summary>
public class AttackTriggerZone : MonoBehaviour
{
    /// <summary> 현재 공격 대상이 변경되었을 때 발생하는 이벤트 </summary>
    public event Action<Enemy> OnTargetChanged;

    private List<Enemy> enemyList = new List<Enemy>();
    private Enemy currentTarget;


    // 첫 번째 적 진입 시에만 타겟을 설정하여, 순차적으로 처리되도록 보장
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Enemy enemy))
        {
            enemyList.Add(enemy);
            enemy.OnDeath += HandleEnemyDied;

            if (currentTarget == null)
            {
                currentTarget = enemy;
                OnTargetChanged?.Invoke(currentTarget);
            }
        }
    }

    // 적 사망 시 List 정리 및 다음 타겟 갱신
    private void HandleEnemyDied(Enemy enemy)
    {
        enemy.OnDeath -= HandleEnemyDied;
        enemyList.Remove(enemy);

        if (currentTarget == enemy)
        {
            currentTarget = enemyList.Count > 0 ? enemyList[0] : null;
            OnTargetChanged?.Invoke(currentTarget);
        }
    }

    /// <summary>
    /// 현재 존 내에 있는 적 목록을 반환합니다.
    /// 각 유닛이 자신의 위치 기준으로 최단거리 적을 찾는데 사용됩니다.
    /// </summary>
    public IReadOnlyList<Enemy> GetEnemiesInZone() => enemyList;
}