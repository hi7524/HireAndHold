using UnityEngine;
using UnityEngine.UI;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 블로커에 구멍을 뚫는 컴포넌트
    /// 4개의 패널로 구멍 주변을 감싸는 방식
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TutorialBlockerHole : MonoBehaviour, ICanvasRaycastFilter
    {
        private RectTransform holeRect;
        private RectTransform holeRect2;  // 두 번째 구멍
        private Image mainImage;
        private RectTransform mainRect;

        // 4방향 패널 (상, 하, 좌, 우)
        private Image[] sidePanels;
        private RectTransform[] sidePanelRects;
        private bool initialized;

        private void Awake()
        {
            mainImage = GetComponent<Image>();
            mainRect = GetComponent<RectTransform>();
        }

        private void InitializeSidePanels()
        {
            if (initialized) return;
            initialized = true;

            sidePanels = new Image[4];
            sidePanelRects = new RectTransform[4];
            string[] names = { "TopPanel", "BottomPanel", "LeftPanel", "RightPanel" };

            Color dimColor = mainImage != null ? mainImage.color : new Color(0, 0, 0, 0.7f);

            for (int i = 0; i < 4; i++)
            {
                var panelObj = new GameObject(names[i]);
                panelObj.transform.SetParent(transform.parent, false);

                var rect = panelObj.AddComponent<RectTransform>();
                var img = panelObj.AddComponent<Image>();
                img.color = dimColor;
                img.raycastTarget = true;

                sidePanels[i] = img;
                sidePanelRects[i] = rect;

                // 메인 블로커와 같은 sibling order 유지
                panelObj.transform.SetSiblingIndex(transform.GetSiblingIndex());
                panelObj.SetActive(false);
            }
        }

        /// <summary>
        /// 구멍 영역 설정
        /// </summary>
        public void SetHole(RectTransform rect)
        {
            holeRect = rect;

            if (rect == null)
            {
                ClearHole();
                return;
            }

            InitializeSidePanels();
            UpdateSidePanels();
        }

        /// <summary>
        /// 구멍 제거
        /// </summary>
        public void ClearHole()
        {
            holeRect = null;

            // 두 번째 구멍도 없으면 메인 이미지 복원
            if (holeRect2 == null)
            {
                if (mainImage != null)
                    mainImage.enabled = true;

                // 사이드 패널 숨기기
                if (sidePanels != null)
                {
                    foreach (var panel in sidePanels)
                    {
                        if (panel != null)
                            panel.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                // 두 번째 구멍만 있으면 그것만 표시
                UpdateSidePanels();
            }
        }

        /// <summary>
        /// 두 번째 구멍 영역 설정
        /// </summary>
        public void SetHole2(RectTransform rect)
        {
            holeRect2 = rect;

            if (rect == null)
            {
                ClearHole2();
                return;
            }

            InitializeSidePanels();
            UpdateSidePanels();
        }

        /// <summary>
        /// 두 번째 구멍 제거
        /// </summary>
        public void ClearHole2()
        {
            holeRect2 = null;

            // 첫 번째 구멍도 없으면 메인 이미지 복원
            if (holeRect == null)
            {
                if (mainImage != null)
                    mainImage.enabled = true;

                // 사이드 패널 숨기기
                if (sidePanels != null)
                {
                    foreach (var panel in sidePanels)
                    {
                        if (panel != null)
                            panel.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                // 첫 번째 구멍만 있으면 그것만 표시
                UpdateSidePanels();
            }
        }

        /// <summary>
        /// 4방향 패널 위치/크기 업데이트
        /// </summary>
        private void UpdateSidePanels()
        {
            if (holeRect == null || sidePanelRects == null) return;

            // 메인 이미지 숨기기 (사이드 패널로 대체)
            if (mainImage != null)
                mainImage.enabled = false;

            // 구멍의 스크린 좌표 계산
            Vector3[] holeCorners = new Vector3[4];
            holeRect.GetWorldCorners(holeCorners);

            // 캔버스 좌표로 변환
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            // 구멍의 min/max 로컬 좌표 계산
            Vector2 holeMin, holeMax;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, RectTransformUtility.WorldToScreenPoint(cam, holeCorners[0]), cam, out holeMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, RectTransformUtility.WorldToScreenPoint(cam, holeCorners[2]), cam, out holeMax);

            // 캔버스 크기
            Vector2 canvasSize = canvasRect.rect.size;
            float canvasMinX = -canvasSize.x / 2;
            float canvasMaxX = canvasSize.x / 2;
            float canvasMinY = -canvasSize.y / 2;
            float canvasMaxY = canvasSize.y / 2;

            // Top 패널 (구멍 위쪽 전체)
            SetPanelRect(sidePanelRects[0], canvasMinX, holeMax.y, canvasSize.x, canvasMaxY - holeMax.y);

            // Bottom 패널 (구멍 아래쪽 전체)
            SetPanelRect(sidePanelRects[1], canvasMinX, canvasMinY, canvasSize.x, holeMin.y - canvasMinY);

            // Left 패널 (구멍 왼쪽, 위아래 제외)
            SetPanelRect(sidePanelRects[2], canvasMinX, holeMin.y, holeMin.x - canvasMinX, holeMax.y - holeMin.y);

            // Right 패널 (구멍 오른쪽, 위아래 제외)
            SetPanelRect(sidePanelRects[3], holeMax.x, holeMin.y, canvasMaxX - holeMax.x, holeMax.y - holeMin.y);

            // 패널 활성화
            foreach (var panel in sidePanels)
            {
                if (panel != null)
                    panel.gameObject.SetActive(true);
            }
        }

        private void SetPanelRect(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(Mathf.Max(0, width), Mathf.Max(0, height));
        }

        /// <summary>
        /// ICanvasRaycastFilter 구현
        /// 구멍 영역 안쪽이면 raycast 무시 (터치 통과)
        /// </summary>
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            // 첫 번째 구멍 체크
            bool isInsideHole1 = holeRect != null && holeRect.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(holeRect, screenPoint, eventCamera);

            // 두 번째 구멍 체크
            bool isInsideHole2 = holeRect2 != null && holeRect2.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(holeRect2, screenPoint, eventCamera);

            // 어느 구멍이든 안쪽이면 false 반환 (터치 통과)
            return !(isInsideHole1 || isInsideHole2);
        }

        private void OnDestroy()
        {
            // 생성한 패널들 정리
            if (sidePanels != null)
            {
                foreach (var panel in sidePanels)
                {
                    if (panel != null)
                        Destroy(panel.gameObject);
                }
            }
        }
    }
}
