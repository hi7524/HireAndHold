using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
public class LoadingSceneUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static LoadingSceneUI Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private Image loadingIcon;

    [Header("Animation")]
    [SerializeField] private bool rotateIcon = true;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Tips")]
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private List<string> loadingTips;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 랜덤 팁 표시
        ShowRandomTip();
    }

    private void Update()
    {
        // 로딩 아이콘 회전
        if (rotateIcon && loadingIcon != null)
        {
            loadingIcon.transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 진행도 업데이트
    /// </summary>
    public void UpdateProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (percentText != null)
        {
            percentText.text = $"{progress * 100:F0}%";
        }
    }

    /// <summary>
    /// 로딩 텍스트 업데이트
    /// </summary>
    public void UpdateLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
    }

    /// <summary>
    /// 랜덤 팁 표시
    /// </summary>
    private void ShowRandomTip()
    {
        if (tipText != null && loadingTips != null && loadingTips.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, loadingTips.Count);
            tipText.text = loadingTips[randomIndex];
        }
    }

    private void OnDestroy()
    {
        Instance = null;
    }
}
