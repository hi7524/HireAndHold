using TMPro;
using UnityEngine;

public class FrameCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval;

    private float deltaTime;
    private float lastUiUpdatedTime;

    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        if (Time.time >= lastUiUpdatedTime + updateInterval)
        {
            float fps = 1.0f / deltaTime;
            fpsText.text = $"{fps:0} FPS";
            lastUiUpdatedTime = Time.time;
        }
    }
}