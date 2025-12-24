using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// 이 컴포넌트를 UI에 붙이면 자동으로 튜토리얼 타겟으로 등록됨
    /// 비활성화 상태에서도 등록됨
    /// </summary>
    public class TutorialTarget : MonoBehaviour
    {
        [Header("타겟 설정")]
        [SerializeField] private string targetKey;
        [SerializeField] private bool useGameObjectName = true;

        public string TargetKey => useGameObjectName ? gameObject.name : targetKey;

        private void Awake()
        {
            // 비활성화 상태에서도 등록
            TutorialTargetRegistry.Register(TargetKey, gameObject);
        }

        private void OnDestroy()
        {
            TutorialTargetRegistry.Unregister(TargetKey);
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
