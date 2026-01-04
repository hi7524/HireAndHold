using UnityEngine;
using UnityEngine.UI;

namespace Tutorial
{
    /// <summary>
    /// 이 컴포넌트를 UI에 붙이면 자동으로 튜토리얼 타겟으로 등록됨
    /// 비활성화 상태에서도 등록됨
    /// 버튼인 경우 클릭 시 자동으로 NotifyButtonTouched 호출
    /// </summary>
    public class TutorialTarget : MonoBehaviour
    {
        [Header("타겟 설정")]
        [SerializeField] private string targetKey;
        [SerializeField] private bool useGameObjectName = true;

        private Button button;
        private bool isListenerRegistered;

        public string TargetKey => useGameObjectName ? gameObject.name : targetKey;

        private void Awake()
        {
            // 비활성화 상태에서도 등록
            TutorialTargetRegistry.Register(TargetKey, gameObject);

            RegisterButtonListener();
        }

        private void OnEnable()
        {
            // Awake가 호출 안 된 경우를 대비 (비활성 상태로 시작했다가 활성화된 경우)
            RegisterButtonListener();
        }

        private void RegisterButtonListener()
        {
            if (isListenerRegistered) return;

            // 버튼 컴포넌트가 있으면 클릭 이벤트 등록
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
                isListenerRegistered = true;
            }
        }

        private void OnButtonClicked()
        {
            // 튜토리얼 매니저에 버튼 터치 알림
            TutorialManager.Instance?.NotifyButtonTouched(TargetKey);
        }

        private void OnDestroy()
        {
            TutorialTargetRegistry.Unregister(TargetKey);

            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (useGameObjectName && string.IsNullOrEmpty(targetKey))
            {
                targetKey = gameObject.name;
            }
        }
#endif
    }
}
