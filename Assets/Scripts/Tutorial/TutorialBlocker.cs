using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 중 터치/드래그 제어
    /// </summary>
    public class TutorialBlocker : MonoBehaviour, IPointerClickHandler
    {
        [Header("블로커 UI")]
        [SerializeField] private GameObject blockerPanel;       // 전체 화면 덮는 패널
        [SerializeField] private Image blockerImage;            // 반투명 검정 이미지
        [SerializeField] private float dimAlpha = 0.7f;         // 어둡게 하는 정도

        [Header("허용 영역")]
        [SerializeField] private RectTransform allowedAreaRect; // 터치 허용 영역
        [SerializeField] private List<string> allowedButtonNames = new List<string>();

        // 구멍 컴포넌트 (blockerImage에 붙음)
        private TutorialBlockerHole blockerHole;

        // 상태
        private bool isBlocking;
        private bool isDragEnabled;  // 드래그 허용 여부 (false면 모든 드래그 차단)
        private string targetButtonName;
        private string dragSourceName;
        private string dragTargetName;
        private Vector2Int[] allowedTiles;
        private int[] allowedUnitIds;

        // 대기용
        private UniTaskCompletionSource targetTouchSource;
        private UniTaskCompletionSource dragCompleteSource;

        // 터치된 버튼/드래그 완료 정보
        private string lastTouchedButton;
        private string lastDragSource;
        private string lastDragTarget;

        private void Awake()
        {
            // blockerImage에 TutorialBlockerHole 컴포넌트 추가
            if (blockerImage != null)
            {
                blockerHole = blockerImage.GetComponent<TutorialBlockerHole>();
                if (blockerHole == null)
                {
                    blockerHole = blockerImage.gameObject.AddComponent<TutorialBlockerHole>();
                }
            }

            Unblock();
        }

        #region 블로킹 제어

        /// <summary>
        /// 블로킹 시작
        /// </summary>
        public void Block()
        {
            isBlocking = true;

            if (blockerPanel != null)
            {
                blockerPanel.SetActive(true);
            }

            if (blockerImage != null)
            {
                var color = blockerImage.color;
                color.a = dimAlpha;
                blockerImage.color = color;
            }

            allowedButtonNames.Clear();
            targetButtonName = null;
            dragSourceName = null;
            dragTargetName = null;
            allowedTiles = null;
            allowedUnitIds = null;
        }

        /// <summary>
        /// 블로킹 해제
        /// </summary>
        public void Unblock()
        {
            isBlocking = false;

            if (blockerPanel != null)
            {
                blockerPanel.SetActive(false);
            }

            allowedButtonNames.Clear();
        }

        /// <summary>
        /// 대화창만 터치 허용 (드래그 차단)
        /// </summary>
        public void AllowDialogOnly()
        {
            allowedButtonNames.Clear();
            allowedButtonNames.Add("DialogPanel");
            isDragEnabled = false;  // 드래그 차단
        }

        /// <summary>
        /// 특정 타겟 터치 허용
        /// </summary>
        public void AllowTarget(string buttonName)
        {
            AllowDialogOnly();
            targetButtonName = buttonName;
            allowedButtonNames.Add(buttonName);
        }

        /// <summary>
        /// 드래그 허용
        /// </summary>
        public void AllowDrag(string sourceName, string targetName, Vector2Int[] tiles, int[] unitIds)
        {
            AllowDialogOnly();
            isDragEnabled = true;  // 드래그 허용
            dragSourceName = sourceName;
            dragTargetName = targetName;
            allowedTiles = tiles;
            allowedUnitIds = unitIds;

            // 드래그 관련 오브젝트 허용
            if (!string.IsNullOrEmpty(sourceName))
                allowedButtonNames.Add(sourceName);
            if (!string.IsNullOrEmpty(targetName))
                allowedButtonNames.Add(targetName);
        }

        #endregion

        #region 터치 감지

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isBlocking) return;

            // 클릭된 오브젝트 확인
            var clickedObject = eventData.pointerCurrentRaycast.gameObject;
            if (clickedObject == null) return;

            string clickedName = clickedObject.name;

            Debug.Log($"[TutorialBlocker] OnPointerClick - clickedName: {clickedName}, targetButtonName: {targetButtonName}, allowedButtons: {string.Join(", ", allowedButtonNames)}");

            // 사이드 패널 클릭 시 두 번째 구멍 영역 체크
            if (IsSidePanel(clickedName) && blockerHole != null)
            {
                Vector2 screenPoint = eventData.position;
                Camera eventCamera = eventData.pressEventCamera;

                // 두 번째 구멍 영역 안이면 targetButtonName으로 처리
                if (!blockerHole.IsRaycastLocationValid(screenPoint, eventCamera))
                {
                    if (!string.IsNullOrEmpty(targetButtonName) && allowedButtonNames.Contains(targetButtonName))
                    {
                        // 실제 버튼 클릭 실행
                        var targetObj = TutorialTargetRegistry.Get(targetButtonName);
                        if (targetObj != null)
                        {
                            var button = targetObj.GetComponent<Button>();
                            button?.onClick.Invoke();
                        }

                        lastTouchedButton = targetButtonName;
                        targetTouchSource?.TrySetResult();
                        return;
                    }
                }
            }

            // 허용된 버튼인지 확인
            if (allowedButtonNames.Contains(clickedName))
            {
                lastTouchedButton = clickedName;

                // 타겟 버튼이면 완료 알림
                if (clickedName == targetButtonName)
                {
                    targetTouchSource?.TrySetResult();
                }
            }
        }

        private bool IsSidePanel(string name)
        {
            return name == "TopPanel" || name == "BottomPanel" || name == "LeftPanel" || name == "RightPanel";
        }

        /// <summary>
        /// 특정 버튼 터치 대기
        /// </summary>
        public async UniTask WaitForTargetTouchAsync(string buttonName)
        {
            targetButtonName = buttonName;
            targetTouchSource = new UniTaskCompletionSource();
            await targetTouchSource.Task;
        }

        /// <summary>
        /// 버튼 터치 알림 (외부에서 호출)
        /// </summary>
        public void NotifyButtonTouched(string buttonName)
        {
            if (buttonName == targetButtonName)
            {
                targetTouchSource?.TrySetResult();
            }
        }

        #endregion

        #region 드래그 감지

        /// <summary>
        /// 드래그 완료 대기
        /// </summary>
        public async UniTask WaitForDragCompleteAsync(string sourceName, string targetName)
        {
            dragSourceName = sourceName;
            dragTargetName = targetName;
            dragCompleteSource = new UniTaskCompletionSource();
            await dragCompleteSource.Task;
        }

        /// <summary>
        /// 드래그 완료 알림 (외부에서 호출)
        /// </summary>
        public void NotifyDragComplete(string sourceName, string targetName)
        {
            lastDragSource = sourceName;
            lastDragTarget = targetName;

            // 조건 확인
            // sourceName/targetName이 null이거나 빈 문자열이면 무조건 매칭 (어떤 유닛이든 허용)
            bool sourceMatch = string.IsNullOrEmpty(dragSourceName) || string.IsNullOrEmpty(sourceName) || sourceName == dragSourceName;
            bool targetMatch = string.IsNullOrEmpty(dragTargetName) || string.IsNullOrEmpty(targetName) || targetName == dragTargetName;

            if (sourceMatch && targetMatch)
            {
                dragCompleteSource?.TrySetResult();
            }
        }

        /// <summary>
        /// 드래그가 허용된 소스인지 확인
        /// </summary>
        public bool IsDragAllowed(string sourceName, int unitId = -1)
        {
            if (!isBlocking) return true;

            // 드래그 자체가 비활성화된 경우 (Touch, TouchTarget 스텝 등)
            if (!isDragEnabled) return false;

            // 소스 이름 체크
            if (!string.IsNullOrEmpty(dragSourceName) && sourceName != dragSourceName)
                return false;

            // 유닛 ID 체크
            if (allowedUnitIds != null && allowedUnitIds.Length > 0)
            {
                if (unitId < 0) return false;

                bool found = false;
                foreach (var allowed in allowedUnitIds)
                {
                    if (unitId == allowed)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }

            return true;
        }

        /// <summary>
        /// 드롭 위치가 허용된 타일인지 확인
        /// </summary>
        public bool IsDropAllowed(Vector2Int tilePosition)
        {
            if (!isBlocking) return true;
            if (allowedTiles == null || allowedTiles.Length == 0) return true;

            foreach (var tile in allowedTiles)
            {
                if (tile == tilePosition)
                    return true;
            }

            return false;
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 현재 블로킹 중인지
        /// </summary>
        public bool IsBlocking => isBlocking;

        /// <summary>
        /// 특정 오브젝트가 허용되었는지 확인
        /// </summary>
        public bool IsObjectAllowed(string objectName)
        {
            if (!isBlocking) return true;
            return allowedButtonNames.Contains(objectName);
        }

        /// <summary>
        /// 허용 버튼 추가
        /// </summary>
        public void AddAllowedButton(string buttonName)
        {
            if (!allowedButtonNames.Contains(buttonName))
            {
                allowedButtonNames.Add(buttonName);
            }
        }

        /// <summary>
        /// 허용 버튼 제거
        /// </summary>
        public void RemoveAllowedButton(string buttonName)
        {
            allowedButtonNames.Remove(buttonName);
        }

        #endregion

        #region 구멍 (Hole) 설정

        /// <summary>
        /// 구멍 영역 설정 (하이라이트 RectTransform)
        /// </summary>
        public void SetHole(RectTransform rect)
        {
            if (blockerHole != null)
            {
                blockerHole.SetHole(rect);
            }
        }

        /// <summary>
        /// 구멍 제거
        /// </summary>
        public void ClearHole()
        {
            if (blockerHole != null)
            {
                blockerHole.ClearHole();
            }
        }

        /// <summary>
        /// 두 번째 구멍 영역 설정
        /// </summary>
        public void SetHole2(RectTransform rect)
        {
            if (blockerHole != null)
            {
                blockerHole.SetHole2(rect);
            }
        }

        /// <summary>
        /// 두 번째 구멍 제거
        /// </summary>
        public void ClearHole2()
        {
            if (blockerHole != null)
            {
                blockerHole.ClearHole2();
            }
        }

        #endregion
    }
}
