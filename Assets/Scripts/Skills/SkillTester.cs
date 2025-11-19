using UnityEngine;
using UnityEngine.InputSystem;    

/// <summary>
/// 스킬 테스트용 스크립트
/// 키보드 1~9번 키로 각 스킬을 테스트할 수 있습니다.
/// </summary>
public class SkillTester : MonoBehaviour
{
    [Header("테스트할 스킬들")]
    [SerializeField] private PlayerSkillBase[] skills;

    [Header("스킬 발동 위치")]
    [SerializeField] private Transform spawnPoint;

    private void Update()
    {
        // 숫자 키 1~9로 스킬 테스트
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            TestSkill(0, "EarthQuake");
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            TestSkill(1, "EternalBlizzard");
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            TestSkill(2, "BlackHole");
        }
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            TestSkill(3, "AirForce");
        }
        else if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            TestSkill(4, "ChaosWave");
        }
        else if (Keyboard.current.digit6Key.wasPressedThisFrame)
        {
            TestSkill(5, "AnkleCatch");
        }
        else if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            TestSkill(6, "Supernova");
        }
        else if (Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            TestSkill(7, "GreatSlow");
        }
    }

    private void TestSkill(int index, string skillName)
    {
        if (index < 0 || index >= skills.Length)
        {
            Debug.LogWarning($"[SkillTester] 스킬 슬롯 {index}가 비어있습니다!");
            return;
        }

        PlayerSkillBase skill = skills[index];
        if (skill == null)
        {
            Debug.LogWarning($"[SkillTester] {skillName} 스킬이 할당되지 않았습니다!");
            return;
        }

        Vector3 usePosition = spawnPoint != null ? spawnPoint.position : transform.position;

        Debug.Log($"[SkillTester] {skillName} 테스트 발동! (키: {index + 1})");
        skill.TryUse(usePosition);
    }

    
    [ContextMenu("Test All Skills")]
    private void TestAllSkills()
    {
        Debug.Log("[SkillTester] 모든 스킬 순차 테스트 시작!");

        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i] != null)
            {
                Vector3 usePosition = spawnPoint != null ? spawnPoint.position : transform.position;
                Debug.Log($"[SkillTester] {i + 1}번 스킬: {skills[i].GetType().Name}");
                skills[i].TryUse(usePosition);
            }
        }

        Debug.Log("[SkillTester] 모든 스킬 테스트 완료!");
    }
}
