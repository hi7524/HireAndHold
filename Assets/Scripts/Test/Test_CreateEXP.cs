using UnityEngine;

// KHI: 경험치 생성 테스트
public class Test_CreateEXP : MonoBehaviour
{
    public Experience expPrf;
    public ExperienceCollector experienceCollector;

    public void OnClickCreateExpPrf()
    {
        var exp = Instantiate(expPrf);

        Vector3 spawnPos = new Vector3(
            Random.Range(-1.5f, 1.5f),
            Random.Range(-1.0f, 1.0f),
            0f
        );

        exp.transform.position = spawnPos;
        exp.SetExpCollecter(experienceCollector);
    }
}