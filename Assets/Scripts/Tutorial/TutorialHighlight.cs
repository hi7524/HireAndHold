using UnityEngine;
using UnityEngine.UI;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 하이라이트 컴포넌트
    /// 빨간색 테두리로 대상 UI를 강조 표시
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class TutorialHighlight : MonoBehaviour
    {
        [Header("테두리 설정")]
        [SerializeField] private float borderWidth = 4f;
        [SerializeField] private Color borderColor = new Color(1f, 0f, 0f, 1f); // 빨간색

        [Header("테두리 이미지")]
        [SerializeField] private Image topBorder;
        [SerializeField] private Image bottomBorder;
        [SerializeField] private Image leftBorder;
        [SerializeField] private Image rightBorder;

        [Header("펄스 애니메이션")]
        [SerializeField] private bool enablePulse = true;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMinAlpha = 0.5f;
        [SerializeField] private float pulseMaxAlpha = 1f;

        private RectTransform rectTransform;
        private float pulseTime;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            SetupBorders();
        }

        private void OnEnable()
        {
            pulseTime = 0f;
            UpdateBorderColors(borderColor);
        }

        private void Update()
        {
            if (enablePulse)
            {
                pulseTime += Time.unscaledDeltaTime * pulseSpeed;
                float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, (Mathf.Sin(pulseTime) + 1f) / 2f);
                Color pulseColor = borderColor;
                pulseColor.a = alpha;
                UpdateBorderColors(pulseColor);
            }
        }

        /// <summary>
        /// 테두리 초기 설정
        /// </summary>
        private void SetupBorders()
        {
            if (topBorder == null || bottomBorder == null || leftBorder == null || rightBorder == null)
            {
                CreateBorders();
            }

            // 모든 테두리 raycastTarget 끄기 (터치 통과)
            if (topBorder != null) topBorder.raycastTarget = false;
            if (bottomBorder != null) bottomBorder.raycastTarget = false;
            if (leftBorder != null) leftBorder.raycastTarget = false;
            if (rightBorder != null) rightBorder.raycastTarget = false;

            UpdateBorderSizes();
        }

        /// <summary>
        /// 테두리 이미지 생성
        /// </summary>
        private void CreateBorders()
        {
            // Top Border
            if (topBorder == null)
            {
                var topObj = new GameObject("TopBorder", typeof(RectTransform), typeof(Image));
                topObj.transform.SetParent(transform, false);
                topBorder = topObj.GetComponent<Image>();
                topBorder.raycastTarget = false; // 터치 통과
                var topRect = topObj.GetComponent<RectTransform>();
                topRect.anchorMin = new Vector2(0, 1);
                topRect.anchorMax = new Vector2(1, 1);
                topRect.pivot = new Vector2(0.5f, 1);
                topRect.anchoredPosition = Vector2.zero;
            }

            // Bottom Border
            if (bottomBorder == null)
            {
                var bottomObj = new GameObject("BottomBorder", typeof(RectTransform), typeof(Image));
                bottomObj.transform.SetParent(transform, false);
                bottomBorder = bottomObj.GetComponent<Image>();
                bottomBorder.raycastTarget = false; // 터치 통과
                var bottomRect = bottomObj.GetComponent<RectTransform>();
                bottomRect.anchorMin = new Vector2(0, 0);
                bottomRect.anchorMax = new Vector2(1, 0);
                bottomRect.pivot = new Vector2(0.5f, 0);
                bottomRect.anchoredPosition = Vector2.zero;
            }

            // Left Border
            if (leftBorder == null)
            {
                var leftObj = new GameObject("LeftBorder", typeof(RectTransform), typeof(Image));
                leftObj.transform.SetParent(transform, false);
                leftBorder = leftObj.GetComponent<Image>();
                leftBorder.raycastTarget = false; // 터치 통과
                var leftRect = leftObj.GetComponent<RectTransform>();
                leftRect.anchorMin = new Vector2(0, 0);
                leftRect.anchorMax = new Vector2(0, 1);
                leftRect.pivot = new Vector2(0, 0.5f);
                leftRect.anchoredPosition = Vector2.zero;
            }

            // Right Border
            if (rightBorder == null)
            {
                var rightObj = new GameObject("RightBorder", typeof(RectTransform), typeof(Image));
                rightObj.transform.SetParent(transform, false);
                rightBorder = rightObj.GetComponent<Image>();
                rightBorder.raycastTarget = false; // 터치 통과
                var rightRect = rightObj.GetComponent<RectTransform>();
                rightRect.anchorMin = new Vector2(1, 0);
                rightRect.anchorMax = new Vector2(1, 1);
                rightRect.pivot = new Vector2(1, 0.5f);
                rightRect.anchoredPosition = Vector2.zero;
            }
        }

        /// <summary>
        /// 테두리 크기 업데이트
        /// </summary>
        private void UpdateBorderSizes()
        {
            if (topBorder != null)
            {
                var rect = topBorder.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(0, borderWidth);
            }

            if (bottomBorder != null)
            {
                var rect = bottomBorder.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(0, borderWidth);
            }

            if (leftBorder != null)
            {
                var rect = leftBorder.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(borderWidth, 0);
            }

            if (rightBorder != null)
            {
                var rect = rightBorder.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(borderWidth, 0);
            }
        }

        /// <summary>
        /// 테두리 색상 업데이트
        /// </summary>
        private void UpdateBorderColors(Color color)
        {
            if (topBorder != null) topBorder.color = color;
            if (bottomBorder != null) bottomBorder.color = color;
            if (leftBorder != null) leftBorder.color = color;
            if (rightBorder != null) rightBorder.color = color;
        }

        /// <summary>
        /// 테두리 너비 설정
        /// </summary>
        public void SetBorderWidth(float width)
        {
            borderWidth = width;
            UpdateBorderSizes();
        }

        /// <summary>
        /// 테두리 색상 설정
        /// </summary>
        public void SetBorderColor(Color color)
        {
            borderColor = color;
            UpdateBorderColors(color);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && topBorder != null)
            {
                UpdateBorderSizes();
                UpdateBorderColors(borderColor);
            }
        }
#endif
    }
}
