// SkillTester.cs - 새로 만들 파일
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillTester : MonoBehaviour
{
    [SerializeField] private EarthQuakeSkill earthquakeSkill;
    [SerializeField] private Transform spawnPoint; // 스킬 발동 위치
    [SerializeField] private KeyCode testKey = KeyCode.Space; // 테스트 키

    private void Update()
    {
        // 스페이스바 누르면 스킬 발동
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (earthquakeSkill != null)
            {
                Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
                earthquakeSkill.TryUse(position);
            }
            else
            {
                Debug.LogError("EarthQuakeSkill이 할당되지 않았습니다!");
            }
        }
    }
}