using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 광석 던전 UI 관리
/// </summary>
public class OreDungeonUI : MonoBehaviour
{
    [SerializeField] private OreDungeonManager gameManager;
    [SerializeField] private TextMeshProUGUI touchCountText;
    [SerializeField] private TextMeshProUGUI oreCountText;

    [Header("패널")]
    [SerializeField] private GameObject defaultPanel;
    [SerializeField] private OreDungeonResultPanel resultPanel;

    [Header("애니메이션 설정")]
    [SerializeField] private float punchScale = 0.5f; // 펀치 스케일 크기
    [SerializeField] private float punchDuration = 0.3f; // 애니메이션 시간

    private void Awake()
    {
        if (!ValidateReferences())
            return;
    }

    private void OnEnable()
    {
        gameManager.OnInitialized += OnInitialized;
        gameManager.OnTouchCountChanged += UpdateTouchCount;
        gameManager.OnOreCountChanged += UpdateOreCount;
        gameManager.OnStageEnded += UpdateResultPanel;
    }

    private void OnDisable()
    {
        gameManager.OnInitialized -= OnInitialized;
        gameManager.OnTouchCountChanged -= UpdateTouchCount;
        gameManager.OnOreCountChanged -= UpdateOreCount;
        gameManager.OnStageEnded -= UpdateResultPanel;
    }

    private void OnInitialized()
    {
        // 초기 UI 설정
        UpdateTouchCount(gameManager.RemainTouchCount);
        UpdateOreCount(gameManager.RemainOreCount);
    }

    private void UpdateResultPanel(bool value)
    {
        resultPanel.SetResultPanel(value, gameManager.GetOreAmount);
        resultPanel.gameObject.SetActive(true);
    }

    private void UpdateTouchCount(int count)
    {
        if (touchCountText != null)
        {
            touchCountText.text = $"{count}";

            // 펀치 스케일 애니메이션
            touchCountText.transform.DOKill();
            touchCountText.transform.DOPunchScale(Vector3.one * punchScale, punchDuration, 1, 0.5f);
        }
    }

    private void UpdateOreCount(int count)
    {
        if (oreCountText != null)
        {
            oreCountText.text = $"{count}";

            // 펀치 스케일 애니메이션
            oreCountText.transform.DOKill();
            oreCountText.transform.DOPunchScale(Vector3.one * punchScale, punchDuration, 1, 0.5f);
        }
    }

    public void SetActiveDefaultPanel(bool value)
    {
        defaultPanel.SetActive(value);
    }

    private bool ValidateReferences()
    {
        if (gameManager == null)
        {
            Debug.LogError($"{nameof(OreDungeonManager)} 참조가 누락되었습니다.");
            return false;
        }

        if (touchCountText == null)
        {
            Debug.LogWarning($"{nameof(touchCountText)} 참조가 누락되었습니다.");
        }

        if (oreCountText == null)
        {
            Debug.LogWarning($"{nameof(oreCountText)} 참조가 누락되었습니다.");
        }

        return true;
    }
}
