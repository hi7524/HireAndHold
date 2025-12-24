using System.Collections.Generic;
using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 타겟 등록소 - 씬의 UI를 등록해서 ScriptableObject에서 참조 가능하게 함
    /// </summary>
    public static class TutorialTargetRegistry
    {
        private static readonly Dictionary<string, GameObject> targets = new Dictionary<string, GameObject>();

        /// <summary>
        /// 타겟 등록
        /// </summary>
        public static void Register(string key, GameObject target)
        {
            if (string.IsNullOrEmpty(key) || target == null) return;

            if (targets.ContainsKey(key))
            {
                targets[key] = target;
                Debug.Log($"[TutorialTarget] 업데이트: {key}");
            }
            else
            {
                targets.Add(key, target);
                Debug.Log($"[TutorialTarget] 등록: {key}");
            }
        }

        /// <summary>
        /// 타겟 해제
        /// </summary>
        public static void Unregister(string key)
        {
            if (targets.ContainsKey(key))
            {
                targets.Remove(key);
                Debug.Log($"[TutorialTarget] 해제: {key}");
            }
        }

        /// <summary>
        /// 타겟 가져오기
        /// </summary>
        public static GameObject Get(string key)
        {
            if (targets.TryGetValue(key, out var target))
            {
                return target;
            }
            return null;
        }

        /// <summary>
        /// 타겟 존재 여부
        /// </summary>
        public static bool Has(string key)
        {
            return targets.ContainsKey(key) && targets[key] != null;
        }

        /// <summary>
        /// 모든 타겟 목록 (디버그용)
        /// </summary>
        public static IEnumerable<string> GetAllKeys()
        {
            return targets.Keys;
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public static void Clear()
        {
            targets.Clear();
        }
    }
}
