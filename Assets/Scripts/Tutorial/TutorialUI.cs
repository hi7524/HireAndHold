using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 UI - 대화창, 하이라이트, 손가락 가이드
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        [Header("대화창")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private TextMeshProUGUI dialogText;
        [SerializeField] private Image characterImage;
        [SerializeField] private RectTransform dialogRect;

        [Header("하이라이트 (첫 번째)")]
        [SerializeField] private GameObject highlightObject;
        [SerializeField] private RectTransform highlightRect;

        [Header("하이라이트 (두 번째)")]
        [SerializeField] private GameObject highlightObject2;
        [SerializeField] private RectTransform highlightRect2;

        [Header("손가락 가이드")]
        [SerializeField] private GameObject handGuideObject;
        [SerializeField] private RectTransform handGuideRect;
        [SerializeField] private Animator handGuideAnimator;
        [SerializeField] private float dragGuideDuration = 1f;
        [SerializeField] private float dragGuideDelay = 0.3f;

        private CancellationTokenSource dragGuideCts;

        [Header("타이핑 설정")]
        [SerializeField] private float typingSpeed = 0.05f;
        [SerializeField] private bool enableTypingEffect = true;

        [Header("앵커 위치")]
        [SerializeField] private Vector2 topAnchorPosition = new Vector2(0, 300);
        [SerializeField] private Vector2 centerAnchorPosition = new Vector2(0, 0);
        [SerializeField] private Vector2 bottomAnchorPosition = new Vector2(0, -300);

        // 상태
        private bool isTyping;
        private bool skipTyping;
        private bool isTouched;
        private string fullText;
        private Coroutine typingCoroutine;

        // 터치 대기용
        private UniTaskCompletionSource touchCompletionSource;

        private void Awake()
        {
            Hide();
        }

        #region 대화창

        /// <summary>
        /// 대화창 표시 (타이핑 효과 포함)
        /// </summary>
        public async UniTask ShowDialogAsync(string text, DialogAnchor anchor, Vector2 customPosition, bool showCharacter)
        {
            fullText = text;
            isTouched = false;
            skipTyping = false;

            // 위치 설정
            SetDialogPosition(anchor, customPosition);

            // 캐릭터 표시
            if (characterImage != null)
            {
                characterImage.gameObject.SetActive(showCharacter);
            }

            // 패널 표시
            dialogPanel?.SetActive(true);

            // 타이핑 효과
            if (enableTypingEffect)
            {
                await TypeTextAsync(text);
            }
            else
            {
                if (dialogText != null)
                {
                    dialogText.text = text;
                }
            }
        }

        /// <summary>
        /// 대화창 위치 설정
        /// </summary>
        private void SetDialogPosition(DialogAnchor anchor, Vector2 customPosition)
        {
            if (dialogRect == null) return;

            Vector2 position = anchor switch
            {
                DialogAnchor.Top => topAnchorPosition,
                DialogAnchor.Center => centerAnchorPosition,
                DialogAnchor.Bottom => bottomAnchorPosition,
                DialogAnchor.Custom => customPosition,
                _ => bottomAnchorPosition
            };

            dialogRect.anchoredPosition = position;
        }

        /// <summary>
        /// 타이핑 효과
        /// </summary>
        private async UniTask TypeTextAsync(string text)
        {
            if (dialogText == null) return;

            isTyping = true;
            dialogText.text = "";

            foreach (char c in text)
            {
                if (skipTyping)
                {
                    dialogText.text = text;
                    break;
                }

                dialogText.text += c;
                await UniTask.Delay(TimeSpan.FromSeconds(typingSpeed), ignoreTimeScale: true);
            }

            isTyping = false;
        }

        /// <summary>
        /// 터치 대기
        /// </summary>
        public async UniTask WaitForTouchAsync()
        {
            touchCompletionSource = new UniTaskCompletionSource();
            await touchCompletionSource.Task;
        }

        /// <summary>
        /// 터치 이벤트 (버튼에서 호출)
        /// </summary>
        public void OnDialogTouched()
        {
            if (isTyping)
            {
                // 타이핑 중이면 스킵
                skipTyping = true;
            }
            else
            {
                // 타이핑 완료 후면 다음으로
                isTouched = true;
                touchCompletionSource?.TrySetResult();
            }
        }

        /// <summary>
        /// 대화창 숨기기
        /// </summary>
        public void HideDialog()
        {
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(false);
            }
        }

        #endregion

        #region 하이라이트

        /// <summary>
        /// 하이라이트 표시 (UnitId 기반)
        /// </summary>
        public void ShowHighlightByUnitId(int unitId, Vector2 offset, Vector2 size)
        {
            if (unitId <= 0) return;

            var target = FindUIObjectByUnitId(unitId);
            if (target == null)
            {
                Debug.LogWarning($"[TutorialUI] UnitId로 하이라이트 타겟을 찾을 수 없음: {unitId}");
                return;
            }

            ShowHighlightForTarget(target, offset, size);
        }

        /// <summary>
        /// 하이라이트 표시
        /// </summary>
        public void ShowHighlight(string targetName, Vector2 offset, Vector2 size)
        {
            if (string.IsNullOrEmpty(targetName)) return;

            // TileGrid 특수 처리 - 전체 그리드 하이라이트
            if (targetName == "TileGrid")
            {
                ShowHighlightAllTiles(offset, size);
                return;
            }

            // 타겟 오브젝트 찾기
            var target = FindUIObject(targetName);
            if (target == null)
            {
                Debug.LogWarning($"[TutorialUI] 하이라이트 타겟을 찾을 수 없음: {targetName}");
                return;
            }

            ShowHighlightForTarget(target, offset, size);
        }

        /// <summary>
        /// 하이라이트 표시 (공통 로직)
        /// </summary>
        private void ShowHighlightForTarget(GameObject target, Vector2 offset, Vector2 size)
        {
            if (highlightObject == null)
            {
                Debug.LogError("[TutorialUI] highlightObject가 연결되지 않았습니다!");
                return;
            }

            if (highlightRect == null)
            {
                Debug.LogError("[TutorialUI] highlightRect가 연결되지 않았습니다!");
                return;
            }

            // 위치 설정
            var targetRect = target.GetComponent<RectTransform>();
            if (targetRect == null)
            {
                Debug.LogWarning($"[TutorialUI] 타겟에 RectTransform이 없음: {target.name}");
                return;
            }

            // 타겟의 월드 위치와 크기 가져오기
            Vector3[] targetCorners = new Vector3[4];
            targetRect.GetWorldCorners(targetCorners);

            // 하이라이트의 부모 Canvas 가져오기
            Canvas highlightCanvas = highlightRect.GetComponentInParent<Canvas>();
            if (highlightCanvas == null)
            {
                Debug.LogError("[TutorialUI] highlightRect의 부모 Canvas를 찾을 수 없음!");
                return;
            }

            // 타겟의 중심 월드 위치 계산
            Vector3 targetCenter = (targetCorners[0] + targetCorners[2]) / 2f;

            // 월드 위치를 하이라이트 캔버스의 로컬 위치로 변환
            RectTransform canvasRect = highlightCanvas.GetComponent<RectTransform>();
            Vector2 localPoint;

            if (highlightCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // ScreenSpaceOverlay인 경우 스크린 좌표로 변환
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, targetCenter);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    highlightRect.parent as RectTransform,
                    screenPoint,
                    null,
                    out localPoint);
            }
            else
            {
                // Camera 기반 캔버스인 경우
                Camera cam = highlightCanvas.worldCamera ?? Camera.main;
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, targetCenter);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    highlightRect.parent as RectTransform,
                    screenPoint,
                    cam,
                    out localPoint);
            }

            // 위치 설정 (offset 적용)
            highlightRect.anchoredPosition = localPoint + offset;

            // 크기 설정
            if (size != Vector2.zero)
            {
                highlightRect.sizeDelta = size;
            }
            else
            {
                // 타겟의 크기 계산 (월드 좌표 기준)
                float width = Vector3.Distance(targetCorners[0], targetCorners[3]);
                float height = Vector3.Distance(targetCorners[0], targetCorners[1]);

                // 스케일 보정
                Vector3 canvasScale = canvasRect.lossyScale;
                highlightRect.sizeDelta = new Vector2(width / canvasScale.x, height / canvasScale.y);
            }

            highlightObject.SetActive(true);

            // TutorialBlocker에 구멍 설정
            var blocker = FindAnyObjectByType<TutorialBlocker>(FindObjectsInactive.Include);
            if (blocker != null)
            {
                blocker.SetHole(highlightRect);
            }
        }

        /// <summary>
        /// 타일 좌표들 기반 하이라이트 표시 (4-panel 구멍 방식)
        /// </summary>
        public void ShowHighlightAtTilePositions(Vector2Int[] tilePositions, Vector2 offset, Vector2 size)
        {
            if (tilePositions == null || tilePositions.Length == 0) return;

            // GridVisualizer 찾기
            var gridVisualizer = FindAnyObjectByType<GridVisualizer>(FindObjectsInactive.Exclude);
            if (gridVisualizer == null)
            {
                Debug.LogWarning("[TutorialUI] GridVisualizer를 찾을 수 없음");
                return;
            }

            // 유효한 GridCell들의 월드 좌표 수집
            Vector3 minWorld = Vector3.positiveInfinity;
            Vector3 maxWorld = Vector3.negativeInfinity;
            int validCellCount = 0;

            foreach (var tilePos in tilePositions)
            {
                var gridCell = gridVisualizer.GetGridCellAt(tilePos);
                if (gridCell != null)
                {
                    // GridCell의 월드 바운드 계산
                    Vector3 cellPos = gridCell.transform.position;
                    Vector3 cellScale = gridCell.transform.lossyScale;
                    float halfWidth = cellScale.x / 2f;
                    float halfHeight = cellScale.y / 2f;

                    Vector3 cellMin = new Vector3(cellPos.x - halfWidth, cellPos.y - halfHeight, cellPos.z);
                    Vector3 cellMax = new Vector3(cellPos.x + halfWidth, cellPos.y + halfHeight, cellPos.z);

                    minWorld = Vector3.Min(minWorld, cellMin);
                    maxWorld = Vector3.Max(maxWorld, cellMax);
                    validCellCount++;
                }
            }

            if (validCellCount == 0)
            {
                Debug.LogWarning("[TutorialUI] 유효한 GridCell을 찾을 수 없음");
                return;
            }

            // 바운딩 박스의 중심과 크기 계산
            Vector3 worldCenter = (minWorld + maxWorld) / 2f;
            Vector3 worldSize = maxWorld - minWorld;

            // 월드 좌표를 스크린 좌표로 변환
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[TutorialUI] Main Camera를 찾을 수 없음");
                return;
            }

            // 하이라이트 캔버스 가져오기
            Canvas highlightCanvas = highlightRect.GetComponentInParent<Canvas>();
            if (highlightCanvas == null)
            {
                Debug.LogError("[TutorialUI] highlightRect의 부모 Canvas를 찾을 수 없음!");
                return;
            }

            // 중심점 스크린 좌표
            Vector2 screenCenter = cam.WorldToScreenPoint(worldCenter);

            // 캔버스 로컬 좌표로 변환
            RectTransform canvasRect = highlightCanvas.GetComponent<RectTransform>();
            Vector2 localPoint;
            Camera canvasCam = highlightCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : highlightCanvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                highlightRect.parent as RectTransform,
                screenCenter,
                canvasCam,
                out localPoint);

            // 위치 설정 (offset 적용)
            highlightRect.anchoredPosition = localPoint + offset;

            // 크기 설정
            if (size != Vector2.zero)
            {
                highlightRect.sizeDelta = size;
            }
            else
            {
                // 월드 크기를 스크린 크기로 변환
                Vector2 screenMin = cam.WorldToScreenPoint(minWorld);
                Vector2 screenMax = cam.WorldToScreenPoint(maxWorld);
                Vector2 screenSize = screenMax - screenMin;

                // 캔버스 스케일 보정
                Vector3 canvasScale = canvasRect.lossyScale;
                highlightRect.sizeDelta = new Vector2(
                    screenSize.x / canvasScale.x,
                    screenSize.y / canvasScale.y);
            }

            highlightObject.SetActive(true);

            // TutorialBlocker에 구멍 설정
            var blocker = FindAnyObjectByType<TutorialBlocker>(FindObjectsInactive.Include);
            if (blocker != null)
            {
                blocker.SetHole(highlightRect);
            }
        }

        /// <summary>
        /// 전체 그리드 타일 하이라이트 표시 (TileGrid 특수 처리)
        /// </summary>
        private void ShowHighlightAllTiles(Vector2 offset, Vector2 size)
        {
            // GridVisualizer 찾기
            var gridVisualizer = FindAnyObjectByType<GridVisualizer>(FindObjectsInactive.Exclude);
            if (gridVisualizer == null)
            {
                Debug.LogWarning("[TutorialUI] GridVisualizer를 찾을 수 없음");
                return;
            }

            // GridVisualizer의 모든 자식 GridCell들의 월드 바운드 계산
            Vector3 minWorld = Vector3.positiveInfinity;
            Vector3 maxWorld = Vector3.negativeInfinity;
            int validCellCount = 0;

            foreach (Transform child in gridVisualizer.transform)
            {
                var gridCell = child.GetComponent<GridCell>();
                if (gridCell != null)
                {
                    Vector3 cellPos = gridCell.transform.position;
                    Vector3 cellScale = gridCell.transform.lossyScale;
                    float halfWidth = cellScale.x / 2f;
                    float halfHeight = cellScale.y / 2f;

                    Vector3 cellMin = new Vector3(cellPos.x - halfWidth, cellPos.y - halfHeight, cellPos.z);
                    Vector3 cellMax = new Vector3(cellPos.x + halfWidth, cellPos.y + halfHeight, cellPos.z);

                    minWorld = Vector3.Min(minWorld, cellMin);
                    maxWorld = Vector3.Max(maxWorld, cellMax);
                    validCellCount++;
                }
            }

            if (validCellCount == 0)
            {
                Debug.LogWarning("[TutorialUI] GridVisualizer에 유효한 GridCell이 없음");
                return;
            }

            // 바운딩 박스의 중심 계산
            Vector3 worldCenter = (minWorld + maxWorld) / 2f;

            // 월드 좌표를 스크린 좌표로 변환
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[TutorialUI] Main Camera를 찾을 수 없음");
                return;
            }

            // 하이라이트 캔버스 가져오기
            Canvas highlightCanvas = highlightRect.GetComponentInParent<Canvas>();
            if (highlightCanvas == null)
            {
                Debug.LogError("[TutorialUI] highlightRect의 부모 Canvas를 찾을 수 없음!");
                return;
            }

            // 중심점 스크린 좌표
            Vector2 screenCenter = cam.WorldToScreenPoint(worldCenter);

            // 캔버스 로컬 좌표로 변환
            RectTransform canvasRect = highlightCanvas.GetComponent<RectTransform>();
            Vector2 localPoint;
            Camera canvasCam = highlightCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : highlightCanvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                highlightRect.parent as RectTransform,
                screenCenter,
                canvasCam,
                out localPoint);

            // 위치 설정 (offset 적용)
            highlightRect.anchoredPosition = localPoint + offset;

            // 크기 설정
            if (size != Vector2.zero)
            {
                highlightRect.sizeDelta = size;
            }
            else
            {
                // 월드 크기를 스크린 크기로 변환
                Vector2 screenMin = cam.WorldToScreenPoint(minWorld);
                Vector2 screenMax = cam.WorldToScreenPoint(maxWorld);
                Vector2 screenSize = screenMax - screenMin;

                // 캔버스 스케일 보정
                Vector3 canvasScale = canvasRect.lossyScale;
                highlightRect.sizeDelta = new Vector2(
                    screenSize.x / canvasScale.x,
                    screenSize.y / canvasScale.y);
            }

            highlightObject.SetActive(true);

            // TutorialBlocker에 구멍 설정
            var blocker = FindAnyObjectByType<TutorialBlocker>(FindObjectsInactive.Include);
            if (blocker != null)
            {
                blocker.SetHole(highlightRect);
            }
        }

        /// <summary>
        /// 하이라이트 숨기기
        /// </summary>
        public void HideHighlight()
        {
            // Unity 오브젝트는 destroyed 상태일 수 있으므로 명시적 null 체크
            if (highlightObject != null)
            {
                highlightObject.SetActive(false);
            }

            // TutorialBlocker 구멍 제거
            var blocker = FindAnyObjectByType<TutorialBlocker>(FindObjectsInactive.Include);
            if (blocker != null)
            {
                blocker.ClearHole();
            }
        }

        #endregion

        #region 두 번째 하이라이트

        /// <summary>
        /// 두 번째 하이라이트 표시
        /// </summary>
        public void ShowHighlight2(string targetName, Vector2 offset, Vector2 size)
        {
            if (string.IsNullOrEmpty(targetName)) return;

            var target = FindUIObject(targetName);
            if (target == null)
            {
                Debug.LogWarning($"[TutorialUI] 두 번째 하이라이트 타겟을 찾을 수 없음: {targetName}");
                return;
            }

            ShowHighlightForTarget2(target, offset, size);
        }

        /// <summary>
        /// 두 번째 타일 좌표 기반 하이라이트 표시
        /// </summary>
        public void ShowHighlightAtTilePositions2(Vector2Int[] tilePositions, Vector2 offset, Vector2 size)
        {
            if (tilePositions == null || tilePositions.Length == 0)
            {
                return;
            }

            if (highlightObject2 == null)
            {
                Debug.LogError("[TutorialUI] highlightObject2가 연결되지 않았습니다! Inspector에서 연결해주세요.");
                return;
            }

            if (highlightRect2 == null)
            {
                Debug.LogError("[TutorialUI] highlightRect2가 연결되지 않았습니다! Inspector에서 연결해주세요.");
                return;
            }

            var gridVisualizer = FindAnyObjectByType<GridVisualizer>(FindObjectsInactive.Exclude);
            if (gridVisualizer == null)
            {
                Debug.LogWarning("[TutorialUI] GridVisualizer를 찾을 수 없음");
                return;
            }

            Vector3 minWorld = Vector3.positiveInfinity;
            Vector3 maxWorld = Vector3.negativeInfinity;
            int validCellCount = 0;

            foreach (var tilePos in tilePositions)
            {
                var gridCell = gridVisualizer.GetGridCellAt(tilePos);
                if (gridCell != null)
                {
                    Vector3 cellPos = gridCell.transform.position;
                    Vector3 cellScale = gridCell.transform.lossyScale;
                    float halfWidth = cellScale.x / 2f;
                    float halfHeight = cellScale.y / 2f;

                    Vector3 cellMin = new Vector3(cellPos.x - halfWidth, cellPos.y - halfHeight, cellPos.z);
                    Vector3 cellMax = new Vector3(cellPos.x + halfWidth, cellPos.y + halfHeight, cellPos.z);

                    minWorld = Vector3.Min(minWorld, cellMin);
                    maxWorld = Vector3.Max(maxWorld, cellMax);
                    validCellCount++;
                }
            }

            if (validCellCount == 0)
            {
                return;
            }

            Vector3 worldCenter = (minWorld + maxWorld) / 2f;

            Camera cam = Camera.main;
            if (cam == null) return;

            Canvas highlightCanvas = highlightRect2.GetComponentInParent<Canvas>();
            if (highlightCanvas == null) return;

            Vector2 screenCenter = cam.WorldToScreenPoint(worldCenter);
            RectTransform canvasRect = highlightCanvas.GetComponent<RectTransform>();
            Vector2 localPoint;
            Camera canvasCam = highlightCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : highlightCanvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                highlightRect2.parent as RectTransform,
                screenCenter,
                canvasCam,
                out localPoint);

            highlightRect2.anchoredPosition = localPoint + offset;

            if (size != Vector2.zero)
            {
                highlightRect2.sizeDelta = size;
            }
            else
            {
                Vector2 screenMin = cam.WorldToScreenPoint(minWorld);
                Vector2 screenMax = cam.WorldToScreenPoint(maxWorld);
                Vector2 screenSize = screenMax - screenMin;

                Vector3 canvasScale = canvasRect.lossyScale;
                highlightRect2.sizeDelta = new Vector2(
                    screenSize.x / canvasScale.x,
                    screenSize.y / canvasScale.y);
            }

            highlightObject2.SetActive(true);

            // TutorialBlocker에 두 번째 구멍 설정
            var blocker = FindAnyObjectByType<TutorialBlocker>(FindObjectsInactive.Include);
            if (blocker != null)
            {
                blocker.SetHole2(highlightRect2);
            }
        }

        /// <summary>
        /// 두 번째 하이라이트 표시 (공통 로직)
        /// </summary>
        private void ShowHighlightForTarget2(GameObject target, Vector2 offset, Vector2 size)
        {
            if (highlightObject2 == null || highlightRect2 == null)
            {
                Debug.LogWarning("[TutorialUI] highlightObject2 또는 highlightRect2가 연결되지 않았습니다!");
                return;
            }

            var targetRect = target.GetComponent<RectTransform>();
            if (targetRect == null)
            {
                Debug.LogWarning($"[TutorialUI] 타겟에 RectTransform이 없음: {target.name}");
                return;
            }

            Vector3[] targetCorners = new Vector3[4];
            targetRect.GetWorldCorners(targetCorners);

            Canvas highlightCanvas = highlightRect2.GetComponentInParent<Canvas>();
            if (highlightCanvas == null) return;

            Vector3 targetCenter = (targetCorners[0] + targetCorners[2]) / 2f;
            RectTransform canvasRect = highlightCanvas.GetComponent<RectTransform>();
            Vector2 localPoint;

            if (highlightCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, targetCenter);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    highlightRect2.parent as RectTransform,
                    screenPoint,
                    null,
                    out localPoint);
            }
            else
            {
                Camera cam = highlightCanvas.worldCamera ?? Camera.main;
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, targetCenter);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    highlightRect2.parent as RectTransform,
                    screenPoint,
                    cam,
                    out localPoint);
            }

            highlightRect2.anchoredPosition = localPoint + offset;

            if (size != Vector2.zero)
            {
                highlightRect2.sizeDelta = size;
            }
            else
            {
                float width = Vector3.Distance(targetCorners[0], targetCorners[3]);
                float height = Vector3.Distance(targetCorners[0], targetCorners[1]);

                Vector3 canvasScale = canvasRect.lossyScale;
                highlightRect2.sizeDelta = new Vector2(width / canvasScale.x, height / canvasScale.y);
            }

            highlightObject2.SetActive(true);

            // TutorialBlocker에 두 번째 구멍 설정
            var blocker = FindAnyObjectByType<TutorialBlocker>(FindObjectsInactive.Include);
            if (blocker != null)
            {
                blocker.SetHole2(highlightRect2);
            }
        }

        /// <summary>
        /// 두 번째 하이라이트 숨기기
        /// </summary>
        public void HideHighlight2()
        {
            if (highlightObject2 != null)
            {
                highlightObject2.SetActive(false);
            }

            var blocker = FindAnyObjectByType<TutorialBlocker>(FindObjectsInactive.Include);
            if (blocker != null)
            {
                blocker.ClearHole2();
            }
        }

        #endregion

        #region 손가락 가이드

        /// <summary>
        /// 손가락 가이드 표시
        /// </summary>
        public void ShowHandGuide(Vector2 offset)
        {
            if (handGuideObject == null) return;

            StopDragGuide();

            // 하이라이트 위치 기준으로 배치
            if (highlightRect != null && handGuideRect != null)
            {
                handGuideRect.position = highlightRect.position;
                handGuideRect.anchoredPosition += offset;
            }

            handGuideObject.SetActive(true);

            // 애니메이션 재생
            if (handGuideAnimator != null)
            {
                handGuideAnimator.SetTrigger("Show");
            }
        }

        /// <summary>
        /// 드래그 가이드 표시 (하이라이트1에서 하이라이트2로 이동)
        /// </summary>
        public void ShowDragGuide(Vector2 offset1, Vector2 offset2)
        {
            if (handGuideObject == null) return;
            if (highlightRect == null || highlightRect2 == null) return;

            StopDragGuide();

            handGuideObject.SetActive(true);
            dragGuideCts = new CancellationTokenSource();
            DragGuideAsync(offset1, offset2, dragGuideCts.Token).Forget();
        }

        private async UniTaskVoid DragGuideAsync(Vector2 offset1, Vector2 offset2, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // 시작 위치 (하이라이트1)
                Vector3 startPos = highlightRect.position;
                startPos += (Vector3)offset1;

                // 끝 위치 (하이라이트2)
                Vector3 endPos = highlightRect2.position;
                endPos += (Vector3)offset2;

                // 시작 위치로 이동
                handGuideRect.position = startPos;

                // 애니메이션 트리거
                if (handGuideAnimator != null)
                {
                    handGuideAnimator.SetTrigger("Show");
                }

                // 잠시 대기
                await UniTask.Delay(TimeSpan.FromSeconds(dragGuideDelay), ignoreTimeScale: true, cancellationToken: ct);

                // 하이라이트1에서 하이라이트2로 이동
                float elapsed = 0f;
                while (elapsed < dragGuideDuration && !ct.IsCancellationRequested)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / dragGuideDuration);
                    handGuideRect.position = Vector3.Lerp(startPos, endPos, t);
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                handGuideRect.position = endPos;

                // 끝에서 잠시 대기 후 반복
                await UniTask.Delay(TimeSpan.FromSeconds(dragGuideDelay), ignoreTimeScale: true, cancellationToken: ct);
            }
        }

        private void StopDragGuide()
        {
            if (dragGuideCts != null)
            {
                dragGuideCts.Cancel();
                dragGuideCts.Dispose();
                dragGuideCts = null;
            }
        }

        /// <summary>
        /// 손가락 가이드 숨기기
        /// </summary>
        public void HideHandGuide()
        {
            StopDragGuide();

            if (handGuideObject != null)
            {
                handGuideObject.SetActive(false);
            }
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 전체 숨기기
        /// </summary>
        public void Hide()
        {
            HideDialog();
            HideHighlight();
            HideHighlight2();
            HideHandGuide();
        }

        /// <summary>
        /// UI 오브젝트 찾기
        /// </summary>
        private GameObject FindUIObject(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return null;

            // 1. Registry에서 검색 (O(1)) - 권장 방식
            var registered = TutorialTargetRegistry.Get(objectName);
            if (registered != null)
            {
                return registered;
            }

            // 2. GameObject.Find로 활성 오브젝트 검색 (폴백)
            var foundByName = GameObject.Find(objectName);
            if (foundByName != null)
            {
                return foundByName;
            }

            // 3. 비활성 오브젝트 포함 검색 (최후의 수단)
            // FindAnyObjectByType은 타입 기반이므로 이름 매칭을 위해 Canvas 하위 순회
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                var found = FindInChildren(canvas.transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// 자식 오브젝트에서 이름으로 검색 (비활성 포함)
        /// </summary>
        private GameObject FindInChildren(Transform parent, string objectName)
        {
            if (parent.name == objectName)
            {
                return parent.gameObject;
            }

            foreach (Transform child in parent)
            {
                var found = FindInChildren(child, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// UnitId로 UI 오브젝트 찾기 (DraggableGridUnitUi 검색)
        /// </summary>
        private GameObject FindUIObjectByUnitId(int unitId)
        {
            if (unitId <= 0) return null;

            // DraggableGridUnitUi 컴포넌트를 가진 오브젝트 중 UnitId가 일치하는 것 찾기
            var draggableUnits = FindObjectsByType<DraggableGridUnitUi>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var unit in draggableUnits)
            {
                if (unit.UnitId == unitId)
                {
                    return unit.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// 캐릭터 이미지 설정
        /// </summary>
        public void SetCharacterImage(Sprite sprite)
        {
            if (characterImage != null && sprite != null)
            {
                characterImage.sprite = sprite;
            }
        }

        #endregion
    }
}
