using UnityEngine;

public struct SkillProjectileData
{
    public ObjectPoolManager poolManager;
    public string poolKey;
    public float damage;
    public bool isCritical;
    public Transform target;
    public Vector3 spawnPosition;
    public Vector3 targetPosition;
    public Vector3 customDirection;
    public float range;
}
