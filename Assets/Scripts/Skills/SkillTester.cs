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
    public void OnSkillBTN1()
    {
        TestSkill(0, "EarthQuake");
    }

public void OnSkillBTN2()
    {
        TestSkill(2, "BlackHole");
    }
    public void OnSkillBTN3()
    {
        TestSkill(3, "AirForce");
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

        // 스킬 데이터가 아직 로드되지 않았으면 Init 호출
        if (!skill.IsSkillDataLoaded)
        {
            skill.Init();
        }

        Vector3 usePosition = spawnPoint != null ? spawnPoint.position : transform.position;
        skill.TryUse(usePosition);
    }

    
    [ContextMenu("Test All Skills")]
    private void TestAllSkills()
    {
        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i] != null)
            {
                Vector3 usePosition = spawnPoint != null ? spawnPoint.position : transform.position;
                skills[i].TryUse(usePosition);
            }
        }
    }
}
