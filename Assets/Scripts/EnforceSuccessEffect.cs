using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 강화 성공 시 별이 육각형 모양으로 퍼지는 애니메이션
/// </summary>
public class EnforceSuccessEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject starPrefab; // 별 이미지 프리팹
    [SerializeField] private Transform effectParent; // 별들이 생성될 부모
    [SerializeField] private int starCount = 6; // 별 개수 (육각형이므로 6개)
    [SerializeField] private float spreadDistance = 150f; // 퍼지는 거리
    [SerializeField] private float duration = 0.8f; // 애니메이션 지속시간
    [SerializeField] private Ease easeType = Ease.OutQuad; // 이징 타입

    [Header("Colors")]
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = new Color(1f, 1f, 1f, 0f); // 투명

    /// <summary>
    /// 강화 성공 효과 재생
    /// </summary>
    public async UniTask PlayEffect()
    {
        if (starPrefab == null || effectParent == null)
        {
            Debug.LogWarning("[EnforceSuccessEffect] starPrefab 또는 effectParent가 설정되지 않았습니다!");
            return;
        }

        // 별 생성 및 애니메이션
        for (int i = 0; i < starCount; i++)
        {
            CreateAndAnimateStar(i);
        }

        // 애니메이션이 끝날 때까지 대기
        await UniTask.Delay((int)(duration * 1000));
    }

    private void CreateAndAnimateStar(int index)
    {
        // 별 생성
        GameObject star = Instantiate(starPrefab, effectParent);
        RectTransform rect = star.GetComponent<RectTransform>();
        Image image = star.GetComponent<Image>();

        if (rect == null || image == null)
        {
            Debug.LogError("[EnforceSuccessEffect] 별 프리팹에 RectTransform 또는 Image 컴포넌트가 없습니다!");
            Destroy(star);
            return;
        }

        // 시작 위치 (중앙)
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.zero; // 작게 시작

        // 시작 색상
        image.color = startColor;

        // 육각형 방향 계산 (60도씩 회전)
        float angle = (360f / starCount) * index;
        float radian = angle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
        Vector2 targetPosition = direction * spreadDistance;

        // 애니메이션 시퀀스
        Sequence seq = DOTween.Sequence();

        // 1. 스케일 업 (0 → 1)
        seq.Append(rect.DOScale(1f, duration * 0.2f).SetEase(Ease.OutBack));

        // 2. 위치 이동 + 페이드 아웃 (동시에)
        seq.Join(rect.DOAnchorPos(targetPosition, duration).SetEase(easeType));
        seq.Join(image.DOColor(endColor, duration).SetEase(Ease.InQuad));

        // 3. 스케일 다운 (후반부)
        seq.Insert(duration * 0.6f, rect.DOScale(0.5f, duration * 0.4f).SetEase(Ease.InQuad));

        // 애니메이션 완료 후 오브젝트 삭제
        seq.OnComplete(() => Destroy(star));
    }

    /// <summary>
    /// Inspector에서 테스트용
    /// </summary>
    [ContextMenu("Test Effect")]
    private void TestEffect()
    {
        PlayEffect().Forget();
    }
}
