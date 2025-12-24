using System;
using System.Collections;
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

        [Header("하이라이트")]
        [SerializeField] private GameObject highlightObject;
        [SerializeField] private RectTransform highlightRect;

        [Header("손가락 가이드")]
        [SerializeField] private GameObject handGuideObject;
        [SerializeField] private RectTransform handGuideRect;
        [SerializeField] private Animator handGuideAnimator;

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
        /// 하이라이트 표시
        /// </summary>
        public void ShowHighlight(string targetName, Vector2 offset, Vector2 size)
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

            // 타겟 오브젝트 찾기
            var target = FindUIObject(targetName);
            if (target == null)
            {
                Debug.LogWarning($"[TutorialUI] 하이라이트 타겟을 찾을 수 없음: {targetName}");
                return;
            }

            // 위치 설정
            var targetRect = target.GetComponent<RectTransform>();
            if (targetRect == null)
            {
                Debug.LogWarning($"[TutorialUI] 타겟에 RectTransform이 없음: {targetName}");
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

            Debug.Log($"[TutorialUI] 하이라이트 표시: {targetName}, 위치: {highlightRect.anchoredPosition}, 크기: {highlightRect.sizeDelta}");

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

        #region 손가락 가이드

        /// <summary>
        /// 손가락 가이드 표시
        /// </summary>
        public void ShowHandGuide(Vector2 offset)
        {
            if (handGuideObject == null) return;

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
        /// 손가락 가이드 숨기기
        /// </summary>
        public void HideHandGuide()
        {
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
            HideHandGuide();
        }

        /// <summary>
        /// UI 오브젝트 찾기
        /// </summary>
        private GameObject FindUIObject(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return null;

            // 1. Registry에서 검색 (O(1))
            var registered = TutorialTargetRegistry.Get(objectName);
            if (registered != null)
            {
                return registered;
            }

            // 2. 이름으로 직접 검색 (폴백)
            var allObjects = Resources.FindObjectsOfTypeAll<RectTransform>();
            foreach (var obj in allObjects)
            {
                if (obj.gameObject.scene.IsValid() && obj.name == objectName)
                {
                    Debug.Log($"[TutorialUI] 이름으로 발견: {objectName}");
                    return obj.gameObject;
                }
            }

            Debug.LogWarning($"[TutorialUI] 타겟을 찾을 수 없음: {objectName}");
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
