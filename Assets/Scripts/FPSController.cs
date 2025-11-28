using UnityEngine;

public class FpsController : MonoBehaviour
{
    [SerializeField]
    private int targetFrameRate = 60;

    void Start()
    {
        // 60 FPS로 프레임 속도 제한 설정
        Application.targetFrameRate = targetFrameRate;
    }
}