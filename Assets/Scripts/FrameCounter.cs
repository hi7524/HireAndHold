using TMPro;
using UnityEngine;

public class FrameCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.1f;

    private float deltaTime;
    private float lastUiUpdatedTime;

    private void Start()
    {
        if (fpsText == null)
        {
            Debug.LogError("FPS Text is not assigned in FrameCounter!");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        if (Time.unscaledTime >= lastUiUpdatedTime + updateInterval)
        {
            float fps = 1.0f / deltaTime;
            string fpsString = $"{fps:0} FPS";
            fpsText.text = fpsString;
            lastUiUpdatedTime = Time.unscaledTime;
        }
    }
}