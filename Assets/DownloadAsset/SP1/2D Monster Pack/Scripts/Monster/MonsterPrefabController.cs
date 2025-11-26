using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP1Assets.MonsterPack2D
{
    public class MonsterPrefabController : MonoBehaviour
    {
        Animator _animator;
        public Animator GetAnimator() { return _animator; }
        List<string> _animNames = new List<string>();
        public List<string> GetAnimationNames() { return _animNames; }

        public void Init()
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogError("Can't find Animator component in children");
                return;
            }

            _animNames.Clear();

            AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;
            foreach (var cl in clips)
            {
                if (cl.name != "_default")
                    _animNames.Add(cl.name);
            }
        }

        public void ResetPose()
        {
            _animator.Play("_default");
            _animator.Update(0f);
            gameObject.SetActive(false);
            gameObject.SetActive(true);
        }


        public void PlayAnimation(string animName, float crossFadeDuration, float normalizedStartTime = 0)
        {
            if (_animNames.Contains(animName) == false)
            {
                Debug.LogWarning($"Animation Name [{animName}] does not exist in this character.");
                return;
            }

            if (crossFadeDuration > 0)
                _animator.CrossFade(animName, crossFadeDuration, 0, normalizedStartTime);
            else
                _animator.Play(animName, 0, normalizedStartTime);
        }
    }
}