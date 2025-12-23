using UnityEngine;
using UnityEngine.UI;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 블로커에 구멍을 뚫는 컴포넌트
    /// blockerImage(BlockPanel)에 붙여서 사용
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TutorialBlockerHole : MonoBehaviour, ICanvasRaycastFilter
    {
        private RectTransform holeRect;

        /// <summary>
        /// 구멍 영역 설정
        /// </summary>
        public void SetHole(RectTransform rect)
        {
            holeRect = rect;
        }

        /// <summary>
        /// 구멍 제거
        /// </summary>
        public void ClearHole()
        {
            holeRect = null;
        }

        /// <summary>
        /// ICanvasRaycastFilter 구현
        /// 구멍 영역 안쪽이면 raycast 무시 (터치 통과)
        /// </summary>
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            // 구멍이 없으면 무조건 막음
            if (holeRect == null || !holeRect.gameObject.activeInHierarchy)
                return true;

            // 구멍 영역 안쪽인지 확인
            bool isInsideHole = RectTransformUtility.RectangleContainsScreenPoint(holeRect, screenPoint, eventCamera);

            // 구멍 안쪽이면 false 반환 (이 Image가 raycast 안 받음 = 터치 통과)
            return !isInsideHole;
        }
    }
}
