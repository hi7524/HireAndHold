using System.Collections.Generic;

/// <summary>
/// 몬스터 리스트를 제공하는 인터페이스
/// MonsterSpawner와 TestMonsterSpawnController가 구현
/// </summary>
public interface IMonsterProvider
{
    IReadOnlyList<Enemy> GetActiveMonsters();
}

/// <summary>
/// 활성 몬스터 제공자를 관리하는 정적 레지스트리
/// MonsterSpawner.Instance가 없는 테스트 씬에서 대체 제공자를 사용할 수 있게 함
/// </summary>
public static class MonsterProviderRegistry
{
    private static IMonsterProvider registeredProvider;

    /// <summary>
    /// 몬스터 제공자 등록 (테스트 씬에서 사용)
    /// </summary>
    public static void Register(IMonsterProvider provider)
    {
        registeredProvider = provider;
    }

    /// <summary>
    /// 몬스터 제공자 등록 해제
    /// </summary>
    public static void Unregister(IMonsterProvider provider)
    {
        if (registeredProvider == provider)
        {
            registeredProvider = null;
        }
    }

    /// <summary>
    /// 활성 몬스터 리스트 반환
    /// MonsterSpawner.Instance 우선, 없으면 등록된 제공자 사용
    /// </summary>
    public static IReadOnlyList<Enemy> GetActiveMonsters()
    {
        // MonsterSpawner.Instance가 있으면 우선 사용
        if (MonsterSpawner.Instance != null)
        {
            return MonsterSpawner.Instance.GetActiveMonsters();
        }

        // 등록된 대체 제공자가 있으면 사용
        if (registeredProvider != null)
        {
            return registeredProvider.GetActiveMonsters();
        }

        // 둘 다 없으면 빈 리스트 반환
        return System.Array.Empty<Enemy>();
    }

    /// <summary>
    /// 몬스터 제공자가 있는지 확인
    /// </summary>
    public static bool HasProvider => MonsterSpawner.Instance != null || registeredProvider != null;
}
