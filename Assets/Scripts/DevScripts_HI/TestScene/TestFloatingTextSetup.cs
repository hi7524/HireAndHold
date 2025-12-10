using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// TestScene 전용 FloatingTextSpawner 초기화
/// FloatingTextSpawner 오브젝트에 추가하여 사용
/// </summary>
[RequireComponent(typeof(FloatingTextSpawner))]
public class TestFloatingTextSetup : MonoBehaviour
{
    private void Awake()
    {
        var spawner = GetComponent<FloatingTextSpawner>();
        if (spawner == null) return;

        // floatingTextRoot 자동 탐색
        if (spawner.floatingTextRoot == null)
        {
            var rootObj = GameObject.Find("FloatingTextRoot");
            if (rootObj != null)
            {
                spawner.floatingTextRoot = rootObj.GetComponent<RectTransform>();
            }
        }

        // floatingTextPrefab 자동 로드
        if (spawner.floatingTextPrefab == null)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>("DamageFloatingText");
            var prefab = handle.WaitForCompletion();
            if (prefab != null)
            {
                spawner.floatingTextPrefab = prefab.GetComponent<FloatingText>();
            }
        }
    }
}
